using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using TheGuide;
using TheGuide.Preconditions;
using TheGuide.Systems;
using TheGuide.Systems.Snowflake;

namespace TheGuide.Modules
{
	//todo: config: which module is 'default'?
	[Name("default")]
	public class Commands : ModuleBase
	{
		private readonly CommandService service;
		private readonly IDependencyMap map;

		public Commands(CommandService service, IDependencyMap map)
		{
			this.service = service;
			this.map = map;
		}

		/// <summary>
		/// Generates snowflake IDs
		/// </summary>
		[Command("snowflake")]
		[Summary("Will generate up to 10 snowflake ids and guids")]
		[Alias("snowflake [amount]\nsnowflake 10")]
		[AdmDevAttr]
		public async Task SnowFlake([Remainder] int rem = 1)
		{
			rem = Math.Max(0, Math.Min(10, rem));
			var gen = new Id64Generator();
			long[] ids = gen.Take(rem).ToArray();
			await ReplyAsync($"Generated snowflake ids:\n{string.Join("\n", ids)}");
			var genGuid = new IdGuidGenerator();
			Guid[] guids = genGuid.Take(rem).ToArray();
			await ReplyAsync($"Generated snowflake guids:\n{string.Join("\n", guids)}");
		}

		/// <summary>
		/// Returns bot version
		/// </summary>
		[Command("version")]
		[Summary("returns the bot version")]
		[Remarks("version")]
		public async Task Version([Remainder] string rem = null) =>
			await ReplyAsync(
				$"I am running on ``{Program.version}``\n" +
				$"Use ``{CommandHandler.prefixChar}info`` for more elaborate information.");

		/// <summary>
		/// Returns bot response time
		/// </summary>
		[Command("ping")]
		[Alias("status")]
		[Summary("Returns the bot response time")]
		[Remarks("ping")]
		public async Task Ping([Remainder] string rem = null)
		{
			var d = 60d / (Context.Client as DiscordSocketClient)?.Latency * 1000;
			if (d != null)
				await ReplyAsync(
					$"My heartrate is ``{(int)d}`` bpm ({(Context.Client as DiscordSocketClient)?.Latency} ms)");
		}

		/// <summary>
		/// Returns bot changelog
		/// </summary>
		[Command("changelog")]
		[Alias("changelogs")]
		[Summary("Sends a changelog via a DM")]
		[Remarks("changelog\nchangelog --OR-- changelog r-3.0")]
		public async Task Changelog([Remainder] string rem = null)
		{
			var changelogFile = rem?.RemoveWhitespace().ToLower() ?? Program.version;
			// Replace \r\n with \n to save some string length
			var path = Path.Combine(Program.AssemblyDirectory, "dist", "changelogs", $"{changelogFile}.txt");
			if (File.Exists(path))
			{
				var changelogTxt = File.ReadAllText(path).Replace("\r\n", "\n");
				if (!string.IsNullOrEmpty(changelogTxt))
				{
					var ch = await Context.Message.Author.CreateDMChannelAsync();
					foreach (string msg in changelogTxt.ChunksUpto(1999))
						await ch.SendMessageAsync(msg);
					await Context.Message.AddReactionAsync("👌");
					await ReplyAsync($"{Context.Message.Author.Username}, I sent you my changelog for {Program.version}! 📚");
					return;
				}
			}
			await ReplyAsync($"Could not find changelogs for ``{changelogFile}``");
		}

		/// <summary>
		/// Returns source repository
		/// </summary>
		[Command("src")]
		[Alias("source")]
		[Summary("Shows a link to the code of this bot")]
		[Remarks("src")]
		public async Task Src([Remainder] string rem = null) =>
			await ReplyAsync($"Here's how I am made! <https://github.com/Jofairden/TheGuide>");

