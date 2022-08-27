using Remora.Commands.Attributes;
using Remora.Commands.Groups;
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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Riddle_Discord_Bot.Interactions;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Rest.Core;

namespace Riddle_Discord_Bot.Commands
{
    internal class RiddleCommands : CommandGroup
    {
        private readonly FeedbackService _feedback;
        private readonly ICommandContext _context;
        private readonly IDiscordRestInteractionAPI _interactionAPI;
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly IRiddleGame _game;

        public RiddleCommands(FeedbackService feedback, ICommandContext context, IDiscordRestInteractionAPI interactionAPI, IDiscordRestChannelAPI channelAPI, IRiddleGame game)
        {
            _feedback = feedback;
            _context = context;
            _interactionAPI = interactionAPI;
            _channelAPI = channelAPI;
            _game = game;
        }

        [Command("Start")]
        [Description("Starts the riddle game")]
        public async Task<IResult> StartGame()
        {
            _game.StartGame();
            var options = new FeedbackMessageOptions(MessageComponents: new IMessageComponent[]
            {
                new ActionRowComponent(new[]
                {
                    new ButtonComponent(Style:ButtonComponentStyle.Primary, "Guess", CustomID:CustomIDHelpers.CreateButtonID("submit-guess"))
                })
            });


            return (Result)await _feedback.SendContextualMessageAsync(new FeedbackMessage(_game.RiddleText ?? "No riddle", Colour:Color.Brown),null,options);
        }


        [Command("modal")]
        [SuppressInteractionResponse(true)]
        public async Task<Result> ShowModalAsync()
        {
            if (_context is not InteractionContext interactionContext)
            {
                return (Result)await _feedback.SendContextualWarningAsync
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
                        CustomIDHelpers.CreateModalID("guessmodal"),
                        "Test Modal",
                        new[]
                        {
                        new ActionRowComponent
                        (
                            new[]
                            {
                                new TextInputComponent
                                (
                                    "modal-text-input",
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
