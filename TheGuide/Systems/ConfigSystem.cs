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
			else
			{
				guid = default(ulong);
				listenCh = default(ulong);
				admRoles = new List<ulong>();
			}
		}

		public ulong guid;
		public List<ulong> admRoles;
		public ulong listenCh;

		public override void Validate(ulong? id)
		{
			guid = guid == default(ulong) ? id ?? default(ulong) : guid;
			admRoles = admRoles ?? new List<ulong>();
		}
	}

	public static class ConfigSystem
    {
		private static string rootDir =>
			Path.Combine(Program.AssemblyDirectory, "dist", "configs");

	    public static ConfigJson config(ulong guid) =>
		    JsonConvert.DeserializeObject<ConfigJson>(File.ReadAllText(Path.Combine(rootDir, $"{guid}.json")));

		public static ConfigJson config(string path) =>
			JsonConvert.DeserializeObject<ConfigJson>(File.ReadAllText(path));

		public static IEnumerable<ulong> configs() =>
			Directory.GetFiles(rootDir, "*.json")
				.Select(x => JsonConvert.DeserializeObject<ConfigJson>(File.ReadAllText(x)).guid);

		public static Dictionary<ulong, ConfigJson> jsonfiles(ulong guid) =>
			Directory.GetFiles(rootDir, "*.json")
				.ToDictionary(x => ulong.Parse(Path.GetFileNameWithoutExtension(x)), config);

		/// <summary>
		/// Maintains content
		/// </summary>
		public static async Task Maintain(IDiscordClient client)
		{
			await Task.Run(() =>
			{
				Directory.CreateDirectory(Directory.GetParent(rootDir).FullName);
				Directory.CreateDirectory(rootDir);

				foreach (var guild in (client as DiscordSocketClient).Guilds)
				{
					var path = Path.Combine(rootDir, $"{guild.Id}.json");
					if (!File.Exists(path))
					{
						var json = new ConfigJson {guid = guild.Id};
						File.WriteAllText(path, json.SerializeToJson());
					}
				}
			});
		}

		/// <summary>
		/// Creates a config
		/// </summary>
		public static async Task<GuideResult> WriteConfig(ulong guid, ConfigJson input, bool check = true)
		{
			await Task.Yield();
			if (check && configs().Any(t => t == guid))
				return new GuideResult($"Config for {guid} not found");
			var result = await WriteConfig(guid, input);
			return result;
		}

		/// <summary>
		/// Writes a config
		/// </summary>
		public static async Task<GuideResult> WriteConfig(ulong guid, ConfigJson input)
		{
			await Task.Yield();
			File.WriteAllText(Path.Combine(rootDir, $"{guid}.json"), input.SerializeToJson());
			var result = new GuideResult();
			result.SetIsSuccess(configs().Any(t => t == guid));
			return result;
		}
	}
}