		/// <summary>
		/// Responds with bot info
		/// </summary>
		[Command("info")]
		[Alias("about")]
		[Summary("Shows elaborate bot info")]
		[Remarks("info --OR-- about")]
		//[RequireContext(ContextType.DM)]
		public async Task Info([Remainder] string rem = null)
		{
			var application = await Context.Client.GetApplicationInfoAsync();
			var client = (Context.Client as DiscordSocketClient);
			await ReplyAsync($"{Format.Bold($"Info for {client?.CurrentUser.GenFullName()}")}\n" +
							 $"- Author: {Tools.GenFullName(application.Owner.Username, application.Owner.Discriminator)} ({application.Owner.Id})\n" +
							 $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
							 $"- API: {DiscordConfig.APIVersion}\n" +
							 $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
							 $"- Uptime: {Tools.GetUptime()} (dd\\hh\\mm\\ss)\n" +
							 $"- Bot version: {Program.version}\n\n" +

							 $"{Format.Bold("Stats")}\n" +
							 $"- Heap Size: {Tools.GetHeapSize()} MB\n" +
							 $"- Guilds: {client?.Guilds.Count}\n" +
							 $"- Channels: {client?.Guilds.Sum(g => g.Channels.Count)} " +
							 $"(of which text: {client?.Guilds.Sum(g => g.TextChannels.Count)}, " +
							 $"voice: {client?.Guilds.Sum(g => g.VoiceChannels.Count)})\n" +
							 $"- Roles: {client?.Guilds.Sum(g => g.Roles.Count)}\n" +
							 $"- Emojis: {client?.Guilds.Sum(g => g.Emojis.Count)}\n" +
							 $"- Users: {client?.Guilds.Sum(g => g.MemberCount)} (of which cached: {client?.Guilds.Sum(g => g.Users.Count)})"
			);
		}

		/// <summary>
		/// Lookup a user
		/// </summary>
		[Command("whois")]
		[Summary("Whois user lookup in guilds")]
		[Remarks("whois <username/nickname> --OR-- whois role:<rolename>\nwhois the guide --OR-- whois role:admin")]
		//[RequireContext(ContextType.DM)]
		public async Task Whois([Remainder][Summary("the user or predicate")]string username)
		{
			var client = (Context.Client as DiscordSocketClient);
			if (username.StartsWith("role:", StringComparison.CurrentCultureIgnoreCase))
			{
				var role = username.Substring(5);
				// Get all users in guilds with this role
				var users =
					client?.Guilds
						.Select(g =>
							g.Users.Where(u =>
								g.Roles.Any(r =>
									!r.IsEveryone
									&& r.Name.Contains(role, StringComparison.CurrentCultureIgnoreCase)
									&& u.Roles.Any(x => x.Id == r.Id))))
						.ToList();

				var predicate = $"``{role} in:roles from:user``";
				if (users.Any(g => g.Any()))
				{
					var sb = new StringBuilder();
					sb.AppendLine($"Users found matching {predicate} predicate\n" +
								$"Format: displayname\n");

					// Print user info
					for (int i = 0; i < users.Count; i++)
					{
						users[i].ToList().ForEach(u =>
							sb.AppendLine($"{u.Username}#{u.Discriminator} ({client?.Guilds.ElementAtOrDefault(i).Name})\n" +
										$"Roles: {(client?.Guilds.ElementAtOrDefault(i).Roles.Where(urole => !urole.IsEveryone && u.Roles.Any(x => x.Id == urole.Id))).PrintRoles()}\n"));
					}

					// Prints all roles together
					//var sb2 = new StringBuilder();
					//client?.Guilds.ToList().ForEach(g => g.Roles.ToList().Where(srole => users.Any(user => user.Any(p => p.RoleIds.Contains(srole.Id))) && !srole.IsEveryone).ToList().ForEach(r => sb2.Append($"{r.Name}, ")));
					//sb.AppendLine($"\n**All roles on users:**\n{sb2}".Truncate(2));
					var msgs = sb.ToString().ChunksUpto(2000);
					foreach (var msg in msgs)
						await ReplyAsync(msg);
				}
				else
					await ReplyAsync($"No users found matching {predicate} predicate.");
			}
			else
			{
				var users =
					client?.Guilds
						.Select(
							g =>
								g.Users.Where(
									u =>
										string.Equals(u.Username, username, StringComparison.CurrentCultureIgnoreCase) ||
										string.Equals(u.Nickname, username, StringComparison.CurrentCultureIgnoreCase)))
						.ToList();

				var predicate = $"``{username} in:name,nickname from:guilds``";

				if (users.Any(g => g.Any()))
				{
					var sb = new StringBuilder();
					sb.AppendLine($"Users found matching {predicate} predicate\n" +
								  $"Format: displayname (nickname) (guild)\n");

					for (int i = 0; i < users.Count; i++)
						foreach (var user in users[i])
							sb.AppendLine($"{(user.Nickname == null ? Format.Bold(user.Username) : user.Username)}#{user.Discriminator}" +
										  $"{(user.Nickname != null ? $" ({Format.Bold(user.Nickname)})" : "")} " +
										  $"({client?.Guilds.ElementAt(i).Name})");

					await ReplyAsync($"{sb}");
				}
				else
					await ReplyAsync($"No users found matching {predicate} predicate.");
			}
		}

