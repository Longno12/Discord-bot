using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using MyAwesomeBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyAwesomeBot.Commands
{
    public class AdminCommands : BaseCommandModule
    {
        public ServerConfigService ConfigService { get; set; }

        [Command("setup")]
        [Description("Opens an interactive setup menu for the bot.")]
        [RequireOwner]
        public async Task Setup(CommandContext ctx)
        {
            var config = ConfigService.GetServerConfig(ctx.Guild.Id);
            var logButtonId = $"setup_log_{ctx.Message.Id}";
            var welcomeButtonId = $"setup_welcome_{ctx.Message.Id}";
            var saveButtonId = $"setup_save_{ctx.Message.Id}";
            var logButton = new DiscordButtonComponent(ButtonStyle.Primary, logButtonId, "Set Log Channel");
            var welcomeButton = new DiscordButtonComponent(ButtonStyle.Success, welcomeButtonId, "Set Welcome Channel");
            var saveButton = new DiscordButtonComponent(ButtonStyle.Danger, saveButtonId, "Save & Exit");

            DiscordEmbedBuilder GenerateEmbed()
            {
                var logChannelName = config.LogChannelId.HasValue ? $"<#{config.LogChannelId.Value}>" : "Not Set";
                var welcomeChannelName = config.WelcomeChannelId.HasValue ? $"<#{config.WelcomeChannelId.Value}>" : "Not Set";
                return new DiscordEmbedBuilder()
                    .WithTitle("🤖 Bot Setup (Legacy Mode)")
                    .WithDescription("Use the buttons below to configure the bot. After clicking, the bot will ask you to mention the channel in chat.")
                    .AddField("Logging Channel", logChannelName, true)
                    .AddField("Welcome Channel", welcomeChannelName, true)
                    .WithColor(DiscordColor.Azure);
            }
            var setupMessage = await new DiscordMessageBuilder().WithEmbed(GenerateEmbed()).AddComponents(logButton, welcomeButton, saveButton).SendAsync(ctx.Channel);
            var tcs = new TaskCompletionSource<bool>();
            async Task ComponentInteractionHandler(DiscordClient sender, ComponentInteractionCreateEventArgs e)
            {
                if (e.User.Id != ctx.User.Id || e.Message.Id != setupMessage.Id) return;
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                if (e.Id == logButtonId || e.Id == welcomeButtonId)
                {
                    var target = (e.Id == logButtonId) ? "log" : "welcome";
                    await ctx.Channel.SendMessageAsync($"Please mention the text channel you want to set as the **{target}** channel. (e.g., #general)");
                    var interactivity = ctx.Client.GetInteractivity();
                    var channelMessageResult = await interactivity.WaitForMessageAsync(m => m.Author.Id == ctx.User.Id && m.ChannelId == ctx.Channel.Id && m.MentionedChannels.Any(),TimeSpan.FromMinutes(1));
                    if (!channelMessageResult.TimedOut)
                    {
                        var mentionedChannel = channelMessageResult.Result.MentionedChannels.First();
                        if (target == "log") config.LogChannelId = mentionedChannel.Id;
                        else config.WelcomeChannelId = mentionedChannel.Id;
                        await channelMessageResult.Result.DeleteAsync();
                        var msgs = await ctx.Channel.GetMessagesAsync(1);
                        await msgs.First().DeleteAsync();
                        await setupMessage.ModifyAsync(b => b.WithEmbed(GenerateEmbed()));
                    }
                }
                else if (e.Id == saveButtonId)
                {
                    await ConfigService.SaveConfigsAsync();
                    await setupMessage.DeleteAsync();
                    await ctx.RespondAsync("✅ Configuration saved successfully!");
                    tcs.TrySetResult(true);
                }
            }
            ctx.Client.ComponentInteractionCreated += ComponentInteractionHandler;
            await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(5)));
            ctx.Client.ComponentInteractionCreated -= ComponentInteractionHandler;

            if (!tcs.Task.IsCompleted)
            {
                await setupMessage.ModifyAsync(b => b.WithContent("Setup timed out.").ClearComponents());
            }
        }
    }
}