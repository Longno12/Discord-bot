using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace MyAwesomeBot.Commands
{
    public class ModerationCommands : BaseCommandModule
    {
        [Command("kick")]
        [Description("Kicks a member from the server.")]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task Kick(CommandContext ctx, DiscordMember member, [RemainingText] string reason = "No reason provided.")
        {
            await member.RemoveAsync(reason);
            await ctx.RespondAsync($"Successfully kicked {member.DisplayName}. Reason: {reason}");
        }

        [Command("ban")]
        [Description("Bans a member from the server.")]
        [RequireUserPermissions(Permissions.BanMembers)]
        public async Task Ban(CommandContext ctx, DiscordMember member, [RemainingText] string reason = "No reason provided.")
        {
            await member.BanAsync(0, reason);
            await ctx.RespondAsync($"Successfully banned {member.DisplayName}. Reason: {reason}");
        }

        [Command("clear")]
        [Description("Deletes a specified number of messages from the current channel.")]
        [RequireUserPermissions(Permissions.ManageMessages)]
        public async Task Clear(CommandContext ctx, int amount)
        {
            if (amount <= 0 || amount > 100)
            {
                await ctx.RespondAsync("Please provide an amount between 1 and 100.");
                return;
            }
            var messages = await ctx.Channel.GetMessagesAsync(amount + 1);
            await ctx.Channel.DeleteMessagesAsync(messages);
            var confirmation = await ctx.RespondAsync($"Deleted {amount} messages.");
            await Task.Delay(3000);
            await confirmation.DeleteAsync();
        }
    }
}