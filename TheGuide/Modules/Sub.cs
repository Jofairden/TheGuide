﻿using Discord;
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
	[Group("unsub")]
	public class Unsub : ModuleBase
	{
		private readonly CommandService service;
		private readonly IDependencyMap map;

		public Unsub(CommandService service, IDependencyMap map)
		{
			this.service = service;
			this.map = map;
		}

		[Name("no-help")]
		[Command, Priority(10)]
		public async Task UnsubMe([Remainder] ITextChannel channel) =>
			await TryUnsub(Context.User, channel);

		[Name("no-help")]
		[Command, Priority(10)]
		[SubAdminAttr]
		public async Task UnsubUser(IUser user, [Remainder] ITextChannel channel) =>
			await TryUnsub(user, channel);

		[Name("no-help")]
		[Command, Priority(10)]
		[SubAdminAttr]
		public async Task UnsubUser(ITextChannel channel, [Remainder]IUser user) =>
			await TryUnsub(user, channel);

		[Name("no-help")]
		[Command, Priority(10)]
		[SubAdminAttr]
		public async Task UnsubUser(IUser user, [Remainder]string rem)
		{
			if (rem.RemoveWhitespace().ToLower() == "all")
				await TryUnSubAll(user);
			else
				await service.ExecuteAsync(Context, "help unsub me", map, MultiMatchHandling.Best);
		}

		[Command("me")]
		[Summary("Unsub to to a channel or all channels")]
		[Remarks("unsub me <channel> --OR-- unsub me all")]
		public async Task Me([Remainder] string rem = null)
		{
			if (rem.RemoveWhitespace().ToLower() == "all")
				await TryUnSubAll(Context.User);
			else
				await service.ExecuteAsync(Context, "help unsub me", map, MultiMatchHandling.Best);
		}

		[Name("no-help")]
		[Command("me")]
		public async Task Me([Remainder] ITextChannel channel)
		{
			await TryUnsub(Context.User, channel);
		}

		[Command("list")]
		[Summary("List channels")]
		[Remarks("list [user]")]
		public async Task List([Remainder] IUser user = null) =>
			await service.ExecuteAsync(Context, $"sub list {($"{user?.Id}" ?? "")}");

		[Command("all")]
		[Summary("Unsub to all channels")]
		[Remarks("sub all")]
		public async Task All([Remainder] string rem = null) =>
			await TryUnSubAll(Context.User);

		[Command("all")]
		[Summary("Unsub a user to all channels")]
		[Remarks("sub all")]
		[SubAdminAttr]
		public async Task All([Remainder]IUser user) =>
			await TryUnSubAll(user);

		private async Task TryUnSubAll(IUser user)
		{
			var guild = Context.Guild as SocketGuild;
			var userJson = SubSystem.LoadSubUserJson(guild.Id, user.Id);


			if (!userJson.SubRoles.Any())
			{
				await ReplyAsync($"``{user.GenFullName()}`` is not subscribed to any channels.");
				return;
			}

			// need enumerable due to lib bug
			List<IRole> roles = new List<IRole>();
			userJson.SubRoles.ForEach(r =>
			{
				var role = guild.GetRole(r);
				if ((user as SocketGuildUser).Roles.Contains(role))
					roles.Add(role);
			});
			if (roles.Any())
				await (user as SocketGuildUser).ChangeRolesAsync(remove: roles);

			userJson.SubRoles.Clear();
			await userJson.Write(guild.Id, true);
			await ReplyAsync($"Unsubscribed ``{user.GenFullName()}`` from all channels.");
		}

		private async Task TryUnsub(IUser user, ITextChannel channel)
		{
			var guild = Context.Guild as SocketGuild;
			var userJson = SubSystem.LoadSubUserJson(guild.Id, user.Id);
			var serverJson = SubSystem.LoadSubServerJson(Context.Guild.Id);

			if (!serverJson.Data.ContainsKey(channel.Id))
			{
				await ReplyAsync($"No subscription exists for {channel.Mention}");
				return;
			}

			var roleID = serverJson.Data[channel.Id];

			if (!userJson.SubRoles.Contains(roleID))
			{
				await ReplyAsync($"``{user.GenFullName()}`` is not subscribed to {channel.Mention}");
				return;
			}

			var role = (user as SocketGuildUser)?.Roles.FirstOrDefault(r => r.Id == roleID);
			if (role != null)
				await ((SocketGuildUser) user).RemoveRolesAsync(role);

			userJson.SubRoles.Remove(roleID);
			var result = await userJson.Write(guild.Id, true);
			await ReplyAsync(result.IsSuccess
				? $"Successfully unsubcribed ``{user.GenFullName()}`` from {channel.Mention}"
				: result.ErrorReason);
		}
	}

	/// <summary>
	/// Sub command module
	/// </summary>
	[Group("sub")]
	[Name("sub")]
	public class Sub : ModuleBase
	{
		private readonly CommandService service;
		private readonly IDependencyMap map;

		public Sub(CommandService service, IDependencyMap map)
		{
			this.service = service;
			this.map = map;
		}

		/// <summary>
		/// Removes all sub roles from the guild, and any user files
		/// </summary>
		/// <returns></returns>
		[Command("clearsubroles")]
		[Alias("csr")]
		[SubAdminAttr]
		public async Task ClearSubRoles()
		{
			var count = await TryClearSubRoles();

			await ReplyAsync(count > 0
				? $"Cleared {count} role{(count > 1 ? "s" : "")}"
				: $"No roles found to be cleared");
		}

		private async Task<int> TryClearSubRoles()
		{
			await Task.Yield();
			var guild = Context.Guild as SocketGuild;
			var serverJson = SubSystem.LoadSubServerJson(guild.Id);

			// Get roles
			var roles =
				serverJson.Data
					.Select(x =>
						guild.GetRole(x.Value))
					.Where(x =>
						x != null)
					.Concat(guild.Roles)
					.Where(x =>
						x.Name.StartsWith("sub-")
						&& serverJson.Data.All(y => y.Value != x.Id))
					.ToList();

			roles.ForEach(async x =>
				await x.DeleteAsync());

			return roles.Count;
		}

		/// <summary>
		/// Will add/remove a certain role as 'admin' for Subsystem to server data
		/// </summary>
		[Command("createadmin")]
		[Summary("Will add/remove a certain role as 'admin' for Subsystem to server data")]
		[Alias("makeadmin", "adminrole", "deleteadmin", "removeadmin", "deladmin", "remadmin", "admin")]
		[Remarks("createadmin <role>\ncreateadmin @administrator")]
		[SubAdminAttr]
		public async Task CreateAdmin([Remainder] IRole role = null)
		{
			if (role == null)
			{
				await service.ExecuteAsync(Context, $"help sub createadmin", map, MultiMatchHandling.Best);
				return;
			}

			var serverJson = 
				SubSystem.LoadSubServerJson(Context.Guild.Id);

			var cond =
				serverJson.AdminRoles.Contains(role.Id);
			var msg = cond
				? $"Role ``{role.Name}`` is no longer a 'SubSystem' admin"
				: $"Role ``{role.Name}`` is now a 'SubSystem' admin";
			if(cond)
				serverJson.AdminRoles.Remove(role.Id);
			else
				serverJson.AdminRoles.Add(role.Id);

			var result =
				await serverJson.Write(Context.Guild.Id);

			await ReplyAsync(result.IsSuccess
				? msg
				: result.ErrorReason);
		}

		/// <summary>
		/// Tries to delete a subscription of a channel
		/// </summary>
		[Command("delete")]
		[Alias("remove")]
		[Summary("Delete a subscription of a channel")]
		[Remarks("delete <channel>\ndelete #github")]
		[SubAdminAttr]
		public async Task Delete([Remainder]ITextChannel channel = null)
		{
			if (channel == null)
			{
				await service.ExecuteAsync(Context, $"help sub delete", map, MultiMatchHandling.Best);
				return;
			}
			await TryDelete(channel);
		}

		/// <summary>
		/// Tries to delete a subscription of a role
		/// </summary>
		[Command("delete")]
		[Alias("remove")]
		[Summary("Delete a subscription of a channel linked to role")]
		[Remarks("delete <role>\ndelete @sub-github")]
		[SubAdminAttr]
		public async Task Delete([Remainder]IRole role = null)
		{
			if (role == null)
			{
				await service.ExecuteAsync(Context, $"help sub delete", map, MultiMatchHandling.Best);
				return;
			}
			await TryDelete(role);
		}

		/// <summary>
		/// Tries to clear all subscriptions for guild
		/// </summary>
		[Command("clear")]
		[Summary("Delete all subscription data for guild")]
		[Remarks("clear")]
		[SubAdminAttr]
		public async Task Clear()
		{
			var guild = Context.Guild as SocketGuild;
			var serverJson = SubSystem.LoadSubServerJson(guild.Id);

			// Get roles
			var roles =
				serverJson.Data
					.Select(x =>
						guild.GetRole(x.Value))
					.Where(x =>
						x != null);

			// Get channels
			var channels =
				serverJson.Data
					.Select(x =>
						guild.GetTextChannel(x.Key))
					.Where(x =>
						x != null)
					.ToList();

			// Reset channel perms
			channels.ForEach(async x =>
			{
				var rolePerms =
					new OverwritePermissions(readMessages: PermValue.Allow);
				await x.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, rolePerms);
			});

			// Reset user data
			await guild.DownloadUsersAsync();
			foreach (var user in guild.Users)
			{
				var userJson = 
					SubSystem.LoadSubUserJson(guild.Id, user.Id);

				// Clear json data
				userJson.SubRoles.Clear();
				await userJson.Write(guild.Id, true);

				// Get user sub roles
				var compliantRoles =
					user.Roles
						.Where(x =>
							roles.Contains(x))
						.ToArray();

				// Remove roles
				if (compliantRoles.Any())
					await user.ChangeRolesAsync(remove: compliantRoles);
			}


			// Clear server data
			serverJson.Data.Clear();
			await serverJson.Write(guild.Id);

			await TryClearSubRoles();

			await ReplyAsync($"Cleared guild subscription data");
		}

		[Command("all")]
		[Summary("Sub to all channels")]
		[Remarks("sub all")]
		public async Task All([Remainder] string rem = null) =>
			await TrySubAll(Context.User);

		[Command("all")]
		[Summary("Sub a user to all channels")]
		[Remarks("sub all")]
		[SubAdminAttr]
		public async Task All([Remainder]IUser user) =>
			await TrySubAll(user);

		/// <summary>
		/// Self command, channel parameter
		/// </summary>
		[Name("no-help")]
		[Command, Priority(10)]
		[Summary("Sub yourself to a certain channel")]
		[Remarks("sub\nsub #github -- OR -- sub github -- OR -- sub \\#github")]
		public async Task SubMe([Remainder] ITextChannel channel) =>
			await TrySub(Context.User, channel);

		/// <summary>
		/// Self command, user + channel parameter
		/// </summary>
		[Name("no-help")]
		[Command, Priority(10)]
		[Summary("Sub a user to a certain channel")]
		[Remarks("sub <user> <channel>\nsub Jofairden #github")]
		[SubAdminAttr]
		public async Task SubUser(IUser user, [Remainder] ITextChannel channel) =>
			await TrySub(user, channel);

		[Name("no-help")]
		[Command, Priority(10)]
		[Summary("Sub a user to a certain channel")]
		[Remarks("sub <user> <channel>\nsub Jofairden #github")]
		[SubAdminAttr]
		public async Task SubUser(IUser user, [Remainder] string rem)
		{
			if (rem.RemoveWhitespace().ToLower() == "all")
				await TrySubAll(user);
			else
				await service.ExecuteAsync(Context, "help sub", map, MultiMatchHandling.Best);
		}

		/// <summary>
		/// Self command, channel + user parameter
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		[Name("no-help")]
		[Command, Priority(10)]
		[Summary("Sub a user to a certain channel")]
		[Remarks("sub <user> <channel>\nsub Jofairden #github")]
		[SubAdminAttr]
		public async Task SubUser(ITextChannel channel, [Remainder] IUser user) =>
			await TrySub(user, channel);

		/// <summary>
		/// Tries to sub yourself to channel
		/// </summary>
		[Command("me")]
		[Summary("Sub yourself to a certain channel, finds any channel by name, mention or ID")]
		[Remarks("me <channel> --OR-- me all\nme #github --OR-- me github --OR-- me \\#github")]
		public async Task Me([Remainder] ITextChannel channel)
		{
			await TrySub(Context.User, channel);
		}

		[Name("no-help")]
		[Command("me")]
		[Summary("Sub yourself to all channels")]
		public async Task Me([Remainder] string channel)
		{
			if (channel.RemoveWhitespace().ToLower() == "all")
				await TrySubAll(Context.User);
			else
				await service.ExecuteAsync(Context, "help sub me", map, MultiMatchHandling.Best);
		}

		/// <summary>
		/// Tries to list all existing subscriptions
		/// </summary>
		/// <returns></returns>
		[Command("list")]
		[Summary("Lists all subscriptions available in the server.")]
		[Remarks("list")]
		public async Task List()
		{
			var serverJson = SubSystem.LoadSubServerJson(Context.Guild.Id);
			var msg = $"No subscriptions found for ``{Context.Guild.Name}``";
			var extra = $"\n\nType ``{CommandHandler.prefixChar}sub <channel>`` or ``{CommandHandler.prefixChar}sub me <channel>`` to subscribe or unsubscribe. " +
						$"\n``{CommandHandler.prefixChar}unsub`` commands are also available.";
			if (serverJson.Data.Any())
			{
				msg = $"Showing {serverJson.Data.Count} subscriptions for ``{Context.Guild.Name}``:\n\n";
				msg += string.Join("\n", serverJson.Data
					.Select(x =>
						$"**{(Context.Guild as SocketGuild)?.TextChannels.FirstOrDefault(c => c.Id == x.Key).Name ?? "null"}**: {Context.Guild.GetRole(x.Value)?.Name ?? "null"}"))
					.Cap(2000 - msg.Length - extra.Length);
			}

			await ReplyAsync(msg + extra);
		}

		/// <summary>
		/// Tries to list all subscriptions for specified user
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		[Command("list")]
		[Summary("Lists all subscriptions from specified user.")]
		[Remarks("list")]
		public async Task List([Remainder] IUser user)
		{
			if (!SubSystem.SubUserExists(Context.Guild.Id, user.Id))
			{
				await ReplyAsync($"Could not find user.json file for ``{user.GenFullName()}``");
				return;
			}
			var guild = Context.Guild as SocketGuild;
			var serverJson = SubSystem.LoadSubServerJson(Context.Guild.Id);
			var userJson = SubSystem.LoadSubUserJson(Context.Guild.Id, user.Id);
			var msg = $"``{user.GenFullName()}`` has no subscriptions.";
			if (userJson.SubRoles.Any())
			{
				msg = $"Showing subscriptions of ``{user.GenFullName()}``\n\n";
				var subs = serverJson.Data.Where(x => userJson.SubRoles.Contains(x.Value)).ToArray();
				if (!subs.Any())
					msg += $"No server subscriptions available";
				else
					msg += 
						string.Join("\n", subs.Select(x => $"**{guild?.GetChannel(x.Key).Name ?? "null"}**: {guild?.GetRole(x.Value).Name ?? "null"}"))
						.Cap(2000 - msg.Length);
			}
			await ReplyAsync(msg);
		}

		/// <summary>
		/// Tries to create a subscription for channel with role
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="role"></param>
		/// <returns></returns>
		[Command("create")]
		[Summary("Create a subscription, finds any channel/role by mention, name or ID")]
		[Remarks("create <channel> <role>\ncreate #github @sub-github")]
		[SubAdminAttr]
		public async Task Create(ITextChannel channel, IRole role)
		{
			await TryCreate(channel, role);
		}

		/// <summary>
		/// Tries to create a subscription for channel with role
		/// </summary>
		/// <param name="role"></param>
		/// <param name="channel"></param>
		/// <returns></returns>
		[Command("create")]
		[Summary("Create a subscription, finds any channel/role by mention, name or ID")]
		[Remarks("create <role> <channel>\ncreate @sub-github #github")]
		[SubAdminAttr]
		public async Task Create(IRole role, ITextChannel channel)
		{
			await Create(channel, role);
		}

		/// <summary>
		/// Tries to create a subscription for channel
		/// </summary>
		[Command("create")]
		[Summary("Create a subscription, finds any channel/role by mention, name or ID")]
		[Remarks("create <channel>\ncreate #github")]
		[SubAdminAttr]
		public async Task Create(ITextChannel channel)
		{
			// find sub-role, not found => make it
			//todo: config: role prefix
			var roleName = $"sub-{channel.Name}";
			var role =
				Context.Guild.Roles.
					FirstOrDefault(r =>
						string.Equals(roleName, r.Name, StringComparison.CurrentCultureIgnoreCase));

			if (role == null)
			{
				role = 
					await Context.Guild.CreateRoleAsync(roleName);
				var rolePerms = 
					new OverwritePermissions(readMessages: PermValue.Allow, readMessageHistory: PermValue.Allow);
				await channel.AddPermissionOverwriteAsync(role, rolePerms);
				await ReplyAsync($"Since no role named ``{roleName}`` was found, I created one with read permission for channel {channel.Mention}");
			}

			await TryCreate(channel, role);
		}

		/// <summary>
		/// Tries to delete subscriptions connected to channel
		/// </summary>
		private async Task TryDelete(ITextChannel channel)
		{
			var guild = Context.Guild as SocketGuild;
			var serverJson =
				SubSystem.LoadSubServerJson(guild.Id);

			var data =
				serverJson.Data
					.Where(x =>
						x.Key == channel.Id)
					.ToDictionary(x => x.Key, x => x.Value);

			if (!data.Any())
			{
				await ReplyAsync($"Could not find any subscriptions for {channel.Mention}");
				return;
			}

			await guild.DownloadUsersAsync();
			foreach (var user in guild.Users)
			{
				var userJson =
					SubSystem.LoadSubUserJson(guild.Id, user.Id);

				var roleIds = data.Select(x => x.Value);
				if (userJson.SubRoles.Any(x => roleIds.Contains(x)))
				{
					userJson.SubRoles =
						userJson.SubRoles
							.Except(data.Select(x => x.Value))
							.ToList();

					await userJson.Write(guild.Id, true);

					var remRoles =
						data
							.Where(x =>
								user.Guild.GetRole(x.Value) != null)
							.Select(x =>
								user.Guild.GetRole(x.Value))
							.ToArray();

					if (remRoles.Any())
						await user.ChangeRolesAsync(remove: remRoles);
				}
			}

			serverJson.Data =
				serverJson.Data
					.Where(kvp => !data.ContainsKey(kvp.Key))
					.ToDictionary(x => x.Key, x => x.Value);

			var rolePerms =
					new OverwritePermissions(readMessages: PermValue.Allow);
			await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, rolePerms);

			var result = await serverJson.Write(Context.Guild.Id);
			await ReplyAsync(result.IsSuccess
				? $"Removed {data.Count} subscription{(data.Count > 1 ? "s" : "")} associated {channel.Mention}"
				: result.ErrorReason);
		}

		/// <summary>
		/// Tries to delete subscriptions connected to role
		/// </summary>
		private async Task TryDelete(IRole role)
		{
			var guild = Context.Guild as SocketGuild;
			var serverJson = 
				SubSystem.LoadSubServerJson(guild.Id);

			var data =
				serverJson.Data
					.Where(x =>
						x.Value == role.Id)
					.ToDictionary(x => x.Key, x => x.Value);

			if (!data.Any())
			{
				await ReplyAsync($"Could not find any subscriptions for role ``{role.Name}``");
				return;
			}

			await guild.DownloadUsersAsync();
			foreach (var user in guild.Users)
			{
				var userJson = 
					SubSystem.LoadSubUserJson(guild.Id, user.Id);

				var roleIds = data.Select(x => x.Value);
				if (userJson.SubRoles.Any(x => roleIds.Contains(x)))
				{
					userJson.SubRoles =
						userJson.SubRoles
							.Except(data.Select(x => x.Value))
							.ToList();

					await userJson.Write(guild.Id, true);

					var remRoles =
						data
							.Where(x =>
								user.Guild.GetRole(x.Value) != null)
							.Select(x =>
								user.Guild.GetRole(x.Value))
							.ToArray();

					if (remRoles.Any())
						await user.ChangeRolesAsync(remove: remRoles);
				}
			}

			serverJson.Data =
				serverJson.Data
					.Where(kvp => !data.ContainsKey(kvp.Key))
					.ToDictionary(x => x.Key, x => x.Value);

			var channels = data
					.Select(x => x.Key)
					.Where(x => guild.GetTextChannel(x) != null)
					.Select(x => guild.GetTextChannel(x))
					.ToList();

			channels.ForEach(async x =>
			{
				var rolePerms =
					new OverwritePermissions(readMessages: PermValue.Allow);
				await x.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, rolePerms);
			});

			var result = await serverJson.Write(Context.Guild.Id);
			await ReplyAsync(result.IsSuccess 
				? $"Removed {data.Count} subscription{(data.Count > 1 ? "s" : "")} associated with role ``{role.Name}``"
				: result.ErrorReason);
		}

		/// <summary>
		/// Attempts to create a subscription for a channel
		/// </summary>
		private async Task TryCreate(ITextChannel channel, IRole role)
		{
			var serverJson = 
				SubSystem.LoadSubServerJson(Context.Guild.Id);

			if (serverJson.Data.Any(x => x.Value == role.Id))
			{
				await ReplyAsync($"There already exists a subscription using role ``{role.Name}``");
				return;
			}

			if (serverJson.Data.ContainsKey(channel.Id))
			{
				await ReplyAsync($"{channel.Mention} already has a subscription using role ``{Context.Guild.GetRole(serverJson.Data[channel.Id]).Name}``");
				return;
			}

			var rolePerms =
					new OverwritePermissions(readMessages: PermValue.Deny);
			await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, rolePerms);

			serverJson.Data.Add(channel.Id, role.Id);
			var result =
				await serverJson.Write(Context.Guild.Id);

			await ReplyAsync(result.IsSuccess 
				? $"Created subscription for {channel.Mention} using role ``{role.Name}``"
				: result.ErrorReason);
		}

		/// <summary>
		/// Subscribes to any missing channel
		/// </summary>
		private async Task TrySubAll(IUser user)
		{
			var guild = 
				Context.Guild as SocketGuild;
			var serverJson = 
				SubSystem.LoadSubServerJson(guild.Id);
			var userJson =
				SubSystem.LoadSubUserJson(guild.Id, user.Id);
			var newSubs =
				serverJson.Data
					.Where(x =>
						!userJson.SubRoles
							.Contains(x.Value))
					.ToArray();
			var ufull = 
				$"``{user.GenFullName()}``";


			if (newSubs.Any())
			{
				// need an enumerable due to lib bug
				var roles =
					newSubs
						.Where(x =>
							guild.GetRole(x.Value) != null
							&& !(user as SocketGuildUser).Roles.Any(r => r.Id == x.Value))
						.Select(x =>
							guild.GetRole(x.Value))
						.ToList();

				if (roles.Any())
				{
					await (user as IGuildUser).ChangeRolesAsync(add: roles.AsEnumerable());
					userJson.SubRoles =
						userJson.SubRoles
						.Union(roles.Select(x => x.Id))
						.ToList();

					var result = 
						await userJson.Write(guild.Id, true);

					await ReplyAsync(result.IsSuccess
						? $"Subscribed {ufull} to all channels"
						: result.ErrorReason);
					return;
				}
			}
			await ReplyAsync($"No subscriptions available for {ufull}");
		}

		/// <summary>
		/// Attempts to sub a user to a certain channel
		/// </summary>
		private async Task TrySub(IUser user, ITextChannel channel)
		{
			var guild = 
				Context.Guild as SocketGuild;
			var userJson = 
				SubSystem.LoadSubUserJson(guild.Id, user.Id);
			var serverJson = 
				SubSystem.LoadSubServerJson(guild.Id);
			var ufull = 
				$"``{user.GenFullName()}``";
			
			// No subscription found
			if (!serverJson.Data.ContainsKey(channel.Id))
			{
				await ReplyAsync($"No subscription for {channel.Mention} found");
				return;
			}

			var roleID = serverJson.Data[channel.Id];
			var role = guild.GetRole(roleID);

			if (userJson.SubRoles.Contains(roleID))
			{
				await ReplyAsync($"{ufull} is already subscribed to {channel.Mention}");
				return;
			}
			// No server role found
			if (role == null)
			{
				await ReplyAsync($"No role of subscription for {channel.Mention} found, contact an administrator");
				return;
			}

			// Sub to channel
			var roles = new List<IRole> {role};
			await (user as IGuildUser).ChangeRolesAsync(add: roles);
			userJson.SubRoles = userJson.SubRoles.Union(roles.Select(x => x.Id)).ToList();

			var result = await userJson.Write(guild.Id, true);
			await ReplyAsync(result.IsSuccess 
				? $"Subscribed ``{user.GenFullName()}`` to {channel.Mention}" 
				: result.ErrorReason);
		}
	}
}
