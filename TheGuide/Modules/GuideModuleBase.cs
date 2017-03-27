using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace TheGuide.Modules
{
	public class SocketGuildCommandContext : SocketCommandContext
	{
		public SocketGuild SocketGuild => Guild as SocketGuild;

		public SocketGuildCommandContext(DiscordSocketClient client, SocketUserMessage msg) : base(client, msg)
		{

		}
	}

	public class GuideModuleBase<T> : ModuleBase<T> where T : class, ICommandContext
	{
		protected override Task<IUserMessage> ReplyAsync(string message, bool isTTS = false, Embed embed = null, RequestOptions options = null) =>
			base.ReplyAsync(message.Unmention(), isTTS, embed, options);
	}
}
