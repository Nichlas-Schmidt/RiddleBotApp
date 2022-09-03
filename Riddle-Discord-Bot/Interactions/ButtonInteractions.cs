using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Interactivity;
using Remora.Results;
using Remora.Discord.Commands.Services;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using System.Drawing;
using Microsoft.Extensions.Options;
using Remora.Discord.Extensions.Formatting;
using System.Text;
using Remora.Discord.Extensions.Embeds;
using Microsoft.Extensions.Configuration;

namespace Riddle_Discord_Bot.Interactions;

public class ButtonInteractions : InteractionGroup
{
    private readonly ILogger<ButtonInteractions> _log;
    private readonly ICommandContext _context;
    private readonly IDiscordRestChannelAPI _channelAPI;
    private readonly IDiscordRestInteractionAPI _interactionAPI;
    private readonly FeedbackService _feedbackService;
    private readonly IRiddleGame _game;
    private Snowflake logflake;
    private Snowflake leaderboardflake;
    private readonly IConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="ButtonInteractions"/> class.
    /// </summary>
    /// <param name="log">The logging instance for this type.</param>
    public ButtonInteractions(ILogger<ButtonInteractions> log, ICommandContext context, IDiscordRestChannelAPI channelAPI, IDiscordRestInteractionAPI interactionAPI, FeedbackService feedbackService, IRiddleGame game, IConfiguration config)
    {
        _log = log;
        _context = context;
        _channelAPI = channelAPI;
        _interactionAPI = interactionAPI;
        _feedbackService = feedbackService;
        _game = game;
        _config = config;
        logflake = new(_config.GetValue<ulong>("log_channel_id"));
        leaderboardflake = new(_config.GetValue<ulong>("leaderboard_channel_id"));
    }

    /// <summary>
    /// Logs submitted modal data.
    /// </summary>
    /// <param name="guessText">The value of the modal text input component.</param>
    /// <returns>A result which may or may not have succeeded.</returns>
    [Modal("guessModal")]
    public async Task<Result> OnModalSubmitAsync(string guessText)
    {
        _log.LogInformation("Received modal response");
        _log.LogInformation("Received input: {Input}", guessText);
        if (_game.ActiveRiddle == null) 
        {
            FeedbackMessageOptions msgopt = new(MessageFlags: MessageFlags.Ephemeral);
            await _feedbackService.SendContextualInfoAsync("no active riddle",ct:this.CancellationToken,options:msgopt);
        }
        else
        {
            bool guessed = false;
            bool more_riddles = true;
            lock (_game)
            {
                if (_game.Guess(guessText))
                {
                    _game.LeaderboardWrite(Mention.User(_context.User));
                    _game.MoveActiveToUsed();
                    more_riddles = _game.SetNextActive();
                    guessed = true;
                }
            }
            if (guessed)
            {
                await _channelAPI.CreateMessageAsync(_context.ChannelID, $"{Mention.User(_context.User)} has guessed CORRECT with guess: {guessText}");
                await _channelAPI.CreateMessageAsync(logflake, $"{_context.User.Username} has guessed CORRECT with guess: {guessText}");
                // Logic for sending next riddle.
                var button_options = new FeedbackMessageOptions(MessageComponents: new IMessageComponent[]
                {
                new ActionRowComponent(new[]
                {
                    new ButtonComponent(Style:ButtonComponentStyle.Primary, "Submit a guess", CustomID:CustomIDHelpers.CreateButtonID("submit-guess"))
                })
                });

                var winners = _game.LeaderboardRead();
                List<IEmbedField> fields = new();
                if (winners.Count > 0)
                {
                    int pos_id = 0;
                    foreach (var win in winners)
                    {
                        pos_id++;
                        var _field = new EmbedField($"{pos_id}.", $"{win.Mention} with {win.Points} Point(s)");
                        fields.Add(_field);
                    }
                }
                else
                {
                    var field = new EmbedField($"Empty", "No winners currently.");
                    fields.Add(field);
                }

                Embed emb = new(Fields: fields, Title: $"Leaderboard", 
                    Footer: new EmbedFooter($"Updated {DateTime.Now.ToShortDateString()}"),
                    Colour:Color.Yellow, Thumbnail:new EmbedThumbnail("https://i.imgur.com/o9fSJdN.png"));

                await _channelAPI.CreateMessageAsync(leaderboardflake, embeds: new[] { emb });

                if(more_riddles != true)
                {
                    _game.StopGame();
                    return (Result)await _feedbackService.SendEmbedAsync(_context.ChannelID, new Embed(Description: "I am out of riddles. Check back later!"));
                }
                return (Result)await _feedbackService.SendEmbedAsync(_context.ChannelID, new Embed(Title:"Riddle",Description: _game.ActiveRiddle?.Text ?? "Error getting text"),options:button_options);

            }
            else
            {
                await _channelAPI.CreateMessageAsync(logflake, $"{_context.User.Username} has guessed WRONG with guess: {guessText}");
                await _feedbackService.SendContextualMessageAsync(new FeedbackMessage("Sorry your guess was not correct",Colour:Color.Black),options: new FeedbackMessageOptions(MessageFlags: MessageFlags.Ephemeral));
                _log.LogInformation("{} has guessed WRONG with guess: {}",_context.User.Username,guessText);
            }
        }

        return await Task.FromResult(Result.FromSuccess());
    }


    [Button("submit-guess")]
    [SuppressInteractionResponse(true)]
    public async Task<Result> OnButtonPressedAsync()
    {
        _log.LogInformation("Button pressed");
        if (_game.ActiveRiddle == null)
        {
            FeedbackMessageOptions msgopt = new(MessageFlags: MessageFlags.Ephemeral);
            await _feedbackService.SendContextualInfoAsync("no active riddle", ct: this.CancellationToken, options: msgopt);
            return await Task.FromResult(Result.FromSuccess());
        }

        if (_context is not InteractionContext interactionContext)
        {
            return (Result)await _feedbackService.SendContextualWarningAsync
            (
                "This command can only be used with slash commands.",
                _context.User.ID,
                new FeedbackMessageOptions(MessageFlags: MessageFlags.Ephemeral)
            );
        }

        var response = new InteractionResponse
        (
            InteractionCallbackType.Modal,
            new
            (
                new InteractionModalCallbackData
                (
                    CustomIDHelpers.CreateModalID("guessModal"),
                    "Give your guess!",
                    new[]
                    {
                        new ActionRowComponent
                        (
                            new[]
                            {
                                new TextInputComponent
                                (
                                    "guess-text",
                                    TextInputStyle.Short,
                                    "Guess",
                                    1,
                                    32,
                                    true,
                                    string.Empty,
                                    "enter guess"
                                )
                            }
                        )
                    }
                )
            )
        );

        var result = await _interactionAPI.CreateInteractionResponseAsync
        (
            interactionContext.ID,
            interactionContext.Token,
            response,
            ct: this.CancellationToken
        );

        return result;
    }

}