		/// <summary>
		/// Generates a mod widget
		/// </summary>
		[Command("widget")]
		[Alias("widgetimg", "widgetimage")]
		[Summary("Generates a widget image of specified mod")]
		[Remarks("widget <mod>\nwidget examplemod")]
		public async Task Widget([Remainder]string mod)
		{
			mod = mod.RemoveWhitespace();
			var mods = ModSystem.mods.Where(m => string.Equals(m, mod, StringComparison.CurrentCultureIgnoreCase));

			if (!mods.Any())
			{
				await ShowSimilarMods(mod);
				return;
			}

			using (var client = new System.Net.Http.HttpClient())
			{
				using (var stream = await client.GetStreamAsync($"{ModSystem.widgetUrl}{mod}.png"))
				{
					var sendFileAsync = Context.Channel?.SendFileAsync(stream, $"{mod}.png", $"Widget for ``{mod}``");
					if (sendFileAsync != null)
						await sendFileAsync;
				}
			}
		}

		/// <summary>
		/// Search on github for repositories
		/// </summary>
		[Command("github")]
		[Alias("gh")]
		[Summary("Returns a search link for github searching for repositories matching your predicate")]
		[Remarks("github <search predicate>\ngithub tmodloader,mod in:name,description,topic")]
		public async Task Github([Remainder][Summary("the predicate")]string rem) =>
			await ReplyAsync($"Uri: https://github.com/search?q={Uri.EscapeDataString(rem)}&type=Repositories");

		/// <summary>
		/// Get an item ID
		/// </summary>
		[Command("itemid")]
		[Alias("item")]
		[Summary("Searches for a vanilla item by ID or name")]
		[Remarks("itemid <name> --OR-- itemid <id>\nitemid Wooden Sword --OR-- itemid 24")]
		public async Task Itemid([Remainder][Summary("the item name or id")] string name)
		{
			string replyText = $"{name} not found.";
			short parsed16Int;
			string usename = name.RemoveWhitespace().ToUpper();
			KeyValuePair<string, short> kvp;

			if (short.TryParse(usename, out parsed16Int) && Program.itemConsts.ContainsValue(parsed16Int))
			{
				kvp = Program.itemConsts.FirstOrDefault(x => x.Value == parsed16Int);
				replyText = $"{kvp.Key} found with ID: {kvp.Value}";
			}
			else if (Program.itemConsts.ContainsKey(usename))
			{
				kvp = Program.itemConsts.FirstOrDefault(x => string.Equals(x.Key, usename, StringComparison.CurrentCultureIgnoreCase));
				replyText = $"{kvp.Key} found with ID: {kvp.Value}";
			}
			await ReplyAsync(replyText);
		}

