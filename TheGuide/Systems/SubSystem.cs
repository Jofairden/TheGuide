using System;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TheGuide.Systems
{
	public class SubServerJson : GuideJson
	{
		public SubServerJson(SubServerJson source = null)
		{
			if (source != null)
			{
				GUID = source.GUID;
				Data = new Dictionary<ulong, ulong>(source.Data);
				AdminRoles = new List<ulong>(source.AdminRoles);
			}
		}

		public async Task<GuideResult> Write(ulong guid)
		{
			var result = await SubSystem.CreateServerSub(guid, this);
			return result;
		}

		public ulong GUID { get; set; } // => guild ID
		public Dictionary<ulong, ulong> Data { get; set; } = new Dictionary<ulong, ulong>(); // key => channelID, value => roleID
		public List<ulong> AdminRoles { get; set; } = new List<ulong>(); // => roleID

		public override void Validate()
		{
			Data = !Data.Any() ? new Dictionary<ulong, ulong>() : Data;
			AdminRoles = !AdminRoles.Any() ? new List<ulong>() : AdminRoles;
		}
	}

	public class SubUserJson : GuideJson
	{
		public SubUserJson(SubUserJson source = null)
		{
			if (source != null)
			{
				Name = source.Name;
				UID = source.UID;
				SubRoles = new List<ulong>(source.SubRoles);
			}
		}

		public async Task<GuideResult> Write(ulong guid, bool ignore = false)
		{
			var result = await SubSystem.CreateUserSub(guid, UID, this, ignore);
			return result;
		}

		public string Name { get; set; }
		public ulong UID { get; set; }
		public List<ulong> SubRoles { get; set; }

		public override void Validate()
		{
			Name = string.IsNullOrEmpty(Name) ? "null" : Name;
			SubRoles = !SubRoles.Any() ? new List<ulong>() : SubRoles;
		}
	}

	public class SubSystem
	{
		// ./assembly/dist/subs/
		public static string rootDir =>
			Path.Combine(AppContext.BaseDirectory, "dist", "subs");

		public static string[] jsonfiles(ulong guildid) =>
			Directory
				.GetFiles(Path.Combine(rootDir, $"{guildid}"), "*.json")
				.Select(Path.GetFileNameWithoutExtension)
				.ToArray();

		/// <summary>
		/// Tries to maintain directories along with server.json files and uid.json files
		/// </summary>
		public static async Task Maintain(IDiscordClient client)
		{
			var discordSocketClient = client as DiscordSocketClient;
			if (discordSocketClient != null)
				foreach (var guild in discordSocketClient.Guilds)
					await MaintainServer(guild);
		}

		public static async Task MaintainServer(SocketGuild guild)
		{
			var path = Path.Combine(rootDir, $"{guild.Id}");
			Directory.CreateDirectory(path);
			if (!File.Exists(Path.Combine(path, "server.json")))
			{
				await CreateServerSub(guild.Id, new SubServerJson {GUID = guild.Id});
				return;
			}

			var json = LoadSubServerJson(guild.Id);

			// Try to remove subscriptions with non existing roles
			var groles = guild.Roles.Where(r => !r.IsEveryone).Select(x => x.Id);
			var roleIds = json.Data.Select(x => x.Value).Except(groles).ToList();
			var keys = roleIds.Select(id => json.Data.First(y => y.Value == id).Key);

			// Try to remove subscriptions with non existing channels
			var gchannels = guild.TextChannels.Select(x => x.Id);
			var channelIds = json.Data.Select(x => x.Key).Except(gchannels).ToList();
			keys = keys.Union(channelIds.Select(id => json.Data.First(y => y.Key == id).Key));

			var totalRoleIds = json.Data.Where(x => keys.Contains(x.Key)).Select(x => x.Value).ToList();

			// Remove keys from data
			keys.ToList()
				.ForEach(key =>
					json.Data.Remove(key));
			// Write data
			await CreateServerSub(guild.Id, json);

			// Remove roles which aren't in a subscription anymore
			totalRoleIds
				.ForEach(async x =>
					await guild.GetRole(x).DeleteAsync());

			await guild.DownloadUsersAsync();
			foreach (var user in guild.Users)
				await MaintainUser(guild, user);
		}

		public static async Task MaintainUser(SocketGuild guild, IUser user)
		{
			var path = Path.Combine(rootDir, $"{guild.Id}");
			Directory.CreateDirectory(path);
			if (!File.Exists(Path.Combine(path, $"{user.Id}.json")))
			{
				var json =
					new SubUserJson
					{
						Name = user.GenFullName(),
						UID = user.Id,
						SubRoles = new List<ulong>()
					};
				await CreateUserSub(guild.Id, user.Id, json);
			}		
			else
			{
				var json = LoadSubUserJson(guild.Id, user.Id);

				// Remove role data for non existant roles
				var groles = guild.Roles.Where(r => !r.IsEveryone).Select(x => x.Id);
				var roleIds = json.SubRoles.Except(groles).ToList();
				roleIds.ForEach(x => json.SubRoles.Remove(x));
				await CreateUserSub(guild.Id, user.Id, json, true);
			}
		}

		/// <summary>
		/// Tries to write a server.json file
		/// </summary>
		public static Task<GuideResult> CreateServerSub(ulong guid, SubServerJson input)
		{
			return Task.Run(() =>
			{
				var path = Path.Combine(rootDir, $"{guid}", $"server.json");
				var json = input.Serialize();
				File.WriteAllText(path, json);
				var raw = File.ReadAllText(path);
				var isSuccess = string.Equals(raw, json);
				return
					new GuideResult(
						isSuccess ? null : "Server.json written was not the same as input.", isSuccess);
			});
		}

		/// <summary>
		/// Tries to write a uid.json file
		/// </summary>
		public static Task<GuideResult> CreateUserSub(ulong guid, ulong uid, SubUserJson input, bool ignoreCheck = false)
		{
			return Task.Run(() =>
			{
				if (!ignoreCheck && SubUserExists(guid, uid))
					return
						new GuideResult(
							"Sub already exists", false);

				var path = Path.Combine(rootDir, $"{guid}", $"{uid}.json");
				var json = input.Serialize();
				File.WriteAllText(path, json);
				var raw = File.ReadAllText(path);
				var isSuccess = string.Equals(raw, json);
				return
					new GuideResult(
						isSuccess ? null : $"{uid}.json written was not the same as input.", isSuccess);
			});
		}

		/// <summary>
		/// Tries to delete a uid.json file
		/// </summary>
		public static Task<GuideResult> DeleteUserSub(ulong guid, ulong uid)
		{
			return Task.Run(() =>
			{
				var path = Path.Combine(rootDir, $"{guid}", $"{uid}.json");
				if (SubUserExists(guid, uid))
					File.Delete(path);
				var isSuccess = !File.Exists(path);
				return
					new GuideResult(
						isSuccess ? null : $"{uid}.json still exists after deletion.", isSuccess);
			});
		}

		/// <summary>
		/// Tries to validate all uid.json files
		/// </summary>
		public static async Task<List<string>> ValidateSubs(ulong guid)
		{
			var count = new List<string>();
			foreach (var name in jsonfiles(guid))
			{
				ulong parsed = ulong.Parse(name);
				if (!SubUserExists(guid, parsed))
					continue;

				var json = LoadSubUserJson(guid, parsed);
				var oldJson = new SubUserJson(json).Serialize();
				json.Validate();
				var result = await CreateUserSub(guid, parsed, json, true);
				var newJson = jsonfiles(guid).FirstOrDefault(j => j == $"{json.UID}");
				if (result.IsSuccess && newJson != null &&
					oldJson != File.ReadAllText(Path.Combine(rootDir, $"{guid}", $"{json.UID}")))
					count.Add(json.Name);
			}
			return count;
		}

		public static bool SubUserExists(ulong guid, ulong uid) =>
			jsonfiles(guid).Any(n => string.Equals(n, uid.ToString()));

		public static SubUserJson LoadSubUserJson(string path) =>
			JsonConvert.DeserializeObject<SubUserJson>(File.ReadAllText(path));

		public static SubUserJson LoadSubUserJson(ulong guid, ulong uid) =>
			JsonConvert.DeserializeObject<SubUserJson>(File.ReadAllText(Path.Combine(rootDir, $"{guid}", $"{uid}.json")));

		public static SubServerJson LoadSubServerJson(string path) =>
			JsonConvert.DeserializeObject<SubServerJson>(path);

		public static SubServerJson LoadSubServerJson(ulong guid) =>
			JsonConvert.DeserializeObject<SubServerJson>(File.ReadAllText(Path.Combine(rootDir, $"{guid}", "server.json")));

		public static bool AnySubId(ulong guid, ulong uid) =>
			jsonfiles(guid).Any(n => string.Equals(n, uid.ToString()));
	}
}
