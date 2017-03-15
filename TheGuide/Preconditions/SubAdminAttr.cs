using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using TheGuide.Systems;

namespace TheGuide.Preconditions
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class SubAdminAttr : PreconditionAttribute
	{
		public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
		{
			var guildUser = context.User as SocketGuildUser;
			var serverJson = SubSystem.LoadSubServerJson(context.Guild.Id);
			var isAdmin = guildUser.GuildPermissions.Administrator;
			var hasPrivileges = serverJson.AdminRoles.Count(x => guildUser.Roles.Any(r => r.Id == x)) > 0;
			return Task.FromResult
				(isAdmin || hasPrivileges
				? PreconditionResult.FromSuccess() : PreconditionResult.FromError("User does not have sufficient privileges"));
		}
	}
}