		/// <summary>
		/// Get a dust ID
		/// </summary>
		[Command("dustid")]
		[Alias("dust")]
		[Summary("Searches for a vanilla dust by ID or name")]
		[Remarks("dustid <name> --OR-- dustid <id>\ndustid Fire --OR-- dustid 6")]
		public async Task Dustid([Remainder][Summary("the dust name or id")] string name)
		{
			string replyText = $"{name} not found.";
			short parsed16Int;
			string usename = name.RemoveWhitespace().ToUpper();
			KeyValuePair<string, short> kvp;

			if (short.TryParse(usename, out parsed16Int) && Program.dustConsts.ContainsValue(parsed16Int))
			{
				kvp = Program.dustConsts.FirstOrDefault(x => x.Value == parsed16Int);
				replyText = $"{kvp.Key} found with ID: {kvp.Value}";
			}
			else if (Program.dustConsts.ContainsKey(usename))
			{
				kvp = Program.dustConsts.FirstOrDefault(x => string.Equals(x.Key, usename, StringComparison.CurrentCultureIgnoreCase));
				replyText = $"{kvp.Key} found with ID: {kvp.Value}";
			}

			await ReplyAsync(replyText);
		}

		/// <summary>
		/// Get a chain id
		/// </summary>
		[Command("chainid")]
		[Alias("chain")]
		[Summary("Searches for a vanilla chain by ID or name")]
		[Remarks("chainid <name> --OR-- chainid <id>\nchainid SilkRope --OR-- chainid 4")]
		public async Task Chainid([Remainder][Summary("the chain name or id")] string name)
		{
			string replyText = $"{name} not found.";
			short parsed16Int;
			string usename = name.RemoveWhitespace().ToUpper();
			KeyValuePair<string, short> kvp;

			if (short.TryParse(usename, out parsed16Int) && Program.chainConsts.ContainsValue(parsed16Int))
			{
				kvp = Program.chainConsts.FirstOrDefault(x => x.Value == parsed16Int);
				replyText = $"{kvp.Key} found with ID: {kvp.Value}";
			}
			else if (Program.chainConsts.ContainsKey(usename))
			{
				kvp = Program.chainConsts.FirstOrDefault(x => string.Equals(x.Key, usename, StringComparison.CurrentCultureIgnoreCase));
				replyText = $"{kvp.Key} found with ID: {kvp.Value}";
			}

			await ReplyAsync(replyText);
		}

		/// <summary>
		/// Get an ammo id
		/// </summary>
		[Command("ammoid")]
		[Alias("ammo")]
		[Summary("Searches for a vanilla ammo by ID or name")]
		[Remarks("ammoid <name> --OR-- ammoid <id>\nammoid Bullet --OR-- ammoid 97")]
		public async Task Ammoid([Remainder][Summary("the ammo name or id")] string name)
		{
			string replyText = $"{name} not found.";
			int parsed16Int;
			string usename = name.RemoveWhitespace();
			KeyValuePair<string, int> kvp;

			if (int.TryParse(usename, out parsed16Int) && Program.ammoConsts.ContainsValue(parsed16Int))
			{
				kvp = Program.ammoConsts.FirstOrDefault(x => x.Value == parsed16Int);
				replyText = $"{kvp.Key} found with ID: {kvp.Value}";
			}
			else if (Program.ammoConsts.ContainsKey(usename))
			{
				kvp = Program.ammoConsts.FirstOrDefault(x => string.Equals(x.Key, usename, StringComparison.CurrentCultureIgnoreCase));
				replyText = $"{kvp.Key} found with ID: {kvp.Value}";
			}

			await ReplyAsync(replyText);
		}

