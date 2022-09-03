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
using Remora.Discord.Extensions.Formatting;
using Humanizer;

namespace Riddle_Discord_Bot.Commands
{
    internal class RiddleCommands : CommandGroup
    {
        private readonly FeedbackService _feedback;
        private readonly IRiddleGame _game;

        public RiddleCommands(FeedbackService feedback, IRiddleGame game)
        {
            _feedback = feedback;
            _game = game;
        }

        [Command("Start")]
        [Description("Starts the riddle game")]
        public async Task<IResult> StartGame()
        {
            bool more_riddles = _game.StartGame();
            var button_options = new FeedbackMessageOptions(MessageComponents: new IMessageComponent[]
            {
                new ActionRowComponent(new[]
                {
                    new ButtonComponent(Style:ButtonComponentStyle.Primary, "Submit a guess", CustomID:CustomIDHelpers.CreateButtonID("submit-guess"))
                })
            });

            if (more_riddles)
            {
                return (Result)await _feedback.SendContextualEmbedAsync(new Embed(Title: "Riddle", Description: _game.ActiveRiddle?.Text ?? "Error getting text"), options: button_options);

            }
            return (Result)await _feedback.SendContextualMessageAsync(new FeedbackMessage("No more remaining riddles.", Colour: Color.Black));
        }

        [Command("NextRiddle")]
        [Description("Starts the riddle game")]
        public async Task<IResult> NextRiddle()
        {
            _game.MoveActiveToUsed();
            bool result = _game.SetNextActive();
            if (result)
            {
                var button_options = new FeedbackMessageOptions(MessageComponents: new IMessageComponent[]
{
                new ActionRowComponent(new[]
                {
                    new ButtonComponent(Style:ButtonComponentStyle.Primary, "Submit a guess", CustomID:CustomIDHelpers.CreateButtonID("submit-guess"))
                })
});


                return (Result)await _feedback.SendContextualEmbedAsync( new Embed(Title: "Riddle", Description: _game.ActiveRiddle?.Text ?? "Error getting text"), options: button_options);

            }
            else
            {
                return (Result)await _feedback.SendContextualMessageAsync(new FeedbackMessage("No more remaining riddles.", Colour: Color.Black));
            }
        }

        [Command("Stop")]
        [Description("Stops the riddle game")]
        public async Task<IResult> StopGame()
        {
            _game.StopGame();


            return (Result)await _feedback.SendContextualMessageAsync(new FeedbackMessage("Game stopped.", Colour: Color.Brown));
        }

        [Command("ReloadRiddles")]
        [Description("updates remaining riddles based on riddles.json")]
        [Ephemeral]
        public async Task<IResult> ReloadRiddles()
        {
            _game.ReloadRiddles();
            string emb_description = $"reloaded riddles from files.";
            Embed emb = new(Description:emb_description);
            return (Result)await _feedback.SendContextualEmbedAsync(emb);
        }

        [Command("Clearused")]
        [Description("Clears used riddles in used_riddles.json and adds them back into the current riddle game.")]
        [Ephemeral]
        public async Task<IResult> ClearUsed()
        {
            string emb_description;
            try
            {
                _game.ClearUsedRiddles();
                emb_description = $"Cleared used riddles";
            }
            catch (Exception e)
            {
                emb_description = $"Failed to clear riddles {e.Message}";
            }
            Embed emb = new(Description: emb_description);
            return (Result)await _feedback.SendContextualEmbedAsync(emb);
        }
    }
}
