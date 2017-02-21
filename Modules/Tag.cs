using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using TheGuide.Preconditions;
using TheGuide.Systems;

namespace TheGuide.Modules
{
	[Group("tag")]
	[Name("tag")]
	public class Tag : ModuleBase
	{
		private readonly CommandService _service;
		private readonly IDependencyMap _map;

		public Tag(CommandService _service, IDependencyMap _map)
		{
			this._service = _service;
			this._map = _map;
		}

		[Command("validate")]
		[Summary("Validates all existing tags for this server")]
		[Remarks("validate")]
		[OwnerAttr]
		public async Task Validate([Remainder] string rem = null)
		{
			var list = await TagSystem.ValidateTags(Context.Guild.Id);
			if (list.Count > 0)
			{
				await ReplyAsync($"Validated ``{list.Count}`` tags\n" +
								$"{string.Join(", ", list)}");
			}
			else
				await ReplyAsync($"No tags needed to be validated.");
		}

		[Command("create")]
		[Summary("Creates a tag")]
		[Remarks("create <name> <content>\ncreate ExampleTag Some output message.")]
		[AdmDevAttr]
		public async Task Create([Summary("the tag name")]string name, [Remainder][Summary("the tag content")] string input)
		{
			if (TagSystem.TagExists(Context.Guild.Id, name))
			{
				await ReplyAsync($"Tag ``{name}`` already exists");
				return;
			}

			var creator = Context.Message.Author.GenFullName();
			var tagjson = new TagJson
			{
				Name = name,
				Output = input,
				TimeCreated = DateTime.Now,
				LastEdited = DateTime.Now,
				Creator = creator,
				LastEditor = creator
			};
			var result = await TagSystem.CreateTag(Context.Guild.Id, name, tagjson);
			if (result.IsSuccess)
				await ReplyAsync($"Created tag ``{name}``");
			else
				await ReplyAsync($"Something went wrong!\n{result.ErrorReason}");
		}

		[Command("edit")]
		[Alias("change", "alter")]
		[Summary("Changes the content of a tag")]
		[Remarks("edit <name> <content>\nedit ExampleTag some new output")]
		[AdmDevAttr]
		public async Task Edit([Summary("the tag name")]string name, [Remainder][Summary("the tag content")] string input)
		{
			if (!TagSystem.TagExists(Context.Guild.Id, name))
			{
				await ReplyAsync($"Tag ``{name}`` doesn\'t exists");
				return;
			}

			var oldTag = TagSystem.LoadTagJson(Path.Combine(TagSystem.rootDir, $"{Context.Guild.Id}", $"{name}.json"));
			var tagjson = new TagJson
			{
				Name = name,
				Output = input,
				TimeCreated = oldTag.TimeCreated,
				LastEdited = DateTime.Now,
				Creator = oldTag.Creator,
				LastEditor = Context.Message.Author.GenFullName()
			};
			var result = await TagSystem.WriteTag(Context.Guild.Id, name, tagjson);
			if (result.IsSuccess)
				await ReplyAsync($"Changed tag ``{name}``");
			else
				await ReplyAsync($"Something went wrong!\n" +
								$"{result.ErrorReason}");
		}

		[Command("delete")]
		[Alias("remove")]
		[Summary("deletes a tag")]
		[Remarks("delete <name>\ndelete ExampleTag")]
		[AdmDevAttr]
		public async Task Delete([Remainder][Summary("name of tag")] string name)
		{
			var usename = name.RemoveWhitespace();
			if (!TagSystem.TagExists(Context.Guild.Id, usename))
			{
				await ReplyAsync($"Tag ``{usename}`` doesn\'t exists");
				return;
			}

			var result = await TagSystem.DeleteTag(Context.Guild.Id, usename);
			if (result.IsSuccess)
				await ReplyAsync($"Deleted tag ``{usename}``");
			else
				await ReplyAsync($"Something went wrong!\n" +
								$"{result.ErrorReason}");
		}

		[Command("get")]
		[Summary("Gets a tag")]
		[Remarks("get <name>\nget ExampleTag")]
		public async Task Get([Remainder][Summary("name of tag")] string name)
		{
			const string affix = "[blankAttempt]:";
			var usename = name.RemoveWhitespace();
			if (usename.StartsWith(affix))
				if (!TagSystem.TagExists(Context.Guild.Id, usename.Substring(affix.Length)))
					throw new Exception("Blah");
				else
					usename = usename.Substring(affix.Length);

			if (TagSystem.TagExists(Context.Guild.Id, usename))
			{
				if (await TagSystem.AttemptExecute(_service, _map, Context, usename))
					return;

				await ReplyAsync($"{TagSystem.LoadTagJson(Context.Guild.Id, usename).Output}");
				return;
			}
			await ReplyAsync($"Tag ``{usename}`` not found.");
		}

