using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace TheGuide.Preconditions
{
	[AttributeUsage(AttributeTargets.Method)]
	public class OwnerAttribute : PreconditionAttribute
	{
		private IApplication _appInfo;

		public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
		{
			if (_appInfo != null)
				return await Task.FromResult(
					context.User.Id == _appInfo.Owner.Id
						? PreconditionResult.FromSuccess()
						: PreconditionResult.FromError("You are not the bot owner.")).ConfigureAwait(false);

			var applicationInfoAsync = (context as SocketCommandContext)?.Client.GetApplicationInfoAsync();
			if (applicationInfoAsync != null)
				_appInfo = await applicationInfoAsync;

			return await Task.FromResult(
				_appInfo != null && context.User.Id == _appInfo.Owner.Id
				? PreconditionResult.FromSuccess() : PreconditionResult.FromError("You are not the bot owner.")).ConfigureAwait(false);
		}
	}
}
