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

namespace Riddle_Discord_Bot.Interactions;

public class ButtonInteractions : InteractionGroup
{
    private readonly ILogger<ButtonInteractions> _log;
    private readonly ICommandContext _context;
    private readonly IDiscordRestChannelAPI _channelAPI;
    private readonly IDiscordRestInteractionAPI _interactionAPI;
    private readonly FeedbackService _feedbackService;
    private readonly IRiddleGame _game;

    /// <summary>
    /// Initializes a new instance of the <see cref="ButtonInteractions"/> class.
    /// </summary>
    /// <param name="log">The logging instance for this type.</param>
    public ButtonInteractions(ILogger<ButtonInteractions> log, ICommandContext context, IDiscordRestChannelAPI channelAPI, IDiscordRestInteractionAPI interactionAPI, FeedbackService feedbackService, IRiddleGame game)
    {
        _log = log;
        _context = context;
        _channelAPI = channelAPI;
        _interactionAPI = interactionAPI;
        _feedbackService = feedbackService;
        _game = game;
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
        if (_game.Guess(guessText))
        {

            await _channelAPI.CreateMessageAsync(_context.ChannelID, $"User {_context.User.Username} has guessed CORRECT with guess: {guessText}");
            _game.NextRiddle();
            // Logic for sending next riddle.
            var option = new FeedbackMessageOptions(MessageComponents: new IMessageComponent[]
            {
                new ActionRowComponent(new[]
                {
                    new ButtonComponent(Style:ButtonComponentStyle.Primary, "Guess", CustomID:CustomIDHelpers.CreateButtonID("submit-guess"))
                })
            });


            var embed = new Embed(Description: _game.RiddleText ?? "No more riddles remaining.", Colour: Color.LawnGreen);
            return (Result)await _channelAPI.CreateMessageAsync(channelID: _context.ChannelID, embeds: new[] { embed }, components:option.MessageComponents);
            //_channelAPI.CreateMessageAsync()
        }
        else
        {
            await _channelAPI.CreateMessageAsync(_context.ChannelID, $"User {_context.User.Username} has guessed WRONG with guess: {guessText}");
        }
        return await Task.FromResult(Result.FromSuccess());
    }


    [Button("submit-guess")]
    [SuppressInteractionResponse(true)]
    public async Task<Result> OnButtonPressedAsync()
    {
        _log.LogInformation("Button pressed");

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
                    "Test Modal",
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
                                    "Short Text",
                                    1,
                                    32,
                                    true,
                                    string.Empty,
                                    "Short Text here"
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
