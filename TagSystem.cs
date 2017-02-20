using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TheGuide
{
    public static class TagSystem
    {
	    private static string _rootDir =>
		    Path.Combine(Program.AssemblyDirectory, "dist", "tags");

	    private static string[] jsonfiles(ulong id) =>
		    Directory.GetFiles(Path.Combine(_rootDir, $"{id}"), "*.json")
			    .Select(Path.GetFileNameWithoutExtension)
			    .ToArray();

		public static async Task CreateTag(string name, TagJson input, ulong id)
		{
			await Task.Yield();
			if (TagExists(id, name)) return;
			await WriteTag(name, input, id);
		}

	    public static async Task WriteTag(string name, TagJson input, ulong id)
	    {
			await Task.Yield();
			string path = Path.Combine(_rootDir, $"{id}", $"{name}.json");
			string json = JsonConvert.SerializeObject(input);
			File.WriteAllText(path, json);
		}

		public static async Task DeleteTag(string name, ulong id)
		{
			await Task.Yield();
			if (TagExists(id, name))
				File.Delete(
					Path.Combine(
						_rootDir, $"{id}", $"{name}.json"));
		}

		public static async Task<string> ListTags(ulong id)
		{
			await Task.Yield();

			var sb = new StringBuilder();
			var files = jsonfiles(id);

			if (files.Length > 0)
			{
				files.ToList().ForEach(n => sb.Append($"{n}, "));
				return sb.ToString().Truncate(2);
			}

			return "no tags found";
		}

	    public static bool TagExists(ulong id, string name)
		    => jsonfiles(id).Any(
				n =>
					string.Equals(n, name));

	    public static async Task<string> GetTag(ulong id, string name)
	    {
		    await Task.Yield();
		    var json =
			    await LoadJson(
				    Path.Combine(
					    _rootDir, $"{id}", $"{name}.json"));
		    return json.Output;
	    }

		public static async Task<TagJson> LoadJson(string path)
		{
			await Task.Yield();
			TagJson json =
				JsonConvert.DeserializeObject<TagJson>(
					File.ReadAllText(
						path));
			return json;
		}

		public static async Task<bool> AttemptExecute(CommandService service, IDependencyMap map, CommandContext context, string name)
		{
			string data = await GetTag(context.Guild.Id, name);
			// command:github jquery in:name,description
			var affix = "command:";
			if (!data.StartsWith(affix) || data.StartsWith("command:tag"))
				return false;

			//var newContext = new DummyContext(context.Client, context.Guild, context.Channel, context.User);
			await service.ExecuteAsync(context, $"{data.Substring(affix.Length)}", map, MultiMatchHandling.Best);

			return true;
		}
	}
}
