using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
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
			}
		}

		public string Name;
		public string Output;
		public DateTime TimeCreated;
		public DateTime LastEdited;
		public string Creator;
		public string LastEditor;
		public List<string> Claimers;

		public override bool Validate()
		{
			return
				!string.IsNullOrEmpty(Name)
				&& !string.IsNullOrEmpty(Output)
				&& !string.IsNullOrEmpty(Creator)
				&& !string.IsNullOrEmpty(LastEditor)
				&& TimeCreated != DateTime.MinValue
				&& LastEdited != DateTime.MinValue
				&& Claimers.Any();
		}
	}

	public static class TagSystem
	{
		private static Id64Generator idGen = new Id64Generator();

		// ./assembly/dist/tags/
		private static string rootDir =>
			Path.Combine(Program.AssemblyDirectory, "dist", "tags");

		// Gets all json filenames from x guild
		public static string[] tagFiles(ulong id) =>
			Directory.GetFiles(Path.Combine(rootDir, $"{id}"), "*.json")
				.Select(Path.GetFileNameWithoutExtension)
				.ToArray();

		



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
