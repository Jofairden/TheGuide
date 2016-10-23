using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TheGuide
{
    public class CommandHandler
    {
        public static int cdDelay = 2500;
        public static int delNotifDelay = 5000;
        public static char prefixChar = '?';
        private CommandService service;
        private DiscordSocketClient client;
        private IDependencyMap map;
        private Dictionary<ulong, DateTime> cooldowns;

        public async Task Install(IDependencyMap _map)
        {
            client = _map.Get<DiscordSocketClient>();
            cooldowns = _map.Get<Dictionary<ulong, DateTime>>();
            service = new CommandService();
            _map.Add(service);
            map = _map;

            await service.AddModules(Assembly.GetEntryAssembly());

            client.MessageReceived += HandleCommand;
        }

        public async Task HandleCommand(SocketMessage parameterMessage)
        {
            var message = parameterMessage as SocketUserMessage;
            int argPos = 0;
            var cleanmsg = new string(message.Content.ToCharArray().Where(x => !Char.IsWhiteSpace(x)).ToArray());
            if (message == null || cleanmsg.Length <= 1 || (!(message.HasMentionPrefix(client.CurrentUser, ref argPos) || message.HasCharPrefix(prefixChar, ref argPos))))
                return;

            var cooldownTime = cooldowns.FirstOrDefault(x => x.Key == message.Author.Id);
            if (cooldownTime.Key != default(ulong))
            {
                if (cooldownTime.Value > DateTime.Now)
                {
                    await message?.DeleteAsync();
                    return;
                }
                else
                    cooldowns.Remove(cooldownTime.Key);
            }

            var context = new CommandContext(client, message);
            var result = await service.Execute(context, argPos, map);

            if (!result.IsSuccess)
            {
                //var channel = await context.User?.CreateDMChannelAsync();
                //await channel?.SendMessageAsync($"**Error** (on command <{message.Content}>): {result.ToString()}");
                await Commands.QuickSend(context, $"**Error** (on command <{message.Content}>): {result.ToString()}", "-d");
            }
            else
            {
                cooldowns.Add(message.Author.Id, DateTime.Now.AddMilliseconds(cdDelay));
                string[] opt = SplitOpt(message.ToString());
                if (opt.Any(x => x[0] == 'd'))
                {
                    message?.DeleteAsync();
                }
            }
        }

        public static string[] SplitOpt(string opt)
        {
            //int index = opt.ToString().IndexOf('-');
            //if (index != -1)
            //{
            //    string optContent = opt.ToString().Substring(index);
            //    string[] optMsg = opt.ToString().Split('-');
            //    optMsg = optMsg.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            //    return optMsg;
            //}
            //return new string[0];
            string[] opts = opt.Split('-');
            opts = opts.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            return opts;
        }
    }
}
