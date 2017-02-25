using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheGuide.Preconditions;
using TheGuide.Systems;

namespace TheGuide.Modules
{
	//todo: make sure roles are managed properly
	//descriptions etc.
	//user.json files!!
	[Group("sub")]
	[Name("sub")]
	public class Sub : ModuleBase
	{
		private CommandService service;
		private IDependencyMap map;

		public Sub(CommandService _service, IDependencyMap _map)
		{
			service = _service;
			map = _map;
		}

		[Command, Priority(10)]
		[Summary("Sub yourself to a certain channel")]
		[Remarks("sub\nsub #channelmention -- OR -- sub channelname -- OR -- sub \\#channelid")]
		public async Task SubMe([Remainder] ITextChannel channel = null)
		{
			if (channel == null)
			{
				await service.ExecuteAsync(Context, $"help module:sub", map, MultiMatchHandling.Best);
				return;
			}

			await TrySub(Context.User, channel);
		}

		[Command, Priority(10)]
		[Summary("Sub a user to a certain channel")]
		[Remarks("sub <user> <channel>\nsub Jofaiden #github")]
		[AdmDevAttr]
		public async Task SubUser(IUser user = null, [Remainder] ITextChannel channel = null)
		{
			if (user == null || channel == null)
			{
				await service.ExecuteAsync(Context, $"help module:sub", map, MultiMatchHandling.Best);
				return;
			}

			await TrySub(user, channel);
		}

		[Command("me")]
		[Summary("Sub yourself to a certain channel, finds any channel by name, mention or ID")]
		[Remarks("me <channel>\nme #channelmention --OR-- me channelname --OR-- me \\#channelmention --OR-- me channelid")]
		public async Task Me([Remainder] ITextChannel channel = null)
		{
			if (channel == null)
			{
				await service.ExecuteAsync(Context, $"help sub me", map, MultiMatchHandling.Best);
				return;
			}
			await TrySub(Context.User, channel);
		}

		//[Command("user")]
		//[Summary("Sub someone a certain channel")]
		//[Remarks("user")]
		//[AdmDevAttr]
		//public async Task User(IUser user, [Remainder] ITextChannel channel) =>
		//	await TrySub(user, channel);

		[Command("delete")]
		[Alias("remove")]
		[Summary("Delete a subscription of a channel")]
		[Remarks("delete <channel>\ndelete #github")]
		[AdmDevAttr]
		public async Task Delete(ITextChannel channel = null)
		{
			if (channel == null)
			{
				await service.ExecuteAsync(Context, $"help sub delete", map, MultiMatchHandling.Best);
				return;
			}

			await TryDelete(channel);
		}

		[Command("delete")]
		[Alias("remove")]
		[Summary("Delete a subscription of a channel")]
		[Remarks("delete <role>\ndelete @sub-github")]
		[AdmDevAttr]
		public async Task Delete(IRole role = null)
		{
			if (role == null)
			{
				await service.ExecuteAsync(Context, $"help sub delete", map, MultiMatchHandling.Best);
				return;
			}

			await TryDelete(role);
		}

		[Command("clear")]
		[Summary("Delete all subscriptions for guild")]
		[OwnerAttr]
		public async Task Clear()
		{
			var oldJson = SubSystem.LoadSubServerJson(Context.Guild.Id);
			var totalSubs = oldJson.Data.Count;
			var oldSubs = oldJson.Data.Select(x => $"**{(Context.Guild as SocketGuild)?.TextChannels.FirstOrDefault(c => c.Id == x.Key).Name ?? "null"}**: {Context.Guild.GetRole(x.Value).Name}");
			var json = new SubServerJson(Context.Guild.Id, new Dictionary<ulong, ulong>());
			await SubSystem.CreateServerSub(json.GUID, json);
			var msg = $"Removed {totalSubs} subscription{(totalSubs > 1 ? "s" : "")} from server:\n\n";
			msg += string.Join("\n", oldSubs).Cap(1999 - msg.Length);
			await ReplyAsync(msg);
		}

		[Command("list")]
		public async Task List()
		{
			var oldJson = SubSystem.LoadSubServerJson(Context.Guild.Id);
			var msg = $"Showing {oldJson.Data.Count} subscriptions of {Context.Guild.Name}:\n\n";
			msg += string.Join("\n", oldJson.Data.Select(x => $"**{(Context.Guild as SocketGuild)?.TextChannels.FirstOrDefault(c => c.Id == x.Key).Name ?? "null"}**: {Context.Guild.GetRole(x.Value).Name}"));
			await ReplyAsync(msg);
		}

