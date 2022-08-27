using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Interactivity.Extensions;
using Remora.Discord.Hosting.Extensions;
using Riddle_Discord_Bot.Commands;
using Riddle_Discord_Bot.Interactions;
using Remora.Discord.Pagination.Extensions;
using Remora.Discord.API.Objects;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Interactivity;

namespace Riddle_Discord_Bot;


internal class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args)
        .UseConsoleLifetime()
        .Build();

        var services = host.Services;
        var log = services.GetRequiredService<ILogger<Program>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var slashService = services.GetRequiredService<SlashService>();
        var checkSlashSupport = slashService.SupportsSlashCommands();
        var updateSlash = await slashService.UpdateSlashCommandsAsync();
        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
        .AddDiscordService
        (
        services =>
            {
                var configuration = services.GetRequiredService<IConfiguration>();

                return configuration.GetValue<string?>("TOKEN") ??
                    throw new InvalidOperationException
                       (
                        "No token found"
                       );
            }
        )
        .ConfigureServices((_, services) =>
        {
            services.Configure<DiscordGatewayClientOptions>(g => g.Intents |= GatewayIntents.MessageContents);
            services.AddSingleton<IRiddleGame, RiddleGame>();
            services
            .AddDiscordCommands(enableSlash: true)
            .AddCommandTree()
                .WithCommandGroup<RiddleCommands>()
                .Finish()
            .AddInteractivity()
            .AddInteractionGroup<ButtonInteractions>();
            
        })
        .ConfigureLogging(
        c => c
        .AddConsole()
        .AddFilter("System.Net.Http.HttpClient.*.LogicalHandler", LogLevel.Warning)
        .AddFilter("System.Net.Http.HttpClient.*.ClientHandler", LogLevel.Warning)
        );
}