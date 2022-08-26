using Microsoft.Extensions.Logging;
using Polly;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riddle_Discord_Bot.Interactions
{
    public class ButtonInteractions : InteractionGroup
    {
        private readonly ILogger<ButtonInteractions> _log;
        private readonly ICommandContext _context;
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly IDiscordRestInteractionAPI _interactionAPI;
        private readonly FeedbackService _feedbackService;

        public ButtonInteractions(ILogger<ButtonInteractions> log, ICommandContext context, IDiscordRestChannelAPI channelAPI, IDiscordRestInteractionAPI interactionAPI, FeedbackService feedbackService)
        {
            _log = log;
            _context = context;
            _channelAPI = channelAPI;
            _interactionAPI = interactionAPI;
            _feedbackService = feedbackService;
        }

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
}
