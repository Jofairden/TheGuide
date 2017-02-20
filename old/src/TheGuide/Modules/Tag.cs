using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheGuide.Preconditions;
using TheGuide.Systems;

namespace TheGuide.Modules
{
    [Group("tag")]
    [Name("tag")]
    public class Tag : ModuleBase
    {
        private CommandService _service;
        private IDependencyMap _map;
        private TagSystem _tags;

        public Tag(CommandService _service, IDependencyMap _map)
        {
            this._service = _service;
            this._map = _map;
            _tags = _map.Get<TagSystem>();
        }

        [Command("create")]
        [Summary("Creates a tag")]
        [AdmDevAttr]
        public async Task Create(string name, [Remainder] string input)
        {
            if (!_tags.HasTag(Context.Guild.Id, name))
			{
                _tags.CreateTag(name, new TagSystem.TagJson { Name = name, Output = input }, Context.Guild);
                await ReplyAsync(
                    $"Created tag ``{name}``");
            }
            else
            {
                await ReplyAsync(
                    $"Tag ``{name}`` already exists");
            }
        }

	    [Command("alter")]
	    [Alias("change", "edit")]
	    [AdmDevAttr]
	    public async Task Alter(string name, [Remainder] string input)
	    {
			if (_tags.HasTag(Context.Guild.Id, name))
			{
				_tags.CreateTag(name, new TagSystem.TagJson { Name = name, Output = input }, Context.Guild);
				await ReplyAsync(
					$"Altered tag ``{name}``\nNew output: {input}");
			}
			else
			{
				await ReplyAsync(
					$"Tag ``{name}`` doesn't exist");
			}
		}

        [Command("delete")]
        [Alias("remove", "del")]
        [Summary("deletes a tag")]
        [AdmDevAttr]
        public async Task Delete(string name)
        {
            if (_tags.DeleteTag(Context.Guild, name))
            {
                await ReplyAsync(
                    $"Deleted tag ``{name}``.");
            }
        }

        [Command("get")]
        [Summary("Gets a tag")]
        public async Task Get(string name)
        {
			if (name.StartsWith("[blankAttempt]:"))
				if (!_tags.HasTag(Context.Guild.Id, name.Substring(15)))
					throw new Exception();
				else
					name = name.Substring(15);

		    if (_tags.HasTag(Context.Guild.Id, name))
		    {
			    bool b = await _tags.AttemptExecute(_service, _map, Context, name);
				if (!b)
				    await ReplyAsync(
					    $"{_tags.GetTag(Context.Guild.Id, name)}");
		    }
		    else
		    {
			    await ReplyAsync(
				    $"Tag ``{name}`` not found.");
		    }
        }

        [Command("list")]
        [Summary("Lists all tags")]
        public async Task List()
        {
            await ReplyAsync(
                    $"**Stored tags for {Context.Guild.Name}**\n" +
                    $"{_tags.ListTags()}\n\n" +
                    $"Usage: ?tag get <name>");
        }

    }
}
