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
            var fb_options = new FeedbackMessageOptions(MessageComponents: new IMessageComponent[]
            {
                new ActionRowComponent(new[]
                {
                    new ButtonComponent(Style:ButtonComponentStyle.Primary, "Guess", CustomID:CustomIDHelpers.CreateButtonID("submit-guess"))
                })
            });
            FeedbackMessage msg = new FeedbackMessage(_game.RiddleText ?? "No more riddles", Colour: Color.AliceBlue);
            return (Result)await _feedback.SendContextualMessageAsync(msg,ct: this.CancellationToken, options:fb_options);
        }
    }
}
