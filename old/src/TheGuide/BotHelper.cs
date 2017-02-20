using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TheGuide
{
    public class BotHelper
    {
        private DiscordSocketClient _client;
        internal ChannelHelper ChannelHelper;
        internal LoggerHelper LoggerHelper;

        public BotHelper(DiscordSocketClient client)
        {
            _client = client;
            ChannelHelper = new ChannelHelper(_client);
            LoggerHelper = new LoggerHelper(_client);
        }
    }

    internal class ChannelHelper
    {
        private DiscordSocketClient _client;

        public ChannelHelper(DiscordSocketClient client)
        {
            _client = client;
        }

        public IEnumerable<IReadOnlyCollection<Tuple<IGuild, IChannel>>> FindGuildChannel(params string[] args)
        {
            var list = new List<Tuple<IGuild, IChannel>>();
            foreach (var guild in _client.Guilds)
            {
                foreach (var arg in args)
                {
                    var query = guild.Channels.FirstOrDefault(c => c.Name.ToUpper() == arg.ToUpper());
                    if (query != null)
                        list.Add(new Tuple<IGuild, IChannel>(guild as IGuild, query as IChannel));
                }
            }
            yield return list;
        }
    }

    internal class LoggerHelper
    {
        private DiscordSocketClient _client;

        public LoggerHelper(DiscordSocketClient client)
        {
            _client = client;
        }
    }
}
