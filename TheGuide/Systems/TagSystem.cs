using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using TheGuide.Systems.Snowflake;

namespace TheGuide.Systems
{
	public class TagJson : GuideJson
	{
		public TagJson(TagJson source = null)
		{
			if (source != null)
			{
				Name = source.Name;
				Output = source.Output;
				TimeCreated = source.TimeCreated;
				LastEdited = source.LastEdited;
				Creator = source.Creator;
				LastEditor = source.LastEditor;
				Claimers = source.Claimers;
				ID = default(long);
			}
			else
			{
				Name = string.Empty;
				Output = string.Empty;
				TimeCreated = DateTime.MinValue;
				LastEdited = DateTime.MinValue;
				Creator = string.Empty;
				LastEditor = string.Empty;
				Claimers = new List<string>();
				ID = default(long);
			}
		}

		public string Name;
		public string Output;
		public DateTime TimeCreated;
		public DateTime LastEdited;
		public string Creator;
		public string LastEditor;
		public List<string> Claimers;
		public long ID;

		public override void Validate(long? id)
		{
			Name = Name == string.Empty ? "Unknown" : Name;
			Output = Output ?? string.Empty;
			TimeCreated = TimeCreated == DateTime.MinValue ? DateTime.Now : TimeCreated;
			LastEdited = LastEdited == DateTime.MinValue ? DateTime.Now : LastEdited;
			Creator = Creator == string.Empty ? "Unknown" : Creator;
			LastEditor = LastEditor == string.Empty ? "Unknown" : LastEditor;
			Claimers = !Claimers.Any() ? new List<string>() : Claimers;
			ID = ID == default(long) ? id ?? default(long) : ID;
		}
	}

	public static class TagSystem
	{
		private static readonly Id64Generator idGen = new Id64Generator();

		// ./assembly/dist/tags/
		private static string rootDir =>
			Path.Combine(Program.AssemblyDirectory, "dist", "tags");

		// Gets all json filenames from x guild
		public static IEnumerable<TagJson> tags(ulong guid) =>
			Directory.GetFiles(Path.Combine(rootDir, $"{guid}"), "*.json")
				.Select(x => JsonConvert.DeserializeObject<TagJson>(File.ReadAllText(x)));

		public static Dictionary<string, TagJson> jsonfiles(ulong guid) =>
			Directory.GetFiles(Path.Combine(rootDir, $"{guid}"), "*.json")
				.ToDictionary(x => x, x => JsonConvert.DeserializeObject<TagJson>(File.ReadAllText(x)));

		public static TagJson getTag(ulong guid, string name) =>
			tags(guid)
				.FirstOrDefault(t => string.Equals(t.Name, name.RemoveWhitespace(), StringComparison.CurrentCultureIgnoreCase));

		public static TagJson getTag(ulong guid, long tid) =>
			tags(guid)
				.FirstOrDefault(t => t.ID == tid);

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
					var path = Path.Combine(rootDir, $"{guild.Id}");
					Directory.CreateDirectory(path);
				}
			});
		}

		/// <summary>
		/// Creates a tag
		/// </summary>
		public static async Task<GuideResult> CreateTag(ulong guid, string name, TagJson input, bool check = true)
		{
			await Task.Yield();
			if (check && tags(guid).Any(t => string.Equals(t.Name, name, StringComparison.CurrentCultureIgnoreCase)))
				return new GuideResult($"Tag ``{name}`` already exists!");
			var result = await WriteTag(guid, input);
			return result;
		}

		/// <summary>
		/// Writes a tag
		/// </summary>
		public static async Task<GuideResult> WriteTag(ulong guid, TagJson input)
		{
			await Task.Yield();
			long useId = input.ID != default(long) ? input.ID : idGen.GenerateId();
			input.ID = useId;
			File.WriteAllText(Path.Combine(rootDir, $"{guid}", $"{useId}.json"), JsonConvert.SerializeObject(input));
			var result = new GuideResult();
			result.SetIsSuccess(tags(guid).Any(t => t.ID == useId));
			return result;
		}

		/// <summary>
		/// Deletes a tag
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static async Task<GuideResult> DeleteTag(ulong guid, string name)
		{
			await Task.Yield();
			var tag = tags(guid).FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.CurrentCultureIgnoreCase));
			if (tag == null)
				return new GuideResult($"Tag ``{name}`` not found!");
			File.Delete(Path.Combine(rootDir, $"{guid}", $"{tag.ID}.json"));
			return new GuideResult("", true);
		}

		/// <summary>
		/// Will validate all tags
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		public static async Task<Dictionary<long, string>> ValidateTags(ulong guid)
		{
			var validatedTags = new Dictionary<long, string>();
			var copy = new List<TagJson>(tags(guid));
			foreach (var tag in copy)
			{
				var oldJson = new TagJson(tag).Serialize();
				tag.Validate(tag.ID);
				var result = await WriteTag(guid, tag);
				var newJson = getTag(guid, tag.ID);
				if (result.IsSuccess && newJson != null && newJson.Serialize() != oldJson)
					validatedTags.Add(tag.ID, tag.Name);
			}
			await RestoreIDs(guid);

			return validatedTags;
		}

		/// <summary>
		/// Will attempt to restore tag IDs by their filename, removes files not named by ID
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		private static async Task RestoreIDs(ulong guid)
		{
			await Task.Yield();

			foreach (var kvp in jsonfiles(guid))
			{
				var json = JsonConvert.DeserializeObject<TagJson>(File.ReadAllText(kvp.Key));
				long parsed;
				if (long.TryParse(Path.GetFileNameWithoutExtension(kvp.Key), out parsed))
				{
					if (json.ID == default(long))
					{
						json.ID = parsed;
						File.WriteAllText(kvp.Key, json.Serialize());
					}
				}
				else
				{
					File.Delete(kvp.Key);
				}
			}
		}

		/// <summary>
		/// Try to execute a tag, if it wants to run a command
		/// </summary>
		/// <param name="service"></param>
		/// <param name="map"></param>
		/// <param name="context"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static async Task<bool> AttemptExecute(CommandService service, IDependencyMap map, CommandContext context, string name)
		{
			var tag = getTag(context.Guild.Id, name);
			if (tag == null)
				return false;

			string data = tag.Output;
			var affix = "command:";
			// Does not run a command
			if (!data.StartsWith(affix) || (!data.RemoveWhitespace().StartsWith("command:tagget") && data.RemoveWhitespace().StartsWith("command:tag")))
				return false;

			//var newContext = new DummyContext(context.Client, context.Guild, context.Channel, context.User);
			await service.ExecuteAsync(context, $"{data.Substring(affix.Length)}", map, MultiMatchHandling.Best);

			return true;
		}
	}
}
