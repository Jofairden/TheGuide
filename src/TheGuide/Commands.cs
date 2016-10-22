using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TheGuide
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

        internal async Task QuickSend(CommandContext context, string msg, string opt = null)
        {
            var output = await context.Channel?.SendMessageAsync(msg);
            if (opt != null)
            {
                Console.WriteLine(opt);
                var optContent = CommandHandler.SplitOpt(opt);
                if (optContent.Any(x => x[0] == 'd'))
                {
                    await Task.Delay(CommandHandler.delNotifDelay);
                    output?.DeleteAsync();
                }
            }
        }

        [Command("disconnect")]
        [RequireContext(ContextType.DM)]
        [Summary("Disconnects the bot. Can only be performed by bot owner.")]
        public async Task disconnect([Remainder] string opt = null)
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            if (Context.User.Id == application.Owner.Id)
            {
                while (true)
                {
                    try
                    {
                        Context.Client.Dispose();
                        Process.GetCurrentProcess().Kill();
                        await Context.Client.DisconnectAsync();
                        Process.GetCurrentProcess().WaitForExit();
                        break;
                    }
                    catch
                    {
                        await Task.Delay(5000);
                    }
                }
            }
        }

        [Command("ping")]
        [Alias("status", "rs")]
        [Summary("Returns the bot response time")]
        public async Task ping([Remainder] string opt = null)
        {
            await QuickSend(Context, 
                $"My response time is ``{(Context.Client as DiscordSocketClient)?.Latency} ms``",
                opt);
        }

        [Command("helpdev")]
        [Alias("hd")]
        public async Task helpdev([Remainder] string opt = null)
        {
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == "DEVELOPERS");
            string txt = role != null && role.IsMentionable ? role.Mention : "@DEVELOPERS";
            await QuickSend(Context, 
                $"**Regarding asking questions to Terraria developers**\nAsking {txt} about vanilla stuff (as in: questions/suggestions/asking about future stuff etc.): They **WILL NOT** respond, and please **DO NOT** even try starting a discussion. Thank you.",
                opt);
        }

        [Command("helpcode")]
        [Alias("hc")]
        public async Task helpcode([Remainder] string opt = null)
        {
            var channel = (Context.Guild as SocketGuild)?.Channels.FirstOrDefault(x => x.Name == "help");
            string txt = channel != null ? (channel as SocketTextChannel)?.Mention : "#help";
            await QuickSend(Context, 
                $"**When you require assistance**\nPlease go to {txt}, provide **a stack trace** _along with your code_ posted on pastebin or hastebin. Thank you.",
                opt);
        }

        [Command("help")]
        [Alias("guide", "commands")]
        public async Task help([Remainder] string opt = null)
        {
            string total = "";
            foreach (var command in service.Commands)
            {
                var result = await command.CheckPreconditions(Context, map);
                if (result.IsSuccess)
                    total = String.Join(", ", total, command.Name);
            }
            if (total.Length > 2)
                total = total.Substring(2);
            //var channel = await Context.User?.CreateDMChannelAsync();
            await QuickSend(Context, 
                $"**Usable commands for {Context.User.Username}**\n{total}\n\nFollow the command with ``-d`` to destroy it after {CommandHandler.delNotifDelay / 1000} seconds.",
                opt);
        }

        [Command("info")]
        [Alias("about")]
        [RequireContext(ContextType.DM)]
        public async Task info([Remainder] string opt = null)
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            var client = (Context.Client as DiscordSocketClient);
            await QuickSend(Context,
                $"{Format.Bold("Info")}\n" +
                $"- Author: {application.Owner.Username} (ID {application.Owner.Id})\n" +
                $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                $"- Uptime: {Tools.GetUptime()}\n" +
                $"- Bot version: {Program.version}\n\n" +

                $"{Format.Bold("Stats")}\n" +
                $"- Heap Size: {Tools.GetHeapSize()} MB\n" +
                $"- Guilds: {client?.Guilds.Count}\n" +
                $"- Channels: {client?.Guilds.Sum(g => g.Channels.Count)}" +
                $"- Users: {client?.Guilds.Sum(g => g.Users.Count)}",
                opt
            );
        }

        [Command("github")]
        [Alias("sourcecode", "source", "gh")]
        public async Task github([Remainder] string opt = null)
        {
            await QuickSend(Context, 
                $"<https://github.com/gorateron/theguide-discord>", 
                opt);
        }
    }
}
