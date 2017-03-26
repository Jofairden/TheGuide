using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace TheGuide.Systems
{
	public class ConfigJson : GuideJson
	{
		public ConfigJson(ConfigJson source = null)
		{
			if (source != null)
			{
				guid = source.guid;
				listenCh = source.listenCh;
				admRoles = source.admRoles;
			}
		}

		public ulong guid { get; set; }
		public List<ulong> admRoles { get; set; } = new List<ulong>();
		public ulong listenCh { get; set; }

		public override void Validate(ulong? id)
		{
			guid = guid == default(ulong) ? id ?? default(ulong) : guid;
			admRoles = admRoles ?? new List<ulong>();
		}
	}

	public static class ConfigSystem
	{
		private static string rootDir =>
			Path.Combine(AppContext.BaseDirectory, "dist", "configs");

		public static ConfigJson config(ulong guid) =>
			config(Path.Combine(rootDir, $"{guid}.json"));

		public static ConfigJson config(string path) =>
			JsonConvert.DeserializeObject<ConfigJson>(Tools.FileReadToEnd(Program._locker, path));

		public static IEnumerable<ulong> configs() =>
			Directory.GetFiles(rootDir, "*.json")
				.Select(x => JsonConvert.DeserializeObject<ConfigJson>(Tools.FileReadToEnd(Program._locker, x)).guid);

		public static Dictionary<ulong, ConfigJson> jsonfiles(ulong guid) =>
			Directory.GetFiles(rootDir, "*.json")
				.ToDictionary(x => ulong.Parse(Path.GetFileNameWithoutExtension(x)), config);

		/// <summary>
		/// Maintains content
		/// </summary>
		public static Task Maintain(IDiscordClient client)
		{
			return Task.Run(() =>
			{
				Directory.CreateDirectory(rootDir);

				var discordSocketClient = client as DiscordSocketClient;
				if (discordSocketClient == null) return;
				foreach (var guild in discordSocketClient.Guilds)
				{
					var path = Path.Combine(rootDir, $"{guild.Id}.json");
					if (File.Exists(path)) continue;
					var json = new ConfigJson { guid = guild.Id };
					Tools.FileWrite(Program._locker, path, json.SerializeToJson());
				}
			});
		}

		/// <summary>
		/// Creates a config
		/// </summary>
		public static Task<GuideResult> WriteConfig(ulong guid, ConfigJson input, bool check = true)
		{
			return Task.Run(() =>
			{
				if (check
					&& !configs().Contains(guid))
					return new GuideResult($"Config for {guid} not found");
				Tools.FileWrite(Program._locker, Path.Combine(rootDir, $"{guid}.json"), input.SerializeToJson());
				var result = new GuideResult();
				result.SetIsSuccess(configs().Contains(guid));
				return result;
			});
		}
	}
}
