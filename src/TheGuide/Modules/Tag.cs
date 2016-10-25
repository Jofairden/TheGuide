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
        private CommandService service;
        private IDependencyMap map;
        private TagSystem tags;

        public Tag(CommandService _service, IDependencyMap _map)
        {
            service = _service;
            map = _map;
            tags = _map.Get<TagSystem>();
        }

        [Command("create")]
        [Alias("make")]
        [Summary("Creates a tag")]
        [AdmDevAttr]
        public async Task create(string name, [Remainder] string input)
        {
            if (!tags.HasTag(name))
            {
                tags.CreateTag(name, new TagSystem.TagJson { name = name, output = input });
                await ReplyAsync(
                    $"Created tag ``{name}``");
            }
            else
            {
                await ReplyAsync(
                    $"Tag ``{name}`` already exists");
            }
        }

        [Command("delete")]
        [Alias("remove", "del")]
        [Summary("deletes a tag")]
        [AdmDevAttr]
        public async Task delete(string name)
        {
            if (tags.DeleteTag(name))
            {
                await ReplyAsync(
                    $"Deleted tag ``{name}``.");
            }
        }

        [Command("get")]
        [Summary("Gets a tag")]
        public async Task get(string name)
        {
            if (tags.HasTag(name))
            {
                await ReplyAsync(
                    $"{tags.GetTag(name)}");
            }
            else
            {
                await ReplyAsync(
                    $"Tag ``{name}`` not found.");
            }
        }

        [Command("list")]
        [Summary("Lists all tags")]
        public async Task list()
        {
            await ReplyAsync(
                    $"**Usable tags**\n" +
                    $"{tags.ListTags()}\n\n" +
                    $"Usage: ?tag get <name>");
        }

    }
}
