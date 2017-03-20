using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace TheGuide.Modules
{
    public class GuideModuleBase<T> : ModuleBase where T: ICommandContext
    {
		protected override Task<IUserMessage> ReplyAsync(string message, bool isTTS = false, Embed embed = null, RequestOptions options = null) =>
			base.ReplyAsync(message.Unmention(), isTTS, embed, options);
	}
}
