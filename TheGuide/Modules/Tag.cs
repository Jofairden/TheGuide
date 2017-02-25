using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheGuide.Preconditions;
using TheGuide.Systems;

namespace TheGuide.Modules
{
	[Group("tag")]
	[Name("tag")]
	public class Tag : ModuleBase
	{
		private readonly CommandService service;
		private readonly IDependencyMap map;

		public Tag(CommandService service, IDependencyMap map)
		{
			this.service = service;
			this.map = map;
		}

		[Command("validate")]
		[Summary("Validates all existing tags for this server")]
		[Remarks("validate")]
		[AdmDevAttr]
		public async Task Validate([Remainder] string rem = null)
		{
			var list = await TagSystem.ValidateTags(Context.Guild.Id);
			if (list.Count > 0)
			{
				await ReplyAsync($"Validated ``{list.Count}`` tags" +
								$"\n\n" +
								$"{string.Join("\n", list.Select(t => $"**{t.Value}** (ID: {t.Key})"))}");
			}
			else
				await ReplyAsync($"No tags needed to be validated.");
		}

		[Command("create")]
		[Alias("make")]
		[Summary("Creates a tag")]
		[Remarks("create <name> <content>\ncreate ExampleTag Some output message.")]
		[AdmDevAttr]
		public async Task Create(string name, [Remainder] string input)
		{
			var json = new TagJson()
			{
				Name = name,
				Output = input,
				TimeCreated = DateTime.Now,
				LastEdited = DateTime.Now,
				Creator = Context.Message.Author.GenFullName(),
				LastEditor = Context.Message.Author.GenFullName(),
			};
			json.Claimers.Add(json.Creator);

			var result = await TagSystem.CreateTag(Context.Guild.Id, name, json);
			string msg =
				result.IsSuccess
					? $"Created tag ``{name}``"
					: result.ErrorReason;
			await ReplyAsync(msg);
		}

		[Command("delete")]
		[Alias("remove")]
		[Summary("deletes a tag")]
		[Remarks("delete <name>\ndelete ExampleTag")]
		[AdmDevAttr]
		public async Task Delete([Remainder] string name)
		{
			var result = await TagSystem.DeleteTag(Context.Guild.Id, name);
			string msg =
				result.IsSuccess
					? $"Deleted tag ``{name}``"
					: result.ErrorReason;
			await ReplyAsync(msg);
		}

		[Command("edit")]
		[Alias("change", "alter")]
		[Summary("Changes the content of a tag")]
		[Remarks("edit <name> <content>\nedit ExampleTag some new output")]
		[AdmDevAttr]
		public async Task Edit(string name, [Remainder] string input)
		{
			TagJson tag = TagSystem.getTag(Context.Guild.Id, name);
			if (tag == null)
			{
				await ReplyAsync($"Tag ``{name}`` not found.");
				return;
			}
			tag.LastEdited = DateTime.Now;
			tag.LastEditor = Context.Message.Author.GenFullName();
			tag.Output = input;
			var result = await TagSystem.WriteTag(Context.Guild.Id, tag);
			string msg =
				result.IsSuccess
					? $"Edited tag ``{name}``"
					: result.ErrorReason;
			await ReplyAsync(msg);
		}

		[Command("get")]
		[Summary("Gets a tag")]
		[Remarks("get <name>\nget ExampleTag")]
		public async Task Get([Remainder] string name)
		{
			name = name.RemoveWhitespace();
			string affix = "[AttemptExecute:]";
			TagJson tag = null;
			if (name.StartsWith(affix))
			{
				name = name.Substring(affix.Length);
				tag = TagSystem.getTag(Context.Guild.Id, name);
				if (tag == null)
					throw new Exception($"Tag ``{name}`` not found.");
			}
			tag = TagSystem.getTag(Context.Guild.Id, name);
			if (tag != null)
			{
				if (await TagSystem.AttemptExecute(service, map, Context, name))
					return;

				affix = tag.Output;
			}
			else
				affix = $"Tag ``{name}`` not found.";

			await ReplyAsync(affix);
		}



		[Command("list")]
		[Summary(
			"Lists all tags or tags owned by specified user. The command will recognize a user by username, discriminator, id or mention."
		)]
		[Remarks("list [user]\nlist --OR-- list Jofairden")]
		public async Task List([Remainder] IUser user = null)
		{
			var tags = TagSystem.tags(Context.Guild.Id).ToArray();
			if (user != null)
				tags = tags.Where(t => t.Claimers.Contains(user.GenFullName())).ToArray();

			string header = user != null
				? $"Tags claimed by {user.GenFullName()}"
				: $"Tags for server {Context.Guild.Name}";

			string content = tags.Any()
				? tags.Select(t => t.Name).PrettyPrint()
				: "No tags found.";

			if (content.Length - header.Length > 2000)
			{
				content = content.Cap(2000 - header.Length);
				var index = content.LastIndexOf(',');
				if (!tags.Any(t => string.Equals(t.Name, content.Substring(index + 1), StringComparison.CurrentCultureIgnoreCase)))
					content = content.Substring(0, index);
			}

			await ReplyAsync($"**{header}**" +
							$"\n\n" +
							$"{content}");
		}