		[Command("list")]
		[Summary("Lists all tags or tags owned by specified user. The command will recognize a user by username, discriminator, id or mention.")]
		[Remarks("list [user]\nlist --OR-- list Jofairden")]
		public async Task List([Remainder] IUser user = null)
		{
			string names = "";
			string header = "Stored tags for ";
			if (user == null)
			{
				names = await TagSystem.ListTagNames(Context.Guild.Id);
				header += Context.Guild.Name;
			}
			else
			{
				var tags = new List<string>();
				var sender = user as SocketGuildUser;
				var jsonnames = TagSystem.jsonfiles(Context.Guild.Id);
				foreach (var jsonname in jsonnames)
				{
					var jsondata = TagSystem.LoadTagJson(Context.Guild.Id, jsonname);
					if (jsondata.Creator == sender.GenFullName())
						tags.Add(jsonname);
				}
				names = string.Join(", ", tags).Cap(2000);
				var name = user.GenFullName();
				if (string.IsNullOrEmpty(names))
					names = $"No tags owned by {name}.";
				header = $"Tags owned by {name}";
			}

			await ReplyAsync(
				$"{Format.Bold($"{header}")}\n" +
				$"{names}\n\n" +
				$"Usage: {CommandHandler.prefixChar}tag get <name>");
		}

		[Command("info")]
		[Summary("Display info of a tag")]
		[Remarks("info <name>\ninfo SomeTag")]
		public async Task Info(string name)
		{
			if (!TagSystem.AnyTagName(Context.Guild.Id, name))
			{
				await ReplyAsync($"Tag ``{name}`` was not found.");
				return;
			}

			var tagjson = TagSystem.LoadTagJson(Context.Guild.Id, name);
			await ReplyAsync($"Info for tag ``{name}``\n" +
							$"{tagjson}");
		}

		// Attempt to claim a command for yourself, or someone else. (must be admin)
		[Command("claim")]
		[Summary("Attempt to claim a command for yourself, or someone else. To give away a tag you must own it, or have administrator privileges. The command will recognize a user by username, discriminator, id or mention.")]
		[Remarks("claim <tag> <user>\n?claim SomeTag Jofairden")]
		public async Task Claim([Summary("name of tag")]string name, [Remainder][Summary("user to claim tag")] IUser user = null)
		{
			if (!TagSystem.AnyTagName(Context.Guild.Id, name))
			{
				await ReplyAsync($"Tag ``{name}`` was not found.");
				return;
			}

			var tagjson = new TagJson(TagSystem.LoadTagJson(Context.Guild.Id, name));
			var sender = Context.Message.Author as SocketGuildUser;
			var claimer = user != null ? user.GenFullName() : sender.GenFullName();
			var senderIsAdmin = sender.Roles.Any(r => r.Permissions.Administrator);
			var senderIsOwner = string.Equals(tagjson.Creator, sender.GenFullName(), StringComparison.CurrentCultureIgnoreCase);

			if (!senderIsOwner && !senderIsAdmin)
			{
				await ReplyAsync($"The ownership of tag ``{name}`` could not be transferred because {sender.GenFullName()} has no claim on it.");
				return;
			}

			var claimerIsOwner = string.Equals(tagjson.Creator, claimer, StringComparison.CurrentCultureIgnoreCase);

			if (claimerIsOwner)
			{
				await ReplyAsync($"{tagjson.Creator} already has a claim on tag ``{name}``!");
				return;
			}

			tagjson.Creator = claimer;
			tagjson.LastEditor = sender.GenFullName();

			var result = await TagSystem.WriteTag(Context.Guild.Id, name, tagjson);
			if (result.IsSuccess)
				await ReplyAsync($"Tag ``{name}`` successfully claimed by {claimer}.");
			else
				await ReplyAsync($"Something went wrong!\n" +
								$"{result.ErrorReason}");
		}

		// Attempt to unclaim a command for yourself, or someone else. (must be admin)
		[Command("unclaim")]
		[Summary("Attempt to unclaim a command for yourself, or someone else. To unclaim a tag you must own it, or have administrator privileges. The command will recognize a user by username, discriminator, id or mention.")]
		[Remarks("unclaim <tag>\n?unclaim SomeTag")]
		public async Task Unclaim([Summary("name of tag")] string name, [Remainder][Summary("user to unclaim the tag")] IUser user = null)
		{
			if (!TagSystem.AnyTagName(Context.Guild.Id, name))
			{
				await ReplyAsync($"Tag ``{name}`` was not found.");
				return;
			}

			var guildUser = Context.Message.Author as SocketGuildUser;
			var claimer = guildUser?.GenFullName();
			var isAdmin = guildUser.Roles.Any(r => r.Permissions.Administrator);

			if (user != null && !isAdmin)
			{
				await ReplyAsync($"{claimer} cannot unclaim {name} for {user.GenFullName()} because no Administrator privilege is present.");
				return;
			}

			var tagjson = new TagJson(TagSystem.LoadTagJson(Context.Guild.Id, name));
			var claimerIsOwner = string.Equals(tagjson.Creator, claimer, StringComparison.CurrentCultureIgnoreCase);

			if (!claimerIsOwner && !isAdmin)
			{
				await ReplyAsync($"Tag ``{name}`` is not {claimer}\'s claim.");
				return;
			}

			var prevOwner = tagjson.Creator;
			tagjson.Creator = "Unknown";
			tagjson.LastEditor = claimer;

			var result = await TagSystem.WriteTag(Context.Guild.Id, name, tagjson);
			if (result.IsSuccess)
				await ReplyAsync($"Tag ``{name}`` is no longer {prevOwner}\'s claim.\nAnyone is now free to claim this tag.");
			else
				await ReplyAsync($"Something went wrong!\n" +
								$"{result.ErrorReason}");
		}
	}

}