		[Command("create")]
		[Summary("Create a subscription, finds any channel/role by mention, name or ID")]
		[Remarks("create <channel> <role>\ncreate #general sub-general")]
		[AdmDevAttr]
		public async Task Create(ITextChannel channel = null, IRole role = null)
		{
			if (channel == null || role == null)
			{
				await service.ExecuteAsync(Context, $"help sub create", map, MultiMatchHandling.Best);
				return;
			}

			await TryCreate(channel, role);
		}

		[Command("Create")]
		[Summary("Create a subscription, finds any channel/role by mention, name or ID")]
		[Remarks("create <channel>\ncreate @sub-general")]
		[AdmDevAttr]
		public async Task Create(ITextChannel channel = null)
		{
			if (channel == null)
			{
				await service.ExecuteAsync(Context, $"help sub create", map, MultiMatchHandling.Best);
				return;
			}

			var roleName = $"sub-{channel.Name}";
			var role = Context.Guild.Roles.FirstOrDefault(r => string.Equals(roleName, r.Name, StringComparison.CurrentCultureIgnoreCase));
			if (role == null)
			{
				role = await Context.Guild.CreateRoleAsync(roleName);
				var rolePerms = new OverwritePermissions(readMessages: PermValue.Allow, readMessageHistory: PermValue.Allow);
				await channel.AddPermissionOverwriteAsync(role, rolePerms);
				rolePerms = new OverwritePermissions(readMessages: PermValue.Deny);
				await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, rolePerms);
				await ReplyAsync($"Since no role named ``{roleName}`` was found, I created one with read permission for channel {channel.Mention}");
			}

			await TryCreate(channel, role);
		}

		private async Task TryDelete(ITextChannel channel)
		{
			var serverJson = SubSystem.LoadSubServerJson(Context.Guild.Id);
			var json = new SubServerJson(serverJson.GUID, serverJson.Data);
			if (!json.Data.ContainsKey(channel.Id))
			{
				await ReplyAsync($"Could not find any subscription for {channel.Mention}");
				return;
			}
			json.Data.Remove(channel.Id);
			await SubSystem.CreateServerSub(Context.Guild.Id, json);
			await ReplyAsync($"Removed subscription for {channel.Mention}");
		}

		private async Task TryDelete(IRole role)
		{
			var serverJson = SubSystem.LoadSubServerJson(Context.Guild.Id);
			var json = new SubServerJson(serverJson.GUID, serverJson.Data);
			var keys = json.Data.Where(x => x.Key == role.Id).Select(i => i.Key).ToArray();
			if (keys.Length <= 0)
			{
				await ReplyAsync($"Could not find any subscriptions for role ``{role.Name}``");
				return;
			}
			for (int i = 0; i < keys.Length; i++)
				json.Data.Remove(keys[i]);

			await SubSystem.CreateServerSub(Context.Guild.Id, json);
			await ReplyAsync($"Removed {keys.Length} {(keys.Length > 0 ? "subscriptions" : "subscription")} for role ``{role.Name}``");
		}

		private async Task TryCreate(ITextChannel channel, IRole role)
		{
			var serverJson = SubSystem.LoadSubServerJson(Context.Guild.Id);
			var json = new SubServerJson(serverJson.GUID, serverJson.Data);
			if (json.Data.ContainsKey(channel.Id))
			{
				await ReplyAsync($"{channel.Mention} already has a subcription using role ``{Context.Guild.GetRole(json.Data[channel.Id]).Name}``");
				return;
			}
			json.Data.Add(channel.Id, role.Id);
			await SubSystem.CreateServerSub(Context.Guild.Id, json);
			await ReplyAsync($"Created subscription for {channel.Mention} using role ``{role.Name}``");
		}

		private async Task TrySub(IUser user, ITextChannel channel)
		{
			var serverJson = SubSystem.LoadSubServerJson(Context.Guild.Id);
			if (!serverJson.Data.ContainsKey(channel.Id))
			{
				await ReplyAsync($"Unable for ``{Tools.GenFullName(user)}`` to subscribe to {channel.Mention} because no subscription is possible.");
				return;
			}

			if (Context.Guild.GetRole(serverJson.Data[channel.Id]) == null)
			{
				await ReplyAsync($"Unable to find the role associated with subscribing to {channel.Mention}, contact an administrator.");
				return;
			}

			var roleID = serverJson.Data[channel.Id];
			var guildRole = Context.Guild.GetRole(roleID);
			if ((user as IGuildUser).RoleIds.Any(r => r == roleID))
			{
				await (user as IGuildUser).RemoveRolesAsync(guildRole);
				await ReplyAsync($"Unsubscribed ``{Tools.GenFullName(user)}`` from {channel.Mention}");
				return;
			}

			await (user as IGuildUser).AddRolesAsync(guildRole);
			await ReplyAsync($"Subscribed ``{Tools.GenFullName(user)}`` to {channel.Mention}");
		}
	}
}
