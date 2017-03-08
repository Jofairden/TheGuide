using Discord.Commands;
using Discord;
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using TheGuide.Systems;

namespace TheGuide.Preconditions
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class ConfAdmAttr : PreconditionAttribute
	{
		public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
		{
			var guildUser = context.User as SocketGuildUser;
			var isAdmin = guildUser.GuildPermissions.Administrator;

			return Task.FromResult
				(isAdmin || CheckResult(context as CommandContext, command, map)
				? PreconditionResult.FromSuccess() : PreconditionResult.FromError("User does not have sufficient privileges"));
		}

		private bool CheckResult(ICommandContext context, CommandInfo command, IDependencyMap map)
		{
			var configRoles = 
				ConfigSystem.config(context.Guild.Id)?.admRoles.ToArray();

			var userRoles = 
				(context.User as IGuildUser)?.RoleIds.ToArray();

			return configRoles.Intersect(userRoles).Any();
		}
	}
}