		[Command("info")]
		[Summary("Display info of a tag")]
		[Remarks("info <name>\ninfo SomeTag")]
		public async Task Info(string name)
		{
			TagJson tag = TagSystem.getTag(Context.Guild.Id, name);
			if (tag == null)
			{
				await ReplyAsync($"Tag ``{name}`` not found.");
				return;
			}

			string header = $"Showing info for tag ``{tag.Name}``";
			string content = string.Join("\n",
				JsonConvert.DeserializeObject<JObject>(tag.ToJson())
					.Properties()
					.Select(p => $"**{p.Name}**: {(p.Value.Type == JTokenType.Array ? string.Join(", ", p.Values()) : p.Value)}"));

			if (content.Length - header.Length > 2000)
				content = content.Cap(2000 - header.Length);

			await ReplyAsync($"**{header}**" +
							$"\n\n" +
							$"{content}");

		}

		[Command("claim")]
		[Summary(
			"Attempt to claim a command for yourself, or someone else. To give away a tag you must have a claim on it, or have administrator privileges. The command will recognize a user by username, discriminator, id or mention."
		)]
		[Remarks("claim <tag> <user>\n?claim SomeTag Jofairden")]
		public async Task Claim(IUser user, [Remainder] string name) =>
			await Claim(name.RemoveWhitespace(), user);

		[Command("claim")]
		[Summary(
			"Attempt to claim a command for yourself, or someone else. To give away a tag you must have a claim on it, or have administrator privileges. The command will recognize a user by username, discriminator, id or mention."
		)]
		[Remarks("claim <tag> <user>\n?claim SomeTag Jofairden")]
		public async Task Claim(string name, [Remainder] IUser user = null)
		{
			TagJson tag = TagSystem.getTag(Context.Guild.Id, name);
			if (tag == null)
			{
				await ReplyAsync($"Tag ``{name}`` not found.");
				return;
			}

			var sender = Context.Message.Author as SocketGuildUser;
			var sfull = sender.GenFullName();
			var claimer = user != null ? user.GenFullName() : sfull;
			var claimerIsClaimer = tag.Claimers.Contains(claimer);
			var senderHasClaim =
				sender != null && sender.GuildPermissions.Administrator
				|| tag.Claimers.Contains(sfull)
				|| tag.Claimers.Count <= 0;

			if (!senderHasClaim)
			{
				await ReplyAsync(
					$"The claims of tag ``{tag.Name}`` could not be altered because {sfull} has no claim on it.");
				return;
			}

			if (claimerIsClaimer)
			{
				await ReplyAsync($"{claimer} already has a claim on ``{tag.Name}``!");
				return;
			}

			tag.LastEdited = DateTime.Now;
			tag.LastEditor = sfull;
			tag.Claimers.Add(claimer);
			var result = await TagSystem.WriteTag(Context.Guild.Id, tag);
			await ReplyAsync(result.IsSuccess
				? $"{claimer} now has a claim on ``{tag.Name}``!"
				: $"{result.ErrorReason}");
		}

		[Command("unclaim")]
		[Summary(
			"Attempt to unclaim a command for yourself, or someone else. To unclaim a tag you must own it, or have administrator privileges. The command will recognize a user by username, discriminator, id or mention."
		)]
		[Remarks("unclaim <tag>\n?unclaim SomeTag")]
		public async Task Unclaim(IUser user, [Remainder] string name) =>
			await Unclaim(name.RemoveWhitespace(), user);

		[Command("unclaim")]
		[Summary(
			"Attempt to unclaim a command for yourself, or someone else. To unclaim a tag you must own it, or have administrator privileges. The command will recognize a user by username, discriminator, id or mention."
		)]
		[Remarks("unclaim <tag>\n?unclaim SomeTag")]
		public async Task Unclaim(string name, [Remainder] IUser user = null)
		{
			TagJson tag = TagSystem.getTag(Context.Guild.Id, name);
			if (tag == null)
			{
				await ReplyAsync($"Tag ``{name}`` not found.");
				return;
			}

			var sender = Context.Message.Author as SocketGuildUser;
			var sfull = sender.GenFullName();
			var unclaimer = user != null ? user.GenFullName() : sfull;
			var unclaimerIsClaimer = tag.Claimers.Contains(unclaimer);
			var senderHasClaim =
				sender != null && sender.GuildPermissions.Administrator
				|| tag.Claimers.Contains(sfull)
				|| tag.Claimers.Count <= 0;

			if (!senderHasClaim)
			{
				await ReplyAsync(
					$"The claims of tag ``{tag.Name}`` could not be altered because {sfull} has no claim on it.");
				return;
			}

			if (!unclaimerIsClaimer)
			{
				await ReplyAsync($"Unable to unclaim because {unclaimer} has no claim on ``{tag.Name}``!");
				return;
			}

			if (user != null && tag.Creator == unclaimer)
			{
				await ReplyAsync($"Unable to unclaim because {unclaimer} is the owner of ``{tag.Name}``!");
				return;
			}

			tag.LastEdited = DateTime.Now;
			tag.LastEditor = sfull;
			tag.Claimers.Remove(unclaimer);
			var result = await TagSystem.WriteTag(Context.Guild.Id, tag);
			await ReplyAsync(result.IsSuccess
				? $"{unclaimer} no longer has a claim on ``{tag.Name}``!"
				: $"{result.ErrorReason}");
		}
	}
}

