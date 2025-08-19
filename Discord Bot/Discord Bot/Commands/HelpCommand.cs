using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAwesomeBot.Commands
{
    public class HelpCommand : BaseCommandModule
    {
        [Command("help")]
        [Description("Displays information about available commands.")]
        public async Task Help(CommandContext ctx, [RemainingText] string commandName = null)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                var embed = new DiscordEmbedBuilder().WithTitle("Help - Command List").WithDescription($"Use `{ctx.Prefix}help [command]` for more info on a specific command.").WithColor(DiscordColor.CornflowerBlue);
                var commands = ctx.CommandsNext.RegisteredCommands.Values.Distinct();
                var commandsByModule = commands.GroupBy(c => c.Module.ModuleType.Name).OrderBy(g => g.Key);
                foreach (var group in commandsByModule)
                {
                    var commandList = string.Join(", ", group.Select(c => $"`{c.Name}`"));
                    embed.AddField(group.Key.Replace("Commands", " Commands"), commandList);
                }
                await ctx.RespondAsync(embed: embed);
            }
            else
            {
                var command = ctx.CommandsNext.FindCommand(commandName, out var args);
                if (command == null)
                {
                    await ctx.RespondAsync($"Command `{commandName}` not found.");
                    return;
                }
                var helpBuilder = new StringBuilder();
                helpBuilder.AppendLine($"**Description:** {command.Description ?? "No description available."}");
                if (command.Aliases.Any())
                {
                    helpBuilder.AppendLine($"**Aliases:** {string.Join(", ", command.Aliases.Select(a => $"`{a}`"))}");
                }
                var embed = new DiscordEmbedBuilder().WithTitle($"Help: `{command.Name}`").WithDescription(helpBuilder.ToString()).WithColor(DiscordColor.Teal);
                await ctx.RespondAsync(embed: embed);
            }
        }
    }
}