		/// <summary>
		/// Get a buff id
		/// </summary>
		[Command("buffid")]
		[Alias("buff")]
		[Summary("Searches for a vanilla buff by ID or name")]
		[Remarks("buffid <name> --OR-- buffid <id>\nbuffid IronSkin --OR-- ammoid 5")]
		public async Task Buffid([Remainder][Summary("the buff name or id")] string name)
		{
			string replyText = $"{name} not found.";
			int parsed16Int;
			string usename = name.RemoveWhitespace();
			KeyValuePair<string, int> kvp;

			if (int.TryParse(usename, out parsed16Int) && Program.buffConsts.ContainsValue(parsed16Int))
			{
				kvp = Program.buffConsts.FirstOrDefault(x => x.Value == parsed16Int);
				replyText = $"{kvp.Key} found with ID: {kvp.Value}";
			}
			else if (Program.buffConsts.ContainsKey(usename))
			{
				kvp = Program.buffConsts.FirstOrDefault(x => string.Equals(x.Key, usename, StringComparison.CurrentCultureIgnoreCase));
				replyText = $"{kvp.Key} found with ID: {kvp.Value}";
			}

			await ReplyAsync(replyText);
		}

		/// <summary>
		/// Get mod info
		/// </summary>
		[Command("mod")]
		[Alias("modinfo")]
		[Summary("Shows info about a mod")]
		[Remarks("mod <internal modname> --OR-- mod <part of name>\nmod examplemod")]
		public async Task Mod([Remainder][Summary("The mod name or part of it")] string mod)
		{
			mod = mod.RemoveWhitespace();
			var mods = ModSystem.mods.Where(m => string.Equals(m, mod, StringComparison.CurrentCultureIgnoreCase));

			if (!mods.Any())
			{
				await ShowSimilarMods(mod);
				return;
			}

			// Fixes not finding files
			mod = ModSystem.mods.FirstOrDefault(m => string.Equals(m, mod, StringComparison.CurrentCultureIgnoreCase));
			if (mod == null)
				return;

			// Some mod is found continue.
			var modjson = JObject.Parse(File.ReadAllText(ModSystem.modPath(mod)));
			var properties =
				modjson.Properties()
				.Select(p => $"**{p.Name.FirstCharToUpper().AddSpacesToSentence()}**: {p.Value}");
			await ReplyAsync(string.Join("\n", properties));
		}

		/// <summary>
		/// Get a modversion
		/// </summary>
		[Command("modversion")]
		[Alias("modver", "modv")]
		[Summary("Gets the version of a mod")]
		[Remarks("modversion <mod>\nmodversion Example Mod")]
		public async Task ModVersion([Remainder] string mod)
		{
			mod = mod.RemoveWhitespace();
			var mods = ModSystem.mods.Where(m => string.Equals(m, mod, StringComparison.CurrentCultureIgnoreCase));

			if (!mods.Any())
			{
				await ShowSimilarMods(mod);
				return;
			}

			// Some mod is found continue.
			var modjson = JObject.Parse(File.ReadAllText(ModSystem.modPath(mod)));
			await ReplyAsync($"**{modjson.Property("displayname").Value}**: {modjson.Property("version").Value}");
		}

		// Helepr method
		private async Task ShowSimilarMods(string mod)
		{
			var msg =
				$"Mod with that name doesn\'t exist" +
				$"\nDid you possibly mean any of these?\n\n";

			var modMsg = "No mods found..."; ;

			// Find similar mods
			var similarMods =
				ModSystem.mods
					.Where(m => m.Contains(mod, StringComparison.CurrentCultureIgnoreCase))
					.ToArray();

			if (similarMods.Any())
			{
				modMsg = similarMods.PrettyPrint();
				// Make sure message doesn't exceed discord's max msg length
				if (modMsg.Length > 2000)
				{
					modMsg = modMsg.Cap(2000 - msg.Length);
					// Make sure message doesn't end with a half cut modname
					var index = modMsg.LastIndexOf(',');
					var lastModClean = modMsg.Substring(index + 1).Replace("`", "").Trim();
					if (ModSystem.mods.All(m => m != lastModClean))
						modMsg = modMsg.Substring(0, index);
				}
			}

			await ReplyAsync($"{msg}{modMsg}");
		}

