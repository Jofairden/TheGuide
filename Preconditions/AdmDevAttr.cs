using Discord.Commands;
using Discord;
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace TheGuide.Preconditions
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class AdmDevAttr : PreconditionAttribute
	{
		public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
		{
			var guildUser = context.User as SocketGuildUser;
			//var userRoles = guildUser?.RoleIds.Select(r => context.Guild.GetRole(r));
			var isAdmin = guildUser.Roles.Any(r => r.Permissions.Administrator);
			return Task.FromResult
				(isAdmin || CheckResult(context as CommandContext, command, map)
				? PreconditionResult.FromSuccess() : PreconditionResult.FromError("User does not have sufficient privileges"));
		}

		private bool CheckResult(ICommandContext context, CommandInfo command, IDependencyMap map)
		{
			var roles = context.Guild.Roles.Where(x => new string[] { "developer", "administrator", "moderator", "admin", "mod", "dev" }.Contains(x.Name, StringComparer.CurrentCultureIgnoreCase)).Select(x => x.Id);
			var enumerable = roles as ulong[] ?? roles.ToArray();
			if (enumerable.Count() <= 1) return true;
			var useRoles = (context.User as IGuildUser)?.RoleIds.ToArray();
			return enumerable.Intersect(useRoles).Any();
		}
	}
}
