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
			var guild = Context.Guild as SocketGuild;
			var roles = new List<string>();
			// loop roles
			if (guild?.Roles != null)
				foreach (var role in guild?.Roles)
				{
					//todo: config: configure prefix?
					if (role.Name.StartsWith("sub-"))
					{
						// Remove from server data
						var serverJson = SubSystem.LoadSubServerJson(Context.Guild.Id);
						serverJson.Data =
							serverJson.Data
							.Where(kvp => kvp.Value != role.Id)
							.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
						await SubSystem.CreateServerSub(Context.Guild.Id, serverJson);

						// Clear from user data
						foreach (var user in guild.Users)
						{
							var userJson = SubSystem.LoadSubUserJson(guild.Id, user.Id);
							userJson.SubRoles = userJson.SubRoles.Where(r => r != role.Id).ToList();
							await SubSystem.CreateUserSub(guild.Id, user.Id, userJson, true);
						}
						roles.Add($"**{role.Name}** ({role.Id})");
						await role.DeleteAsync();
					}
				}
			//reply
			var msg = roles.Count > 0 ? $"Removed {roles.Count} roles:\n" : "No roles found.";
			await ReplyAsync(roles.Count > 0 ? $"{msg}{string.Join("\n", roles).Cap(2000 - msg.Length)}" : msg);
		}

		/// <summary>
		/// Will add/remove a certain role as 'admin' for Subsystem to server data
		/// </summary>
		[Command("createadmin")]
		[Alias("makeadmin", "adminrole", "deleteadmin", "removeadmin", "deladmin", "remadmin", "admin")]
		[SubAdminAttr]
		public async Task CreateAdmin([Remainder] IRole role)
		{
			var serverJson = SubSystem.LoadSubServerJson(Context.Guild.Id);
			var msg = $"Role ``{role.Name}`` is no longer a 'SubSystem' admin.";
			if (!serverJson.AdminRoles.Contains(role.Id))
			{
				serverJson.AdminRoles.Add(role.Id);
				msg = $"Role ``{role.Name}`` is now a 'SubSystem' admin.";
			}
			else
				serverJson.AdminRoles.Remove(role.Id);

			await SubSystem.CreateServerSub(Context.Guild.Id, serverJson);
			await ReplyAsync(msg);
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
		[Summary("Delete all subscriptions for guild")]
		[Remarks("clear")]
		[SubAdminAttr]
		public async Task Clear()
		{
			var oldJson = SubSystem.LoadSubServerJson(Context.Guild.Id);
			if (oldJson.Data.Any())
			{
				// distinct select roles from data
				var data =
					oldJson.Data
						.GroupBy(kvp => kvp.Value)
						.Select(g => g.First())
						.Select(kvp => kvp.Value);

				var guild = Context.Guild as SocketGuild;
				if (guild != null)
				{
					// guild roles found from json data
					var groles =
						guild.Roles
						.Where(r => data.Contains(r.Id))
						.ToArray();

					await guild.DownloadUsersAsync();
					// tries to remove roles from users
					foreach (var user in guild.Users)
					{
						var roles =
							user.Roles
								.Where(r =>
									groles.Contains(r)).ToArray();

						if (roles.Any())
							await user.RemoveRolesAsync(roles);

						// reset user data
						await SubSystem.CreateUserSub(
							Context.Guild.Id,
							user.Id,
							new SubUserJson()
							{
								Name = user.GenFullName(),
								UID = user.Id,
								SubRoles = new List<ulong>()

							},
							true);
					}

					//todo: config: remove roles from server upon clear?
					//remove roles
					if (true) // true/false config
						foreach (var socketRole in groles)
							await socketRole.DeleteAsync();
				}
			}
			var oldSubs =
				oldJson.Data
					.Select(x =>
						$"**{(Context.Guild as SocketGuild)?.TextChannels.FirstOrDefault(c => c.Id == x.Key).Name ?? "null"}**: {Context.Guild.GetRole(x.Value)?.Name ?? "null"}");

			// Reset server data
			await SubSystem.CreateServerSub(
				Context.Guild.Id,
				new SubServerJson()
				{
					GUID = Context.Guild.Id,
					AdminRoles = oldJson.AdminRoles
				});

			var msg = $"No subscriptions removed from server {Context.Guild.Name}";
			if (oldJson.Data.Count > 0)
			{
				msg = $"Removed {oldJson.Data.Count} subscription{(oldJson.Data.Count > 1 ? "s" : "")} from server:\n\n";
				msg += string.Join("\n", oldSubs).Cap(2000 - msg.Length);
			}

			await ReplyAsync(msg);
		}

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
		[Remarks("me <channel>\nme #github --OR-- me github --OR-- me \\#github")]
		public async Task Me([Remainder] ITextChannel channel)
		{
			await TrySub(Context.User, channel);
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
			var msg = $"No subscriptions found for ``{Context.Guild.Name}";
			if (serverJson.Data.Any())
			{
				msg = $"Showing {serverJson.Data.Count} subscriptions for ``{Context.Guild.Name}``:\n\n";
				msg += string.Join("\n", serverJson.Data
					.Select(x =>
						$"**{(Context.Guild as SocketGuild)?.TextChannels.FirstOrDefault(c => c.Id == x.Key).Name ?? "null"}**: {Context.Guild.GetRole(x.Value)?.Name ?? "null"}"))
					.Cap(2000 - msg.Length);
			}

			await ReplyAsync(msg);
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
			// find sub-role, not found => make it
			var roleName = $"sub-{channel.Name}";
			var findRole = Context.Guild.Roles.FirstOrDefault(r => string.Equals(roleName, r.Name, StringComparison.CurrentCultureIgnoreCase));
			if (findRole == null)
			{
				findRole = await Context.Guild.CreateRoleAsync(roleName);
				var rolePerms = new OverwritePermissions(readMessages: PermValue.Allow, readMessageHistory: PermValue.Allow);
				await channel.AddPermissionOverwriteAsync(findRole, rolePerms);
				rolePerms = new OverwritePermissions(readMessages: PermValue.Deny);
				await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, rolePerms);
				await ReplyAsync($"Since no role named ``{roleName}`` was found, I created one with read permission for channel {channel.Mention}");
			}

			await TryCreate(channel, findRole);
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
		/// <param name="channel"></param>
		/// <returns></returns>
		[Command("create")]
		[Summary("Create a subscription, finds any channel/role by mention, name or ID")]
		[Remarks("create <channel>\ncreate #github")]
		[SubAdminAttr]
		public async Task Create(ITextChannel channel)
		{
			// find sub-role, not found => make it
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

		/// <summary>
		/// Tries to delete subscriptions connected to channel
		/// </summary>
		/// <param name="channel"></param>
		/// <returns></returns>
		private async Task TryDelete(ITextChannel channel)
		{
			var serverJson = SubSystem.LoadSubServerJson(Context.Guild.Id);
			var keys = serverJson.Data.Where(x => x.Key == channel.Id).Select(i => i.Key).ToArray();
			if (keys.Length <= 0)
			{
				await ReplyAsync($"Could not find any subscription for {channel.Mention}");
				return;
			}
			foreach (ulong key in keys)
				serverJson.Data.Remove(key);

			await SubSystem.CreateServerSub(Context.Guild.Id, serverJson);
			await ReplyAsync($"Removed {keys.Length} subscription{(keys.Length > 0 ? "s" : "")} for {channel.Mention}");
		}

		/// <summary>
		/// Tries to delete subscriptions connected to role
		/// </summary>
		/// <param name="role"></param>
		/// <returns></returns>
		private async Task TryDelete(IRole role)
		{
			var serverJson = SubSystem.LoadSubServerJson(Context.Guild.Id);
			var keys = serverJson.Data.Where(x => x.Value == role.Id).Select(i => i.Key).ToArray();
			if (keys.Length <= 0)
			{
				await ReplyAsync($"Could not find any subscriptions for role ``{role.Name}``");
				return;
			}
			foreach (ulong key in keys)
				serverJson.Data.Remove(key);

			await SubSystem.CreateServerSub(Context.Guild.Id, serverJson);
			await ReplyAsync($"Removed {keys.Length} subscription{(keys.Length > 0 ? "s" : "")} for role ``{role.Name}``");
		}

		/// <summary>
		/// Attempts to create a subscription for a channel
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="role"></param>
		/// <returns></returns>
		private async Task TryCreate(ITextChannel channel, IRole role)
		{
			var serverJson = SubSystem.LoadSubServerJson(Context.Guild.Id);
			if (serverJson.Data.ContainsKey(channel.Id))
			{
				await ReplyAsync($"{channel.Mention} already has a subcription using role ``{Context.Guild.GetRole(serverJson.Data[channel.Id]).Name}``");
				return;
			}
			serverJson.Data.Add(channel.Id, role.Id);
			await SubSystem.CreateServerSub(Context.Guild.Id, serverJson);
			await ReplyAsync($"Created subscription for {channel.Mention} using role ``{role.Name}``");
		}

		/// <summary>
		/// Attempts to sub a user to a certain channel
		/// </summary>
		/// <param name="user"></param>
		/// <param name="channel"></param>
		/// <returns></returns>
		private async Task TrySub(IUser user, ITextChannel channel)
		{
			var serverJson = SubSystem.LoadSubServerJson(Context.Guild.Id);

			// No subscription found
			if (!serverJson.Data.ContainsKey(channel.Id))
			{
				await ReplyAsync($"Unable for ``{user.GenFullName()}`` to subscribe to {channel.Mention} because no subscription exists.");
				return;
			}

			// No server role found
			if (Context.Guild.GetRole(serverJson.Data[channel.Id]) == null)
			{
				await ReplyAsync($"Unable to find the role associated with subscribing to {channel.Mention}, contact an administrator.");
				return;
			}

			GuideResult result;
			var userJson = SubSystem.LoadSubUserJson(Context.Guild.Id, user.Id);
			var roleID = serverJson.Data[channel.Id];
			var guildRole = Context.Guild.GetRole(roleID);

			// Unsub
			if ((user as IGuildUser).RoleIds.Any(r => r == roleID))
			{
				await (user as IGuildUser).RemoveRolesAsync(guildRole);
				if (userJson.SubRoles.Contains(roleID))
					userJson.SubRoles.Remove(roleID);

				result = await SubSystem.CreateUserSub(Context.Guild.Id, user.Id, userJson, true);
				await ReplyAsync(result.IsSuccess ? $"Unsubscribed ``{user.GenFullName()}`` from {channel.Mention}" : result.ErrorReason);
				return;
			}

			// Sub to channel
			await (user as IGuildUser).AddRolesAsync(guildRole);
			if (!userJson.SubRoles.Contains(roleID))
				userJson.SubRoles.Add(roleID);

			result = await SubSystem.CreateUserSub(Context.Guild.Id, user.Id, userJson, true);
			await ReplyAsync(result.IsSuccess ? $"Subscribed ``{user.GenFullName()}`` to {channel.Mention}" : result.ErrorReason);
		}
	}
}
