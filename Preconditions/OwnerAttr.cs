using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TheGuide.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class OwnerAttr : PreconditionAttribute
    {
        IApplication _appInfo;

		public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
		{
			if (_appInfo == null)
				_appInfo = await (context as CommandContext)?.Client.GetApplicationInfoAsync();

			return await Task.FromResult(
				context.User.Id == _appInfo.Owner.Id
				? PreconditionResult.FromSuccess() : PreconditionResult.FromError("You are not the bot owner.")).ConfigureAwait(false);
		}
    }
}
