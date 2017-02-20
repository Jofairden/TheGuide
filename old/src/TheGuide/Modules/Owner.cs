using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheGuide.Preconditions;

namespace TheGuide.Modules
{
    [OwnerAttr]
    [Name("owner")]
    public class Owner : ModuleBase
    {
        private CommandService service;
        private IDependencyMap map;

        public Owner(CommandService _service, IDependencyMap _map)
        {
            service = _service;
            map = _map;
        }

        [Command("disconnect")]
        [Summary("Disconnects the bot.")]
        public async Task disconnect([Remainder] string opt = null)
        {
            // issue: do not use yet
            Context.Client.Dispose();
            await Context.Client.DisconnectAsync().ConfigureAwait(false);
            await (Context.Client as DiscordSocketClient).LogoutAsync().ConfigureAwait(false);
            await Task.Delay(1500).ConfigureAwait(false);
            Environment.FailFast("");
        }
    }
}
