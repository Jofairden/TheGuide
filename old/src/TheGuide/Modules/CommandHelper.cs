using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TheGuide.Preconditions;

namespace TheGuide.Modules
{
    public static class CommandHelper
    {
        //deprecated
        public static async Task QuickSend(CommandContext context, string msg, string opt = null)
        {
            var output = await context.Channel?.SendMessageAsync(msg);
            if (opt != null)
            {
                Console.WriteLine(opt);
                var optContent = CommandHandler.SplitOpt(opt);
                if (optContent.Any(x => x[0] == 'd'))
                {
                    await Task.Delay(CommandHandler.deleteDelay);
                    await output?.DeleteAsync();
                }
            }
        }
    }
}
