using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyAwesomeBot.Commands;
using MyAwesomeBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

public class Bot
{
    public DiscordClient Client { get; private set; }
    public CommandsNextExtension Commands { get; private set; }

    public async Task RunAsync()
    {
        var configJson = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var discordConfig = new DiscordConfiguration
        {
            Token = configJson["Token"],
            TokenType = TokenType.Bot,
            AutoReconnect = true,
            MinimumLogLevel = LogLevel.Information,
            Intents = DiscordIntents.All
        };

        Client = new DiscordClient(discordConfig);
        Client.Ready += OnClientReady;
        Client.GuildMemberAdded += OnGuildMemberAdded;
        Client.GuildMemberRemoved += OnGuildMemberRemoved;
        Client.MessageDeleted += OnMessageDeleted;
        Client.MessageUpdated += OnMessageUpdated;
        Client.UseInteractivity(new InteractivityConfiguration
        {
            Timeout = TimeSpan.FromMinutes(5)
        });

        var services = new ServiceCollection()
            .AddSingleton<ServerConfigService>()
            .BuildServiceProvider();

        var commandsConfig = new CommandsNextConfiguration
        {
            StringPrefixes = new[] { configJson["Prefix"] },
            EnableDms = false,
            EnableMentionPrefix = true,
            Services = services,
            EnableDefaultHelp = false,
        };

        Commands = Client.UseCommandsNext(commandsConfig);
        Commands.RegisterCommands<AdminCommands>();
        Commands.RegisterCommands<HelpCommand>();
        Commands.RegisterCommands<ModerationCommands>();
        var configService = services.GetRequiredService<ServerConfigService>();
        await configService.LoadConfigsAsync();
        await Client.ConnectAsync();
        await Task.Delay(-1);
    }

    private Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
    {
        sender.Logger.LogInformation("Client is ready to process events.");
        return Task.CompletedTask;
    }

    private async Task OnGuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
    {
        var configService = (ServerConfigService)Commands.Services.GetService(typeof(ServerConfigService));
        var config = configService.GetServerConfig(e.Guild.Id);
        if (config.WelcomeChannelId.HasValue)
        {
            var channel = await sender.GetChannelAsync(config.WelcomeChannelId.Value);
            if (channel != null)
            {
                var welcomeMessage = string.IsNullOrEmpty(config.WelcomeMessage)
                    ? $"Welcome to {e.Guild.Name}, {e.Member.Mention}!"
                    : config.WelcomeMessage.Replace("{user}", e.Member.Mention).Replace("{server}", e.Guild.Name);
                await channel.SendMessageAsync(welcomeMessage);
            }
        }
    }

    private async Task OnGuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
    {
        var configService = (ServerConfigService)Commands.Services.GetService(typeof(ServerConfigService));
        var config = configService.GetServerConfig(e.Guild.Id);
        if (config.LogChannelId.HasValue)
        {
            var logChannel = await sender.GetChannelAsync(config.LogChannelId.Value);
            if (logChannel != null)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle("Member Left")
                    .WithDescription($"{e.Member.Username}#{e.Member.Discriminator} ({e.Member.Id}) has left the server.")
                    .WithColor(DiscordColor.DarkGray)
                    .WithTimestamp(DateTime.UtcNow);
                await logChannel.SendMessageAsync(embed: embed);
            }
        }
    }

    private async Task OnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
    {
        if (e.Guild == null || e.Message.Author?.IsBot != false) return;
        var configService = (ServerConfigService)Commands.Services.GetService(typeof(ServerConfigService));
        var config = configService.GetServerConfig(e.Guild.Id);
        if (config.LogChannelId.HasValue)
        {
            var logChannel = await sender.GetChannelAsync(config.LogChannelId.Value);
            if (logChannel != null)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle("Message Deleted")
                    .WithDescription($"**Author:** {e.Message.Author.Mention}\n**Channel:** {e.Channel.Mention}\n**Content:**\n{e.Message.Content}")
                    .WithColor(DiscordColor.Red)
                    .WithTimestamp(DateTime.UtcNow);
                await logChannel.SendMessageAsync(embed: embed);
            }
        }
    }

    private async Task OnMessageUpdated(DiscordClient sender, MessageUpdateEventArgs e)
    {
        if (e.Guild == null || e.Author.IsBot || e.MessageBefore == null) return;
        if (e.MessageBefore.Content == e.Message.Content) return;
        var configService = (ServerConfigService)Commands.Services.GetService(typeof(ServerConfigService));
        var config = configService.GetServerConfig(e.Guild.Id);

        if (config.LogChannelId.HasValue)
        {
            var logChannel = await sender.GetChannelAsync(config.LogChannelId.Value);
            if (logChannel != null)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle("Message Edited")
                    .WithDescription($"**Author:** {e.Author.Mention}\n**Channel:** {e.Channel.Mention}\n[Jump to Message]({e.Message.JumpLink})")
                    .AddField("Before", e.MessageBefore.Content)
                    .AddField("After", e.Message.Content)
                    .WithColor(DiscordColor.Yellow)
                    .WithTimestamp(DateTime.UtcNow);
                await logChannel.SendMessageAsync(embed: embed);
            }
        }
    }
}