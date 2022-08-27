using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Gateway.Responders;
using Remora.Discord.Interactivity;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riddle_Discord_Bot;

internal class PingPongResponder : IResponder<IMessageCreate>
{
    private readonly IDiscordRestChannelAPI _channelAPI;

    public PingPongResponder(IDiscordRestChannelAPI channelAPI)
    {
        _channelAPI = channelAPI;
    }

    async Task<Result> IResponder<IMessageCreate>.RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct)
    {
        if (gatewayEvent.Content == "!start")
        {
            var options = new FeedbackMessageOptions(MessageComponents: new IMessageComponent[]
            {
                new ActionRowComponent(new[]
                {
                    new ButtonComponent(Style:ButtonComponentStyle.Primary, "click me!", CustomID:CustomIDHelpers.CreateButtonID("my-button"))
                })
            });


            var embed = new Embed(Description: "This could  be riddle data", Colour: Color.LawnGreen);
            return (Result)await _channelAPI.CreateMessageAsync(gatewayEvent.ChannelID, embeds: new[] { embed }, ct: ct, components: options.MessageComponents);
        }
        return Result.FromSuccess();
    }
}
