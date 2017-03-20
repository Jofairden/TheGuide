using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using TheGuide.Preconditions;
using TheGuide.Systems;

namespace TheGuide.Modules
{
	[Group("config")]
	[Name("config")]
	public class Config : GuideModuleBase<SocketCommandContext>
	{
		private readonly CommandService _service;
		private readonly IDependencyMap _map;

		public Config(CommandService service, IDependencyMap map)
		{
			_service = service;
			_map = map;
		}

		[Command("setrole")]
		[ConfigAdmin]
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
		[ConfigAdmin]
		public async Task AddRole([Remainder] IRole role)
		{
			var config = ConfigSystem.config(Context.Guild.Id);
			if (!config.admRoles.Contains(role.Id))
			{
				config.admRoles.Add(role.Id);
				await ConfigSystem.WriteConfig(Context.Guild.Id, config);
				await ReplyAsync($"`{role.Name}` is now a config admin role.");
			}
			else
			{
				await ReplyAsync($"`{role.Name}` was already a config admin role.");
			}
		}

		[Command("removerole")]
		[Alias("remrole", "delrole", "deleterole")]
		[ConfigAdmin]
		public async Task RemoveRole([Remainder] IRole role)
		{
			var config = ConfigSystem.config(Context.Guild.Id);
			if (config.admRoles.Contains(role.Id))
			{
				config.admRoles.Remove(role.Id);
				await ConfigSystem.WriteConfig(Context.Guild.Id, config);
				await ReplyAsync($"`{role.Name}` is no longer a config admin role.");
			}
			else
			{
				await ReplyAsync($"`{role.Name}` is was not a config admin role, impossible to remove.");
			}
		}
	}
}
