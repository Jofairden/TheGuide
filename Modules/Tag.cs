using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using TheGuide.Preconditions;

namespace TheGuide.Modules
{
	[Group("tag")]
	[Name("tag")]
	public class Tag : ModuleBase
	{
		private CommandService _service;
		private IDependencyMap _map;

		public Tag(CommandService _service, IDependencyMap _map)
		{
			this._service = _service;
			this._map = _map;
		}

		[Command("create")]
		[Summary("Creates a tag")]
		[Remarks("create <name> <output>\ncreate example-Tag Some output message.")]
		[AdmDevAttr]
		public async Task Create(string name, [Remainder] string input)
		{
			if (TagSystem.TagExists(Context.Guild.Id, name))
			{
				await ReplyAsync(
					$"Tag ``{name}`` already exists");
				return;
			}

			await TagSystem.CreateTag(
				name, new TagJson { Name = name, Output = input }, Context.Guild.Id);
			await ReplyAsync(
				$"Created tag ``{name}``");
		}

		[Command("alter")]
		[Alias("change", "edit")]
		[AdmDevAttr]
		public async Task Alter(string name, [Remainder] string input)
		{
			if (!TagSystem.TagExists(Context.Guild.Id, name))
			{
				await ReplyAsync(
					$"Tag ``{name}`` doesn\'t exists");
				return;
			}

			await TagSystem.WriteTag(
				name, new TagJson { Name = name, Output = input }, Context.Guild.Id);
			await ReplyAsync(
				$"Changed tag ``{name}``");
		}

		[Command("delete")]
		[Alias("remove", "del")]
		[Summary("deletes a tag")]
		[AdmDevAttr]
		public async Task Delete(string name)
		{
			if (!TagSystem.TagExists(Context.Guild.Id, name))
			{
				await ReplyAsync(
					$"Tag ``{name}`` doesn\'t exists");
				return;
			}

			await TagSystem.DeleteTag(name, Context.Guild.Id);
			await ReplyAsync($"Deleted tag ``{name}``");
		}

		[Command("get")]
		[Summary("Gets a tag")]
		public async Task Get(string name)
		{
			var affix = "[blankAttempt]:";
			if (name.StartsWith(affix))
				if (!TagSystem.TagExists(Context.Guild.Id, name.Substring(affix.Length)))
					throw new Exception();
				else
					name = name.Substring(affix.Length);

			if (TagSystem.TagExists(Context.Guild.Id, name))
			{
				if (await TagSystem.AttemptExecute(_service, _map, Context, name))
					return;

				await ReplyAsync($"{await TagSystem.GetTag(Context.Guild.Id, name)}");
				return;
			}
			await ReplyAsync($"Tag ``{name}`` not found.");
		}

		[Command("list")]
		[Summary("Lists all tags")]
		public async Task List()
		{
			await ReplyAsync(
					$"**Stored tags for {Context.Guild.Name}**\n" +
					$"{await TagSystem.ListTags(Context.Guild.Id)}\n\n" +
					$"Usage: ?tag get <name>");
		}


	}

}
