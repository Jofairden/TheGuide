using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using TheGuide.Preconditions;
using TheGuide.Systems;
using TheGuide.Systems.Snowflake;

namespace TheGuide.Modules
{
	[Group("config")]
	[Name("config")]
	public class Config : ModuleBase
	{
		private readonly CommandService service;
		private readonly IDependencyMap map;

		public Config(CommandService service, IDependencyMap map)
		{
			this.service = service;
			this.map = map;
		}

		[Command("setrole")]
		[ConfAdmAttr]
		public async Task SetRole([Remainder] IRole role)
		{
			var config = ConfigSystem.config(Context.Guild.Id);
			if (config.admRoles.Contains(role.Id))
				await AddRole(role);
			else
				await RemoveRole(role);
		}

		[Command("addrole")]
		[Alias("makerole")]
		[ConfAdmAttr]
		public async Task AddRole([Remainder] IRole role)
		{
			var config = ConfigSystem.config(Context.Guild.Id);
			if (!config.admRoles.Contains(role.Id))
			{
				config.admRoles.Add(role.Id);
				await ConfigSystem.WriteConfig(Context.Guild.Id, config);
				await ReplyAsync($"``{role.Name}`` is now a config admin role.");
			}
		}

		[Command("removerole")]
		[Alias("remrole", "delrole", "deleterole")]
		[ConfAdmAttr]
		public async Task RemoveRole([Remainder] IRole role)
		{
			var config = ConfigSystem.config(Context.Guild.Id);
			if (config.admRoles.Contains(role.Id))
			{
				config.admRoles.Remove(role.Id);
				await ConfigSystem.WriteConfig(Context.Guild.Id, config);
				await ReplyAsync($"``{role.Name}`` is no longer a config admin role.");
			}
		}
	}
}
