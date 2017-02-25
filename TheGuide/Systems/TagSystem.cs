using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Newtonsoft.Json;

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
			}
			else
			{
				Name = string.Empty;
				Output = string.Empty;
				TimeCreated = DateTime.MinValue;
				LastEdited = DateTime.MinValue;
				Creator = string.Empty;
				LastEditor = string.Empty;
			}
		}

		public string Name;
		public string Output;
		public DateTime TimeCreated;
		public DateTime LastEdited;
		public string Creator;
		public string LastEditor;

		public override bool Validate()
		{
			return
				!string.IsNullOrEmpty(Name)
				&& !string.IsNullOrEmpty(Output)
				&& !string.IsNullOrEmpty(Creator)
				&& !string.IsNullOrEmpty(LastEditor)
				&& TimeCreated != DateTime.MinValue
				&& LastEdited != DateTime.MinValue;
		}
	}

	// It is common practice here, to put the guildid (ulong) as the first parameter, any abstract types go last.
	public static class TagSystem
	{
		// ./assembly/dist/tags/
		public static string rootDir =>
			Path.Combine(Program.AssemblyDirectory, "dist", "tags");

		// Gets all json filenames from x guild
		public static string[] jsonfiles(ulong id) =>
			Directory.GetFiles(Path.Combine(rootDir, $"{id}"), "*.json")
				.Select(Path.GetFileNameWithoutExtension)
				.ToArray();

		// Attempts to create x tag for y guild with z input, if it does not exist yet.
		public static async Task<GuideResult> CreateTag(ulong guildid, string name, TagJson input)
		{
			await Task.Yield();
			if (TagExists(guildid, name)) return new GuideResult("Tag already exists", false);
			var result = await WriteTag(guildid, name, input);
			return result;
		}

		// Attempts to write x tag for y with z input
		public static async Task<GuideResult> WriteTag(ulong guildid, string name, TagJson input)
		{
			await Task.Yield();
			var path = Path.Combine(rootDir, $"{guildid}", $"{name}.json");
			var json = input.Serialize();
			File.WriteAllText(path, json);
			var text = File.ReadAllText(path);
			var isSuccess = string.Equals(text, json);
			return new GuideResult(isSuccess ? null : "Tag written was not the same as input.", isSuccess);
		}

		public static async Task<GuideResult> DeleteTag(ulong guildid, string name)
		{
			await Task.Yield();
			var path = Path.Combine(rootDir, $"{guildid}", $"{name}.json");
			if (TagExists(guildid, name))
				File.Delete(path);
			var isSuccess = File.Exists(path);
			return new GuideResult(isSuccess ? null : "File still exists after deletion.", isSuccess);
		}

		// Try to list all tag names from guild in format: `tag, tag1, tag2
		public static async Task<string> ListTagNames(ulong guildid)
		{
			await Task.Yield();

			var sb = new StringBuilder();
			var files = jsonfiles(guildid);

			if (files.Length <= 0)
				return "No tags were found.";

			return string.Join(", ", files);
		}

		// Attempts to validate tags
		// Tags which need validation are rewritten
		public static async Task<List<string>> ValidateTags(ulong guildid)
		{
			var count = new List<string>();
			foreach (var name in jsonfiles(guildid))
			{
				if (!TagExists(guildid, name))
					continue;

				var json = LoadTagJson(guildid, name);

				if (!json.Validate())
				{
					var tagjson = new TagJson
					{
						Name = name,
						Output = json.Output,
						TimeCreated = json.TimeCreated,
						LastEdited = json.LastEdited,
						Creator = json.Creator,
						LastEditor = json.LastEditor
					};

					if (tagjson.TimeCreated == DateTime.MinValue)
						tagjson.TimeCreated = DateTime.Now;
					if (tagjson.LastEdited == DateTime.MinValue)
						tagjson.LastEdited = DateTime.Now;
					if (tagjson.Creator == null)
						tagjson.Creator = "Unknown";
					if (tagjson.LastEditor == null)
						tagjson.LastEditor = "Unknown";

					await WriteTag(guildid, name, tagjson);
					count.Add(name);
				}
			}
			return count;
		}

		public static bool TagExists(ulong guildid, string name) =>
			jsonfiles(guildid).Any(n => string.Equals(n, name));

		public static TagJson LoadTagJson(string path) =>
			JsonConvert.DeserializeObject<TagJson>(File.ReadAllText(path));

		public static TagJson LoadTagJson(ulong id, string name) =>
			JsonConvert.DeserializeObject<TagJson>(File.ReadAllText(Path.Combine(rootDir, $"{id}", $"{name}.json")));

		public static bool AnyTagName(ulong guildid, string name) =>
			jsonfiles(guildid).Any(n => string.Equals(name, n));

		// Try to execute a tag, if it wants to run a command
		public static async Task<bool> AttemptExecute(CommandService service, IDependencyMap map, CommandContext context, string name)
		{
			string data = LoadTagJson(context.Guild.Id, name).Output;
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
