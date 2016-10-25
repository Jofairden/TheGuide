using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TheGuide.Preconditions;

namespace TheGuide.Modules
{
    public class Commands : ModuleBase
    {
        private CommandService service;
        private IDependencyMap map;

        public Commands(CommandService _service, IDependencyMap _map)
        {
            service = _service;
            map = _map;
        }

        [Command("version")]
        [Alias("-v")]
        [Summary("returns the bot version")]
        public async Task version([Remainder] string rem = null)
        {
            await ReplyAsync(
                $"Bot version: ``{Program.version}``");
        }

        [Command("ping")]
        [Alias("status")]
        [Summary("Returns the bot response time")]
        public async Task ping([Remainder] string rem = null)
        {
            await ReplyAsync(
                $"My response time is ``{(Context.Client as DiscordSocketClient)?.Latency} ms``");
        }

        [Command("helpdev")]
        [Alias("hd")]
        [Summary("Quickly show a message which contains info about asking questions to developers")]
        public async Task helpdev([Remainder] string rem = null)
        {
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToUpper() == "DEVELOPERS");
            string roleMention = role != null && role.IsMentionable ? role.Mention : "@DEVELOPERS";
            await ReplyAsync(
                $"**Regarding asking questions to Terraria developers**\n" +
                $"Asking {roleMention} about vanilla stuff (as in: questions/suggestions/asking about future stuff etc.): They **WILL NOT** respond, and please **DO NOT** even try starting a discussion. Thank you.");
        }

        [Command("helpcode")]
        [Alias("hc")]
        [Summary("Quickly show a message which contains info for the help chat.")]
        public async Task helpcode([Remainder] string rem = null)
        {
            var channel = (Context.Guild as SocketGuild)?.Channels.FirstOrDefault(x => x.Name.ToUpper() == "HELP");
            string channelMention = channel != null ? (channel as SocketTextChannel)?.Mention : "#help";
            await ReplyAsync(
                $"**When you require assistance**\n" +
                $"Please go to {channelMention}, provide **a stack trace** _along with your code_ posted on pastebin or hastebin. Thank you.");
        }

        // forgive me for this piece of crap, I don't feel motivated to make this a proper piece of code atm.
        // also, I believe it does not check aliases
        [Command("help")]
        [Alias("guide")]
        [Summary("Shows info about commands")]
        public async Task help([Remainder] string rem = null)
        {
            string total = "";
            if (rem != null)
            {
                if (rem.Any(x => char.IsWhiteSpace(x)))
                {
                    var split = rem.Split(' ');
                    if (split.Any())
                    {
                        var module = split[0];
                        var command = split[1];
                        var cmd = service.Commands.FirstOrDefault(x => x.Module.Prefix.ToUpper() == module.ToUpper() && x.Name.ToUpper() == command.ToUpper());
                        if (cmd != null)
                        {
                            string summ = cmd.Summary.Length > 0 ? cmd.Summary : "no summary";
                            await ReplyAsync($"**Command info for** ``{module} {command}``\n" +
                                $"({module}) **{command}**: {summ}");
                        }
                        else
                            await ReplyAsync($"Command ``{command}`` from module ``{module}`` not found.");
                        return;
                    }
                }
                var cmds = service.Commands.Where(x => x.Module.Prefix.ToUpper() == rem.ToUpper());
                if (cmds.Any())
                {
                    foreach (var command in cmds)
                    {
                        var result = await command.CheckPreconditions(Context, map);
                        if (result.IsSuccess)
                            total = String.Join(", ", total, command.Name);
                    }
                    if (total.Length > 2)
                        total = total.Substring(2);
                    await ReplyAsync($"**Usable commands for {Context.User.Username}**\n" +
                        $"**{rem}**: {total}");
                }
                else
                {
                    var cmd = service.Commands.FirstOrDefault(x => x.Name.ToUpper() == rem.ToUpper());
                    if (cmd != null && cmd.Module.Prefix == "")
                    {
                        await ReplyAsync($"**Command info for ``{cmd.Name}``**\n" +
                            $"{cmd.Text} (module: {cmd.Module}): {cmd.Summary}");
                    }
                    else
                    {
                        await ReplyAsync($"Command ``{rem}`` not found");
                    }
                }
                return;
            }
            foreach (var command in service.Commands)
            {
                if (command.Module.Prefix == "")
                {
                    var result = await command.CheckPreconditions(Context, map);
                    if (result.IsSuccess)
                        total += command.Name + ", ";
                }
            }
            total = Tools.TruncateString(total, total.Length - 2);
            string modules = "";
            foreach (var module in service.Modules)
            {
                if (!new string[] { "COMMANDS", "OWNER" }.Any(x => x == module.Name.ToUpper()) && module.Commands.Any())
                {
                    modules += $"\n_{module.Name}_: ";
                    foreach (var command in module.Commands)
                    {
                        modules += $"{command.Name}, ";
                    }
                    modules = Tools.TruncateString(modules, modules.Length - 2);
                }
            }

            await ReplyAsync($"**Usable commands for {Context.User.Username}**\n" +
                $"{total}" +
                $"{modules}\n\n" +
                $"Get command specific help: ``{CommandHandler.prefixChar}help <name>`` or ``{CommandHandler.prefixChar}help <module> <name>``");
        }

        [Command("info")]
        [Alias("about")]
        [Summary("Shows elaborate bot info")]
        [RequireContext(ContextType.DM)]
        public async Task info([Remainder] string rem = null)
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            var client = (Context.Client as DiscordSocketClient);
            await ReplyAsync($"{Format.Bold("Info")}\n" +
                $"- Author: {application.Owner.Username} (ID {application.Owner.Id})\n" +
                $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                $"- Uptime: {Tools.GetUptime()}\n" +
                $"- Bot version: {Program.version}\n\n" +

                $"{Format.Bold("Stats")}\n" +
                $"- Heap Size: {Tools.GetHeapSize()} MB\n" +
                $"- Guilds: {client?.Guilds.Count}\n" +
                $"- Channels: {client?.Guilds.Sum(g => g.Channels.Count)}\n" +
                $"- Users: {client?.Guilds.Sum(g => g.Users.Count)}"
            );
        }

        [Command("github")]
        [Alias("sourcecode", "source", "gh")]
        [Summary("Shows a link to the code of this bot")]
        public async Task github([Remainder] string rem = null)
        {
            await ReplyAsync(
                $"Here's how I am made! <https://github.com/gorateron/theguide-discord>");
        }

        [Command("links")]
        [Alias("quicklink", "ql")]
        [Summary("Shows easy tml links")]
        public async Task links([Remainder] string rem = null)
        {
            var updateschannel = (Context.Guild as SocketGuild)?.Channels.FirstOrDefault(x => x.Name.ToUpper() == "TMODLOADER-UPDATES") as ITextChannel;
            var faqchannel = (Context.Guild as SocketGuild)?.Channels.FirstOrDefault(x => x.Name.ToUpper() == "FAQ") as ITextChannel;
            await ReplyAsync(
                $"**tML thead**: <http://forums.terraria.org/index.php?threads/1-3-tmodloader-a-modding-api.23726/>\n" +
                $"**tML github**: <https://github.com/bluemagic123/tModLoader/>\n" +
                $"**tML releases**: <https://github.com/bluemagic123/tModLoader/releases>\n" +
                $"**tML updates**: " + (updateschannel != null ? updateschannel.Mention : "#tmodloader-updates") + "\n" +
                $"**FAQ**: " + (faqchannel != null ? faqchannel.Mention : "#faq"));
        }
    }
}
