using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.EmojiTools;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using TheGuide.Preconditions;
using TheGuide.Systems;
using TheGuide.Systems.Snowflake;

namespace TheGuide.Modules
{
	//todo: config: which module is 'default'?
	[Name("default")]
	public class Commands : GuideModuleBase<SocketCommandContext>
	{
		private readonly CommandService _service;
		private readonly IDependencyMap _map;

		public Commands(CommandService service, IDependencyMap map)
		{
			_service = service;
			_map = map;
		}

		/// <summary>
		/// Generates snowflake IDs
		/// </summary>
		[Command("snowflake"), Ratelimit(5, 2, Measure.Minutes)]
		[Summary("Will generate up to 10 snowflake ids and guids")]
		[Remarks("snowflake [amount]\nsnowflake 10")]
		[ConfigAdmin]
		public async Task SnowFlake([Remainder] int rem = 1)
		{
			rem = Math.Max(0, Math.Min(10, rem));

			var sb = new StringBuilder();
			var gen = new Id64Generator();
			var genGuid = new IdGuidGenerator();
			var ids = gen.Take(rem).ToArray();
			var guids = genGuid.Take(rem).ToArray();

			sb.Append($"Generated snowflake ids:\n{string.Join("\n", ids)}\n\n");
			sb.Append($"Generated snowflake guids:\n{string.Join("\n", guids)}");

			await ReplyAsync($"{sb}".Cap(2000));
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
		[Command("ping"), Ratelimit(5, 1, Measure.Minutes)]
		[Alias("status")]
		[Summary("Returns the bot response time")]
		[Remarks("ping")]
		public async Task Ping([Remainder] string rem = null)
		{
			var sw = Stopwatch.StartNew();
			var d = 60d / (Context.Client as DiscordSocketClient)?.Latency * 1000;
			if (d != null)
			{
				var l = (Context.Client as DiscordSocketClient).Latency;
				var msg = await ReplyAsync(
					$"My heartrate is ``{(int)d}`` bpm ({l} ms)");
				await msg.ModifyAsync(p => p.Content =
					$"{msg.Content}" +
					$"\nMessage response time is {sw.ElapsedMilliseconds} ms" +
					$"\n[Delta: {Math.Abs(sw.ElapsedMilliseconds - l)} ms]");
				sw.Stop();
			}

		}


		/// <summary>
		/// Returns bot changelog
		/// </summary>
		[Command("changelog")]
		[Alias("changelogs")]
		[Summary("Sends a changelog via a DM")]
		[Remarks("changelog [value]\nchangelog --OR-- changelog list --OR-- changelog r-3.0")]
		public async Task Changelog([Remainder] string rem = null)
		{
			var path = Path.Combine(AppContext.BaseDirectory, "dist", "changelogs");
			var files =
				Directory.GetFiles(path)
					.Select(Path.GetFileNameWithoutExtension)
					.ToArray();

			if (rem != null && rem.Trim().ICEquals("list"))
			{
				await ReplyAsync(files.Any()
					? $"Found changelogs:\n" +
					files.PrettyPrint() +
					$"\n\nUse ``{CommandHandler.prefixChar}changelog <value>`` to get changelogs.\nExample: ``{CommandHandler.prefixChar}changelog {Program.version}``"
					: $"No changelogs found.");
				return;
			}

			var changelogFile = rem?.RemoveWhitespace().ToLower() ?? Program.version;
			// Replace \r\n with \n to save some string length
			path = Path.Combine(AppContext.BaseDirectory, "dist", "changelogs", $"{changelogFile}.txt");
			if (File.Exists(path))
			{
				var changelogTxt = File.ReadAllText(path).Replace("\r\n", "\n");
				if (!string.IsNullOrEmpty(changelogTxt))
				{
					var ch = await Context.Message.Author.CreateDMChannelAsync();
					changelogTxt.ChunksUpto(2000).ToList().ForEach(async x => await ch.SendMessageAsync(x));
					await ch.SendMessageAsync($"Other changelogs:\n{files.Where(x => !x.ICEquals(changelogFile)).PrettyPrint()}");
					await Context.Message.AddReactionAsync(UnicodeEmoji.FromText(":ok_hand:")); // adds a reaction emoji
					await ReplyAsync($"{Context.Message.Author.Username}, I sent you my changelog for {Program.version}! {UnicodeEmoji.FromText(":books:")}"); // emoji in message
					return;
				}
			}
			await ReplyAsync($"Could not find changelogs for `{changelogFile.Cap(10)}`");
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
		[Command("info"), Ratelimit(2, 1, Measure.Minutes)]
		[Alias("about")]
		[Summary("Shows elaborate bot info")]
		[Remarks("info --OR-- about\ninfo expand")]
		//[RequireContext(ContextType.DM)]
		public async Task Info([Remainder] string rem = "")
		{
			var application = await Context.Client.GetApplicationInfoAsync();
			var client = Context.Client as DiscordSocketClient;

			var msg = $"**Info**\n" +
						$"- Author: {Tools.GenFullName(application.Owner.Username, application.Owner.Discriminator)}" +
						$" [ID: {application.Owner.Id}]\n" +
						$"- Library: Discord.Net ({DiscordConfig.Version})" +
						$" [API: {DiscordConfig.APIVersion}]\n" +
						$"- Runtime: {AppContext.TargetFrameworkName} {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture} \n" +
						$"- Uptime: {Tools.GetUptime()} (dd\\.hh\\:mm\\:ss)\n" +
						$"- Bot version: {Program.version}";

			if (rem.Trim().ICEquals("expand"))
			{
				msg += $"\n\n**Stats**\n" +
						$"- Heap Size: {Tools.GetHeapSize()} MB\n" +
						$"- Guilds: {client?.Guilds.Count}\n" +
						$"- Channels: {client?.Guilds.Sum(g => g.Channels.Count)}" +
						$" [Text: {client?.Guilds.Sum(g => g.TextChannels.Count)}]" +
						$" [Voice: {client?.Guilds.Sum(g => g.VoiceChannels.Count)}]\n" +
						$"- Roles: {client?.Guilds.Sum(g => g.Roles.Count)}\n" +
						$"- Emojis: {client?.Guilds.Sum(g => g.Emojis.Count)}\n" +
						$"- Users: {client?.Guilds.Sum(g => g.MemberCount)}" +
						$" [Cached: {client?.Guilds.Sum(g => g.Users.Count)}]";
			}

			await ReplyAsync(msg.Cap(2000));
		}

		/// <summary>
		/// Lookup a user
		/// </summary>
		[Command("whois"), Ratelimit(5, 1, Measure.Minutes)]
		[Summary("Whois user lookup in guilds")]
		[Remarks("whois <username/nickname> --OR-- whois role:<rolename>\nwhois the guide --OR-- whois role:admin")]
		//[RequireContext(ContextType.DM)]
		public async Task Whois([Remainder][Summary("the user or predicate")]string username)
		{
			var client = (Context.Client as DiscordSocketClient);
			var sb = new StringBuilder();
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
				}
				else
					sb.Append($"No users found matching predicate {predicate}");
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
					sb.AppendLine($"Users found matching predicate {predicate}\n" +
								$"Format: displayname (nickname) (guild)\n");

					for (int i = 0; i < users.Count; i++)
						foreach (var user in users[i])
							sb.AppendLine($"{(user.Nickname == null ? Format.Bold(user.Username) : user.Username)}#{user.Discriminator}" +
										$"{(user.Nickname != null ? $" ({Format.Bold(user.Nickname)})" : "")} " +
										$"({client?.Guilds.ElementAt(i).Name})");
				}
				else
					sb.Append($"No users found matching predicate {predicate}");


			}

