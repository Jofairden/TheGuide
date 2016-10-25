using Discord.Commands;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TheGuide.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdmDevAttr : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(CommandContext context, CommandInfo command, IDependencyMap map)
        {
            return Task.FromResult
                (CheckResult(context, command, map)
                ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("User does not have the admin or developer role"));
        }

        private bool CheckResult(CommandContext context, CommandInfo command, IDependencyMap map)
        {
            var roles = context.Guild.Roles.Where(x => new string[] { "DEVELOPER", "ADMINISTRATOR" }.Contains(x.Name.ToUpper())).Select(x => x.Id);
            if (roles.Count() > 1)
            {
                var useRoles = (context.User as IGuildUser)?.RoleIds.ToArray();
                return roles.Intersect(useRoles).Count() > 0;
            }
            return true;
        }
    }
}
