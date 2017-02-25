using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TheGuide.Systems.Helpers;

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
			else
			{
				GUID = default(ulong);
				Data = new Dictionary<ulong, ulong>();
				AdminRoles = new List<ulong>();
			}
		}

		public ulong GUID; // => guild ID
		public Dictionary<ulong, ulong> Data; // key => channelID, value => roleID
		public List<ulong> AdminRoles; // => roleID

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
			else
			{
				Name = string.Empty;
				UID = default(ulong);
				SubRoles = new List<ulong>();
			}
		}

		public string Name;
		public ulong UID;
		public List<ulong> SubRoles;

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
			Path.Combine(Program.AssemblyDirectory, "dist", "subs");

		public static string[] jsonfiles(ulong guildid) =>
			Directory.GetFiles(Path.Combine(rootDir, $"{guildid}"), "*.json")
				.Select(Path.GetFileNameWithoutExtension)
				.ToArray();

		/// <summary>
		/// Tries to maintain directories along with server.json files and uid.json files
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public static async Task Maintain(IDiscordClient client)
		{
			await Task.Run(async () => 
			{
				Directory.CreateDirectory(Directory.GetParent(rootDir).FullName);
				Directory.CreateDirectory(rootDir);
				foreach (var guild in (client as DiscordSocketClient).Guilds)
				{
					var path = Path.Combine(rootDir, $"{guild.Id}");
					Directory.CreateDirectory(path);
					if (!File.Exists(Path.Combine(path, "server.json")))
					{
						await CreateServerSub(guild.Id, new SubServerJson {GUID = guild.Id});
					}
					foreach (var socketGuildUser in guild.Users)
					{
						path = Path.Combine(path, $"{socketGuildUser.Id}.json");
						var userJson = File.Exists(path)
							? LoadSubUserJson(path)
							: new SubUserJson
							{
								Name = socketGuildUser.GenFullName(),
								UID = socketGuildUser.Id,
								SubRoles = new List<ulong>()
							};
						await CreateUserSub(guild.Id, socketGuildUser.Id, userJson);
					}
				}
			});
		}

		/// <summary>
		/// Tries to write a server.json file
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		public static async Task<GuideResult> CreateServerSub(ulong guid, SubServerJson input)
		{
			var path = Path.Combine(rootDir, $"{guid}", $"server.json");
			var json = input.Serialize();
			await Task.Run(() => File.WriteAllText(path, json));
			var raw = await Task.Run(() => File.ReadAllText(path));
			var isSuccess = string.Equals(raw, json);
			return
				new GuideResult(
				isSuccess ? null : "Server.json written was not the same as input.", isSuccess);
		}

		/// <summary>
		/// Tries to write a uid.json file
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="uid"></param>
		/// <param name="input"></param>
		/// <param name="ignoreCheck"></param>
		/// <returns></returns>
		public static async Task<GuideResult> CreateUserSub(ulong guid, ulong uid, SubUserJson input, bool ignoreCheck = false)
		{
			await Task.Yield();
			if (!ignoreCheck && SubUserExists(guid, uid))
				return 
					new GuideResult( 
					"Sub already exists", false);

			var path = Path.Combine(rootDir, $"{guid}", $"{uid}.json");
			var json = input.Serialize();
			await Task.Run(() => File.WriteAllText(path, json));
			var raw = await Task.Run(() => File.ReadAllText(path));
			var isSuccess = string.Equals(raw, json);
			return
				new GuideResult(
				isSuccess ? null : $"{uid}.json written was not the same as input.", isSuccess);
		}

		/// <summary>
		/// Tries to delete a uid.json file
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="uid"></param>
		/// <returns></returns>
		public static async Task<GuideResult> DeleteUserSub(ulong guid, ulong uid)
		{
			await Task.Yield();
			var path = Path.Combine(rootDir, $"{guid}", $"{uid}.json");
			if (SubUserExists(guid, uid))
				await Task.Run(() => File.Delete(path));
			var isSuccess = !File.Exists(path);
			return 
				new GuideResult(
					isSuccess ? null : $"{uid}.json still exists after deletion.", isSuccess);
		}

		/// <summary>
		/// Tries to validate all uid.json files
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
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
