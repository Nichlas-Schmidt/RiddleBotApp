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

namespace Riddle_Discord_Bot.Interactions;

public class ModalInteractions : InteractionGroup
{
    private readonly ILogger<ModalInteractions> _log;
    private readonly ICommandContext _context;
    private readonly IDiscordRestChannelAPI _channelAPI;
    private readonly IDiscordRestInteractionAPI _interactionAPI;
    private readonly FeedbackService _feedbackService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModalInteractions"/> class.
    /// </summary>
    /// <param name="log">The logging instance for this type.</param>
    public ModalInteractions(ILogger<ModalInteractions> log, ICommandContext context, IDiscordRestChannelAPI channelAPI, IDiscordRestInteractionAPI interactionAPI, FeedbackService feedbackService)
    {
        _log = log;
        _context = context;
        _channelAPI = channelAPI;
        _interactionAPI = interactionAPI;
        _feedbackService = feedbackService;
    }

    /// <summary>
    /// Logs submitted modal data.
    /// </summary>
    /// <param name="guessText">The value of the modal text input component.</param>
    /// <returns>A result which may or may not have succeeded.</returns>
    [Modal("guessModal")]
    public Task<Result> OnModalSubmitAsync(string guessText)
    {
        _log.LogInformation("Received modal response");
        _log.LogInformation("Received input: {Input}", guessText);
        _channelAPI.CreateMessageAsync(_context.ChannelID, $"{_context.User.Username} guessed {guessText}");
        return Task.FromResult(Result.FromSuccess());
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
