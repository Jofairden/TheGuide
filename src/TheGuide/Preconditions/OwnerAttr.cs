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
        public async override Task<PreconditionResult> CheckPermissions
            (CommandContext context, CommandInfo command, IDependencyMap map)
        {
            return await Task.FromResult
                (await CheckResult(context, command, map).ConfigureAwait(false)
                     ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("User is not the bot owner.")).ConfigureAwait(false);
        }

        private async Task<bool> CheckResult(CommandContext context, CommandInfo command, IDependencyMap map)
        {
            var info = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
            return context.User.Id == info.Owner.Id;
        }
    }
}
