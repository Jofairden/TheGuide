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
using TheGuide.Systems.Helpers;

namespace TheGuide.Systems
{
	public class SubServerJson
	{
		public SubServerJson(ulong guid = default(ulong), Dictionary<ulong, ulong> data = null)
		{
			GUID = guid;
			Data = data ?? new Dictionary<ulong, ulong>();
		}

		public ulong GUID;
		// Role ID key representing Channel ID value
		public Dictionary<ulong, ulong> Data;

		public string Serialize() =>
			JsonConvert.SerializeObject(this);

		public bool Validate()
		{
			return GUID != default(ulong)
				&& Data.Any();
		}

		public override string ToString()
		{
			var fields =
			this.GetType()
			.GetFields()
			.Select(fi => new { FieldName = fi.Name, FieldValue = fi.GetValue(this) })
			.ToDictionary(x => x.FieldName, x => x.FieldValue);

			return string.Join("\n", fields.Select(x => $"**{x.Key.AddSpacesToSentence().Uncapitalize()}**: {x.Value}").ToArray());
		}
	}

	public class SubUserJson
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

		public string Name;
		public ulong UID;
		public List<ulong> SubRoles;

		public bool Validate()
		{
			return
				!string.IsNullOrEmpty(Name)
				&& UID != default(ulong)
				&& SubRoles.Any();
		}

		public string Serialize() => JsonConvert.SerializeObject(this);

		public override string ToString()
		{
			var fields =
			this.GetType()
			.GetFields()
			.Select(fi => new { FieldName = fi.Name, FieldValue = fi.GetValue(this) })
			.ToDictionary(x => x.FieldName, x => x.FieldValue);

			return string.Join("\n", fields.Select(x => $"**{x.Key.AddSpacesToSentence().Uncapitalize()}**: {x.Value}").ToArray());
		}
	}

	public class SubResult : IResult
	{
		public SubResult(string y = null, bool z = false)
		{
			Error = null;
			ErrorReason = y;
			IsSuccess = z;
		}

		public void SetCommandError(CommandError? x)
		{
			Error = x;
		}

		public void SetErrorReason(string x)
		{
			ErrorReason = x;
		}

		public void SetIsSuccess(bool x)
		{
			IsSuccess = x;
		}

		public CommandError? Error { get; private set; } = null;

		public string ErrorReason { get; private set; } = null;

		public bool IsSuccess { get; private set; } = false;
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

		// Tries to maintain directories along with server.json files
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
						await CreateServerSub(guild.Id, new SubServerJson(guild.Id));
					}
				}
			});
		}

		// Tries to write a server.json file
		public static async Task<SubResult> CreateServerSub(ulong guid, SubServerJson input)
		{
			var path = Path.Combine(rootDir, $"{guid}", $"server.json");
			var json = input.Serialize();
			await Task.Run(() => File.WriteAllText(path, json));
			var raw = await Task.Run(() => File.ReadAllText(path));
			var isSuccess = string.Equals(raw, json);
			return
				new SubResult(
				isSuccess ? null : "Server.json written was not the same as input.", isSuccess);
		}

		// Tries to write a uid.json file
		public static async Task<SubResult> CreateUserSub(ulong guid, ulong uid, SubUserJson input, bool ignoreCheck = false)
		{
			await Task.Yield();
			if (!ignoreCheck && SubUserExists(guid, uid))
				return 
					new SubResult( 
					"Sub already exists", false);

			var path = Path.Combine(rootDir, $"{guid}", $"{uid}.json");
			var json = input.Serialize();
			await Task.Run(() => File.WriteAllText(path, json));
			var raw = await Task.Run(() => File.ReadAllText(path));
			var isSuccess = string.Equals(raw, json);
			return
				new SubResult(
				isSuccess ? null : $"{uid}.json written was not the same as input.", isSuccess);
		}

		// Tries to delete a uid.json file
		public static async Task<SubResult> DeleteTag(ulong guid, ulong uid)
		{
			await Task.Yield();
			var path = Path.Combine(rootDir, $"{guid}", $"{uid}.json");
			if (SubUserExists(guid, uid))
				await Task.Run(() => File.Delete(path));
			var isSuccess = !File.Exists(path);
			return 
				new SubResult(
					isSuccess ? null : $"{uid}.json still exists after deletion.", isSuccess);
		}

		// Tries to validate all uid.json files
		public static async Task<List<string>> ValidateSubs(ulong guid)
		{
			var count = new List<string>();
			foreach (var name in jsonfiles(guid))
			{
				ulong parsed = ulong.Parse(name);
				if (!SubUserExists(guid, parsed))
					continue;

				var json = LoadSubUserJson(guid, parsed);

				if (!json.Validate())
				{
					var subjson = new SubUserJson
					{
						Name = name,
						SubRoles = json.SubRoles
					};

					if (string.IsNullOrEmpty(subjson.Name))
						subjson.Name = "Unknown";
					if (subjson.SubRoles == null)
						subjson.SubRoles = new List<ulong>();

					await CreateUserSub(guid, parsed, subjson, true);
					count.Add(name);
				}
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
