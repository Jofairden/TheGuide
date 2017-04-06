using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace TheGuide.Modules
{
	public class GuideModuleBase<T> : ModuleBase<T> where T : class, ICommandContext
	{
		//property injection fixed in 00681
		public IDependencyMap DependencyMap { get; set; }
		public DiscordSocketClient Client { get; set; }
		public CommandService CommandService { get; set; }

		// You can override ReplyAsync, do some stuff.
		protected override Task<IUserMessage> ReplyAsync(string message, bool isTTS = false, Embed embed = null,
			RequestOptions options = null)
		{
			// Try to replace @everyone or @here if present
			// \x200B is a no-width blank space
			// By inserting it we basically sabotage the mentions
			return base.ReplyAsync(
				message.Replace(Context.Guild.EveryoneRole.Mention, "@every\x200Bone").Replace("@here", "@he\x200Bre"), isTTS,
				embed, options);
		}
	}
}