		// help v2.0
		// somewhat optimized, also checks for aliases now
		// by now, actually probably needs a v3.0 lol
		[Command("help")]
		[Alias("guide")]
		[Summary("Shows info about commands")]
		[Remarks("help [module] [command]\nhelp whois --OR-- help tag create")]
		public async Task Help([Remainder] string rem = null)
		{
			//Requests all commands
			var sender = Context.Message.Author as SocketGuildUser;
			var header = $"{Format.Bold($"Usable commands for {sender?.Username} in {Context.Guild.Name}")}";
			var commandlist = "No commands found.";
			var modules = "";

			string sentModule;
			string sentCommand;

			//Help is called with no arguments, get all commands including other modules
			if (rem == null)
			{
				commandlist = "";
				// Get 'default' or non-module commands
				//TODO: config: which module is/are 'default'?
				foreach (var command in service.Commands)
				{
					var alias = command.Module.Aliases.First();
					if (string.IsNullOrEmpty(alias) || string.Equals(alias, "default"))
					{
						var result = await command.CheckPreconditionsAsync(Context, map);
						if (result.IsSuccess)
							commandlist += command.Name + ", ";
					}
				}

				if (string.IsNullOrEmpty(commandlist))
					commandlist = $"No commands found matching predicate ``filter:commands in:module:default condition:usable``";
				else if (commandlist.TrimEnd().EndsWith(","))
					commandlist = commandlist.Truncate(2);

				// Get commands in modules
				foreach (var module in service.Modules)
				{
					var alias = module.Aliases.First();
					// TODO: config: which modules are included?
					if (!string.IsNullOrEmpty(alias) && !string.Equals(alias, "default") && module.Commands.Any())
					{
						modules += $"\n``{module.Name}``: ";
						foreach (var command in module.Commands)
						{
							if (string.Equals(command.Name, "no-help", StringComparison.CurrentCultureIgnoreCase))
								continue;
							var result = await command.CheckPreconditionsAsync(Context, map);
							if (result.IsSuccess)
								modules += $"{command.Name}, ";
						}
						modules = modules.Truncate(2);
					}
				}

				if (string.IsNullOrEmpty(modules))
					modules = "No results matching predicate ``find:modules in:commandservice``";

				await ReplyAsync($"{header}\n" +
								 $"{commandlist}\n" +
								 $"\n**Modules**" +
								 $"{modules}\n\n" +
								 $"Get command specific help: ``{CommandHandler.prefixChar}help [module] <name>``");
			}
			else
			{
				// help is called with arguments
				CommandInfo currentCommand;
				if (rem.Any(char.IsWhiteSpace) && !rem.StartsWith("module:") && !string.Equals(rem, "sub", StringComparison.CurrentCultureIgnoreCase))
				{
					// x command from module y
					//TODO: config: setting for name to hardmatch, or also look for aliases?
					var args = rem.Split(' ');
					sentModule = args[0];
					sentCommand = args[1];
					currentCommand = service.Modules.FirstOrDefault(m =>
								string.Equals(m.Aliases.First(), sentModule, StringComparison.CurrentCultureIgnoreCase))?
						.Commands.FirstOrDefault(
							c => c.Aliases.Any(a => string.Equals(a, $"{sentModule} {sentCommand}", StringComparison.CurrentCultureIgnoreCase)));
				}
				else
				{
					// x command from module default
					sentModule = null;
					sentCommand = rem;
					currentCommand = service.Commands.FirstOrDefault(c =>
							c.Aliases.Any(a => string.Equals(a, sentCommand, StringComparison.CurrentCultureIgnoreCase)));

					// check for modules
					if (currentCommand == null)
					{
						var checkCommand = sentCommand.StartsWith("module:") ? rem.Substring(7) : rem;
						var currentModule = service.Modules.FirstOrDefault(m =>
								m.Aliases.Any(a => string.Equals(a, checkCommand, StringComparison.CurrentCultureIgnoreCase)));

						// A module was found
						if (currentModule != null)
						{
							header += $" **for module {currentModule.Aliases.First()}**";
							commandlist = "";
							foreach (var command in currentModule.Commands)
							{
								if (string.Equals(command.Name, "no-help", StringComparison.CurrentCultureIgnoreCase))
									continue;
								var result = await command.CheckPreconditionsAsync(Context, map);
								if (result.IsSuccess)
									commandlist += command.Name + ", ";
							}
							if (string.IsNullOrEmpty(commandlist))
								commandlist = $"No commands found matching predicate ``filter:commands in:module:{sentCommand} condition:usable``";
							else if (commandlist.TrimEnd().EndsWith(","))
								commandlist = commandlist.Truncate(2);

							await ReplyAsync($"{header}\n" +
											$"{commandlist}\n\n" +
											$"Get command specific help: ``{CommandHandler.prefixChar}help [module] <name>``");
							return;
						}
						else // Also no module found.
						{
							await ReplyAsync($"No results matching predicate ``{sentCommand} in:commands,modules``");
							return;
						}
					}
				}

				var checkPreconditionsAsync = currentCommand?.CheckPreconditionsAsync(Context, map);
				if (checkPreconditionsAsync != null)
				{
					var result = await checkPreconditionsAsync;
					var summ = currentCommand.Summary == null || currentCommand.Summary.Length <= 0
						? "No summary"
						: currentCommand.Summary;
					var remarkSplit = currentCommand.Remarks.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
					var remarks = currentCommand.Remarks == null || currentCommand.Remarks.Length <= 0
						? "No remarks"
						: remarkSplit.Length > 1
							? $"``{CommandHandler.prefixChar}{remarkSplit[0]}``" +
							$"\n" +
							$"**Example:** ``{CommandHandler.prefixChar}{remarkSplit[1]}``"
							: $"``{CommandHandler.prefixChar}{currentCommand.Remarks}``";
					var moduleAlias = currentCommand.Module.Aliases.First();
					var commandAlias =
						$"``{currentCommand.Aliases.First(a => string.Equals($"{(sentModule == null ? sentCommand : $"{sentModule} {sentCommand}")}", a, StringComparison.CurrentCultureIgnoreCase))}``";
					var headerCommand =
						$"command:{sentCommand}";
					var headerText = sentModule == null
						? $"_{headerCommand}_"
						: $"_module:{moduleAlias} {headerCommand}_";
					// If more aliases present, append them to the commandAlias string
					if (currentCommand.Aliases.Count > 1)
						commandAlias += $"\n**Aliases**: " +
										string.Join(", ",
											currentCommand.Aliases.Where(
													a => !string.Equals($"{(sentModule == null ? sentCommand : $"{sentModule} {sentCommand}")}", a, StringComparison.CurrentCultureIgnoreCase))
												.Select(a => $"``{a}``"));
					await
						ReplyAsync(
							$"**Command info for** {headerText}" +
							$"\n" +
							$"{(sentModule == null ? "" : $"**Module **: ``{moduleAlias}``\n")}" +
							$"**Command**: {commandAlias}" +
							$"\n" +
							$"**Summary**: ``{summ}``" +
							$"\n" +
							$"**Usage**: {remarks}" +
							$"\n" +
							$"**Usable by {sender?.Username}**: ``{(result.IsSuccess ? "yes" : "no")}``");
				}
				else
					await ReplyAsync($"No results matching predicate ``{(sentModule != null ? $"{sentCommand} in:module:{sentModule}" : $"find:{sentCommand} in:commands")}``");
			}
		}
	}
}