			string notify = sb.ToString();
			if (notify.Length > 2000)
			{
				sb.Clear();
				sb.Append(notify.Cap(2000));
				var len = sb.Length;
				sb.Remove(len - 2, 2).Insert(len - 2, "``");
				notify = sb.ToString();
			}
			await ReplyAsync(notify);
		}

		/// <summary>
		/// Generates a mod widget
		/// </summary>
		[Command("widget"), Ratelimit(2, 1, Measure.Minutes)]
		[Alias("widgetimg", "widgetimage")]
		[Summary("Generates a widget image of specified mod")]
		[Remarks("widget <mod>\nwidget examplemod")]
		public async Task Widget([Remainder]string mod)
		{
			mod = mod.RemoveWhitespace();
			var result = await ShowSimilarMods(mod);

			if (result)
				using (var client = new System.Net.Http.HttpClient())
				using (var stream = await client.GetStreamAsync($"{ModSystem.widgetUrl}{mod}.png"))
				{
					var sendFileAsync = Context.Channel?.SendFileAsync(stream, $"{mod}.png", $"Widget for ``{mod}``");
					if (sendFileAsync != null)
						await sendFileAsync;
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
			}
			else if (Program.itemConsts.ContainsKey(usename))
			{
				kvp = Program.itemConsts.FirstOrDefault(x => string.Equals(x.Key, usename, StringComparison.CurrentCultureIgnoreCase));
			}
			replyText = $"{kvp.Key} found with ID: {kvp.Value}";
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
			}
			else if (Program.dustConsts.ContainsKey(usename))
			{
				kvp = Program.dustConsts.FirstOrDefault(x => string.Equals(x.Key, usename, StringComparison.CurrentCultureIgnoreCase));
			}
			replyText = $"{kvp.Key} found with ID: {kvp.Value}";

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
			}
			else if (Program.chainConsts.ContainsKey(usename))
			{
				kvp = Program.chainConsts.FirstOrDefault(x => string.Equals(x.Key, usename, StringComparison.CurrentCultureIgnoreCase));
			}
			replyText = $"{kvp.Key} found with ID: {kvp.Value}";

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
			}
			else if (Program.ammoConsts.ContainsKey(usename))
			{
				kvp = Program.ammoConsts.FirstOrDefault(x => string.Equals(x.Key, usename, StringComparison.CurrentCultureIgnoreCase));
			}
			replyText = $"{kvp.Key} found with ID: {kvp.Value}";
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
			}
			else if (Program.buffConsts.ContainsKey(usename))
			{
				kvp = Program.buffConsts.FirstOrDefault(x => string.Equals(x.Key, usename, StringComparison.CurrentCultureIgnoreCase));
			}
			replyText = $"{kvp.Key} found with ID: {kvp.Value}";

			await ReplyAsync(replyText);
		}

		/// <summary>
		/// Shows hot mods
		/// </summary>
		[Command("hot"), Ratelimit(2, 1, Measure.Minutes)]
		[Summary("Shows top 10 hottest mods")]
		[Remarks("hot")]
		public async Task Hot([Remainder] string rem = null)
		{
			using (var client = new System.Net.Http.HttpClient())
			{
				var response = await client.GetAsync(ModSystem.hotUrl);
				var postResponse = await response.Content.ReadAsStringAsync();
				var json = JArray.Parse(postResponse);
				var values = json.Children<JObject>()
					.Select(x =>
						$"**{x.Property("name").Value}**: {int.Parse(x.Property("dls").Value.ToString()):n0}").ToList();
				values.Insert(0, "``Showing top 10 hottest mods (mod: hotness)``");
				await ReplyAsync(string.Join("\n", values));

			}
		}

		/// <summary>
		/// Get mod info
		/// </summary>
		[Command("mod")]
		[Alias("modinfo")]
		[Summary("Shows info about a mod")]
		[Remarks("mod <internal modname> --OR-- mod <part of name>\nmod examplemod")]
		public async Task Mod([Remainder] string mod)
		{
			mod = mod.RemoveWhitespace();
			var result = await ShowSimilarMods(mod);

			if (result)
			{
				// Fixes not finding files
				mod = ModSystem.mods.FirstOrDefault(m => string.Equals(m, mod, StringComparison.CurrentCultureIgnoreCase));
				if (mod == null)
					return;

				// Some mod is found continue.
				var modjson = JObject.Parse(File.ReadAllText(ModSystem.modPath(mod)));
				var properties = new List<string>();
				var culture = new CultureInfo("en-US");
				foreach (var property in modjson.Properties())
				{
					if (string.IsNullOrEmpty(property.Value.ToString())) continue;
					var name = property.Name;
					string value =
							string.Equals(name, "downloads", StringComparison.CurrentCultureIgnoreCase)
							? $"{property.Value:n0}"
							: string.Equals(name, "updateTimeStamp", StringComparison.CurrentCultureIgnoreCase)
							? DateTime.Parse($"{property.Value}").ToString("dddd, MMMMM d, yyyy h:mm:ss tt", culture)
							: $"{property.Value}";
					properties.Add($"**{name.FirstCharToUpper()}**: {value}");
				}
				properties.Add($"**Widget:** <{ModSystem.widgetUrl}{mod}.png>");
				using (var client = new System.Net.Http.HttpClient())
				{
					var values = new Dictionary<string, string>
					{
						{"modname", mod}
					};
					var content = new System.Net.Http.FormUrlEncodedContent(values);
					var response = await client.PostAsync(ModSystem.homepageUrl, content);
					var postResponse = await response.Content.ReadAsStringAsync();
					if (!string.IsNullOrEmpty(postResponse))
					{
						var json = JObject.Parse(postResponse);
						properties.Add($"**Homepage:** <{json.Property("homepage").Value}>");
					}
				}

				await ReplyAsync(string.Join("\n", properties).Cap(2000));
			}
		}

		[Command("popular"), Ratelimit(2, 1, Measure.Minutes)]
		[Alias("popularmods")]
		[Summary("Shows top 10 popular mods")]
		[Remarks("popular")]
		public async Task Popular([Remainder] string rem = null)
		{
			using (var client = new System.Net.Http.HttpClient())
			{
				var response = await client.GetAsync(ModSystem.popularUrl);
				var postResponse = await response.Content.ReadAsStringAsync();
				var entries =
					postResponse
						.Split(new string[] { "<br>" }, StringSplitOptions.None)
						.Where((x, i) => i < 10)
						.ToDictionary(
							x => new string(x.Where(char.IsLetter).ToArray()), y => new string(y.Where(char.IsDigit).ToArray()));

				await ReplyAsync($"``Showing top 10 popular mods (mod: downloads)``\n" +
								 string.Join("\n", entries.Select(x => $"**{x.Key}**: {int.Parse(x.Value):n0}")));
			}
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
			var result = await ShowSimilarMods(mod);

			if (result)
			{
				// Some mod is found continue.
				var modjson = JObject.Parse(File.ReadAllText(ModSystem.modPath(mod)));
				await ReplyAsync($"**{modjson.Property("displayname").Value}**: {modjson.Property("version").Value}".Cap(2000));
			}
		}

		// Helper method
		private async Task<bool> ShowSimilarMods(string mod)
		{
			var mods = ModSystem.mods.Where(m => string.Equals(m, mod, StringComparison.CurrentCultureIgnoreCase));

			if (mods.Any()) return true;

			var msg =
				$"Mod with that name doesn\'t exist" +
				$"\nDid you possibly mean any of these?\n\n";

			var modMsg = "No mods found..."; ;

			// Find similar mods

			var similarMods =
				ModSystem.mods
					.Where(m => m.Contains(mod, StringComparison.CurrentCultureIgnoreCase)
								&& m.LevenshteinDistance(mod) <= m.Length - 2) // prevents insane amount of mods found
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
			return false;
		}

		// help v3.0
		[Command("help")]
		[Alias("guide")]
		[Summary("Shows info about commands")]
		[Remarks("help [module] [command]\nhelp whois --OR-- help tag get")]
		public async Task Help()
		{
			//Requests all commands
			var sender = Context.Message.Author as SocketGuildUser;
			var headerTxt = $"**Usable commands for {sender?.Username} in {Context.Guild.Name}**";
			var commandTxt = "";
			var modulesTxt = "";

			var defModule =
				_service.Modules.FirstOrDefault(x =>
					string.Equals(x.Name, "default", StringComparison.CurrentCultureIgnoreCase));

			if (defModule != null)
			{
				foreach (var command in defModule.Commands.Where(x => !x.Name.ICEquals("no-help")))
				{
					var result =
						await command.CheckPreconditionsAsync(Context, _map);
					if (result.IsSuccess)
						commandTxt += $"{command.Name}, ";

				}
			}

			if (string.IsNullOrEmpty(commandTxt))
				commandTxt =
					$"No default commands found.";

			else if (commandTxt.TrimEnd().EndsWith(","))
				commandTxt =
					commandTxt.Truncate(2);

			var modules = _service.Modules.Where(x =>
				!x.Name.ICEquals("default")).ToArray();

			if (modules.Any())
			{
				foreach (var module in modules)
				{
					var modAlias = module.Aliases.First();
					if (!string.IsNullOrEmpty(modAlias)
						&& !module.Name.ICEquals("no-help")
						&& module.Commands.Any()
						)
					{
						string tmptxt = $"\n``{module.Name}``: ";
						foreach (var command in module.Commands.Where(x => !x.Name.ICEquals("no-help")))
						{
							var result =
								await command.CheckPreconditionsAsync(Context, _map);
							if (result.IsSuccess)
								tmptxt += $"{command.Name}, ";
						}
						if (!string.IsNullOrEmpty(tmptxt.Split(':')[1].Trim()))
							modulesTxt += tmptxt;
					}

					if (modulesTxt.TrimEnd().EndsWith(","))
						modulesTxt =
							modulesTxt.Truncate(2);
				}
			}

			if (string.IsNullOrEmpty(commandTxt + modulesTxt))
			{
				await ReplyAsync($"No commands found.");
				return;
			}

			await ReplyAsync($"{headerTxt}\n" +
							$"{commandTxt}\n" +
							$"\n" +
							$"**Modules**" +
							$"{modulesTxt}\n\n" +
							$"Get command specific help: `{CommandHandler.prefixChar}help <module> <command>`");
		}

		[Name("no-help")]
		[Command("help")]
		[Alias("guide")]
		public async Task Help(string predicate)
		{
			predicate = predicate.RemoveWhitespace().ToLower();
			var sender = Context.Message.Author as SocketGuildUser ?? Context.Message.Author;
			var headerTxt = $"**Usable commands for {sender.Username}**";
			var modPrefix = "module:";
			var startsWithModPrefix = predicate.StartsWith(modPrefix);
			var isMod = _service.Modules.Any(x => x.Name.ICEquals(predicate));
			var isCommand = _service.Commands.Any(x => x.Name.ICEquals(predicate));
			if (startsWithModPrefix || (isMod && !isCommand))
			{
				if (startsWithModPrefix)
					predicate = predicate.Substring(modPrefix.Length);
				headerTxt += $" **in module {predicate}**";
				var module = _service.Modules.FirstOrDefault(x =>
					x.Aliases.First().ICEquals(predicate));

				if (module == null)
				{
					await ReplyAsync($"Module `{predicate}` not found".Cap(2000));
					return;
				}

				var commands =
					module.Commands
						.Where(x =>
							!x.Name.ICEquals("no-help"));

				var usableCommands = new List<CommandInfo>();
				var commandInfos = commands as CommandInfo[] ?? commands.ToArray();
				commandInfos.ToList().ForEach(async x =>
				{
					var b = await x.CheckPreconditionsAsync(Context, _map);
					if (b.IsSuccess)
						usableCommands.Add(x);
				});

				await ReplyAsync(usableCommands.Any() ? 
							($"{headerTxt}\n" +
							usableCommands.Select(x => $"{x.Name}").PrettyPrint()).Cap(2000)
							: $"No usable commands found.");
				
			}
			else
			{
				modPrefix = "command:";
				if (predicate.StartsWith(modPrefix))
					predicate = predicate.Substring(modPrefix.Length);

				var command =
					_service.Commands
						.FirstOrDefault(x =>
							x.Aliases.Contains(predicate)
							&& !x.Name.ICEquals("no-help"));

				if (command == null)
				{
					await ReplyAsync($"Command `{predicate}` not found".Cap(2000));
					return;
				}

				var checkPreconditionsAsync =
					command.CheckPreconditionsAsync(Context, _map);

				if (checkPreconditionsAsync != null)
				{
					var result =
						await checkPreconditionsAsync;

					var aliases =
						command.Aliases
							.Skip(1)
							.ToArray();

					var split = command.Remarks.Split('\n') ?? new string[0];

					var usage =
						split.Length > 1
							? $"`{split[0]}`\n**Example**: `{split[1]}`"
							: $"`{command.Remarks}`";

					await
						ReplyAsync(
							$"**Module **: `{command.Module.Name}`\n" +
							$"**Command**: `{command.Aliases.First()}`" +
							$"{(aliases.Any() ? $"\n**Aliases**: {aliases.PrettyPrint()}" : "")}\n" +
							$"{(command.Summary?.Length > 0 ? $"**Summary**: ``{command.Summary}`\n" : "")}" +
							$"{(command.Remarks?.Length > 0 ? $"**Usage**: {usage}\n" : "")}" +
							$"**Usable by {sender?.Username}**: `{(result.IsSuccess ? "yes" : "no")}`");
				}
				else
					await ReplyAsync($"No command found");
			}

		}

		[Name("no-help")]
		[Command("help")]
		[Alias("guide")]
		public async Task Help(string module, [Remainder] string predicate)
		{
			var sender = Context.Message.Author as SocketGuildUser;
			const string modPrefix = "module:";
			const string cmdPrefix = "command:";

			module = module.ToLower();
			predicate = predicate.RemoveWhitespace().ToLower();

			if (module.StartsWith(cmdPrefix))
			{
				var tmp = module;
				module = predicate;
				predicate = tmp;
			}
			else if (predicate.StartsWith(modPrefix))
			{
				var tmp = predicate;
				predicate = module;
				module = tmp;
			}

			var split = module.Split(':');
			if (split.Length > 1)
				module = split[1];
			split = predicate.Split(':');
			if (split.Length > 1)
				predicate = split[1];

			var command =
				_service.Modules
					.FirstOrDefault(x =>
						x.Name.ICEquals(module))
					?.Commands
					.FirstOrDefault(x =>
						x.Aliases.Contains($"{module} {predicate}")
						&& !x.Name.ICEquals("no-help"));

			if (command != null)
			{
				var result =
					await command.CheckPreconditionsAsync(Context, _map);

				var aliases =
					command.Aliases
						.Skip(1)
						.ToArray();

				split =
					command.Remarks?.Split('\n') ?? new string[0];

				var usage =
					split.Length > 1
						? $"`{split[0]}`\n**Example**: `{split[1]}`"
						: $"`{command.Remarks}`";

				await
					ReplyAsync(
						$"**Module **: `{command.Module.Name}`\n" +
						$"**Command**: `{command.Name}`" +
						$"{(aliases.Any() ? $"\n**Aliases**: {aliases.PrettyPrint()}" : "")}\n" +
						$"{(command.Summary?.Length > 0 ? $"**Summary**: `{command.Summary}`\n" : "")}" +
						$"{(command.Remarks?.Length > 0 ? $"**Usage**: {usage}\n" : "")}" +
						$"**Usable by {sender?.Username}**: `{(result.IsSuccess ? "yes" : "no")}`");
			}
			else
				await ReplyAsync($"Command `{predicate}` in module `{module}` not found");
		}
	}
}
