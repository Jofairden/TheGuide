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
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Terraria.ID;
using TheGuide;

namespace TheGuide.Modules
{
	public class Commands : ModuleBase
	{
		private CommandService service;
		private IDependencyMap map;

		public Commands(CommandService _service, IDependencyMap _map)
		{
			service = _service;
			map = _map;
		}

		[Command("version")]
		[Summary("returns the bot version")]
		[Remarks("version")]
		public async Task Version([Remainder] string rem = null)
		{
			await ReplyAsync(
				$"I am running on ``{Program.version}``\n" +
				"Use ``?info`` for more elaborate information.");
		}

		[Command("ping")]
		[Alias("status")]
		[Summary("Returns the bot response time")]
		[Remarks("ping")]
		public async Task Ping([Remainder] string rem = null)
		{
			var latency = (Context.Client as DiscordSocketClient)?.Latency;
			if (latency != null)
				await ReplyAsync(
					$"My heartrate is ``{(int) (60d/latency*1000)}`` bpm ({latency} ms)");
		}

		//[Command("helpdev")]
		//[Alias("hd")]
		//[Summary("Quickly show a message which contains info about asking questions to developers")]
		//[Remarks("helpdev")]
		//public async Task Helpdev([Remainder] string rem = null)
		//{
		//	var role = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToUpper() == "DEVELOPERS");
		//	string roleMention = role != null && role.IsMentionable ? role.Mention : "@DEVELOPERS";
		//	await ReplyAsync(
		//		$"**Regarding asking questions to Terraria developers**\n" +
		//		$"Asking {roleMention} about vanilla stuff (as in: questions/suggestions/asking about future stuff etc.): They **WILL NOT** respond, and please **DO NOT** even try starting a discussion. Thank you.");
		//}

		//[Command("helpcode")]
		//[Alias("hc")]
		//[Summary("Quickly show a message which contains info for the help chat")]
		//[Remarks("helpcode")]
		//public async Task Helpcode([Remainder] string rem = null)
		//{
		//	var channel = (Context.Guild as SocketGuild)?.Channels.FirstOrDefault(x => x.Name.ToUpper() == "HELP");
		//	string channelMention = channel != null ? (channel as SocketTextChannel)?.Mention : "#help";
		//	await ReplyAsync(
		//		$"**When you require assistance**\n" +
		//		$"Please go to {channelMention}, provide error logs (especially a **strack trace**) _along with your code_ posted on hastebin. Thank you.");
		//}

		[Command("changelog")]
		[Summary("Sends a changelog via DM")]
		[Remarks("changelog")]
		public async Task Changelog([Remainder] string rem = null)
		{
			var ch = await Context.Message.Author.CreateDMChannelAsync();
			await ch.SendMessageAsync($"{Format.Bold($"Update logs for {Program.version} (from r1.*)")}\n\n" +
			                          $"```New commands```\n" +
			                          $"whois\n" +
			                          $"widget\n" +
			                          $"github\n" +
			                          $"itemid, dustid, chainid, ammoid, buffid\n" +
			                          $"mod\n" +
			                          $"tag alter\n" +
			                          $"\n" +
			                          $"{Format.Bold("whois command")}\n" +
			                          $"Look up any user I can find in the guilds I'm connected to!\n" +
			                          $"\n" +
			                          $"{Format.Bold("widget command")}\n" +
			                          $"Generate any mod widget!\n" +
			                          $"\n" +
			                          $"{Format.Bold("github command")}\n" +
			                          $"Search on github for repositories with any predicate!\n" +
			                          $"\n" +
			                          $"{Format.Bold("*id commands")}\n" +
			                          $"Look up any constant ID (name OR value)\n" +
			                          $"\n" +
			                          $"{Format.Bold("mod command")}\n" +
			                          $"Get a lot of mod info!\n" +
			                          $"\n" +
			                          $"{Format.Bold("tag alter")}\n" +
			                          $"Only available for mods. Can now change existing tags\n" +
			                          $"\n" +
			                          $"{Format.Bold(Format.Underline("Tags are now found automatically!"))}\n" +
			                          $"Any tags which do not share a name with any existing command will be found automatically!\n" +
			                          $"This means for example ``{CommandHandler.prefixChar}tag get SomeTagName`` can also be done as ``{CommandHandler.prefixChar}SomeTagName``\n" +
			                          $"Try it out!\n" +
			                          $"\n" +
			                          $"{Format.Bold(Format.Underline("Tags can now run other commands!"))}\n" +
			                          $"You can now program tags in such a way that, they will run like another command!\n" +
			                          $"For example: ``{CommandHandler.prefixChar}tag create MyMod command:widget MyMod``" +
			                          $" will act as if you called ``{CommandHandler.prefixChar}widget MyMod`` when you call this tag!"
									  );

			await ch.SendMessageAsync($"```Overhauled code```" +
			                          $"\n" +
			                          $"I was rewritten nearly entirely from scratch!\n" +
			                          $"\n" +
			                          $"``New version``: {Program.version}\n" +
			                          $"Expanded the {CommandHandler.prefixChar}info command\n" +
			                          $"The {CommandHandler.prefixChar}status command now returns my heartrate\n" +
			                          $"Changed github->src (the old github command is now src or source)\n" +
			                          $"helpdev and helpcode are no longer internal commands, instead they are now tags. You can call them with hd or hc (aliases)\n" +
			                          $"-v no longer works to call the status command\n" +
			                          $"The links command is no longer internal, this also became a tag. You can call it with links." +
			                          $"\n" +
			                          $"```Help command expand```\n" +
			                          $"The help command is now more useful and also shows usage and examples of commands\n" +
			                          $"\n" +
			                          $"{Format.Underline($"Remember to use ?help <command> to checkout the new help command expansion!\n" + $"For example: ``?help github``")}\n" +
			                          $"\n" +
			                          $"```Tags safeguard```\n" +
			                          $"Tags calling themselves {Format.Bold(Format.Underline("should not"))} function, please contact my owner if they do.\n" +
			                          $"**They should** simply return the contents");

			await ReplyAsync($"{Context.Message.Author.Mention}, I sent you my changelog!");
		}

		[Command("src")]
		[Alias("source")]
		[Summary("Shows a link to the code of this bot")]
		[Remarks("src")]
		public async Task Src([Remainder] string rem = null)
		{
			await ReplyAsync(
				$"Here's how I am made! <https://github.com/gorateron/theguide-discord>");
		}

		[Command("info")]
		[Alias("about")]
		[Summary("Shows elaborate bot info")]
		[Remarks("info")]
		//[RequireContext(ContextType.DM)]
		public async Task Info([Remainder] string rem = null)
		{
			var application = await Context.Client.GetApplicationInfoAsync();
			var client = (Context.Client as DiscordSocketClient);
			await ReplyAsync($"{Format.Bold($"Info for {Program.fullname}")}\n" +
			                 $"- Author: {application.Owner.Username}${application.Owner.Discriminator} (ID {application.Owner.Id})\n" +
			                 $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
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

		[Command("whois")]
		[Summary("Whois user lookup")]
		[Remarks("whois <username/nickname> --OR-- whois role:<rolename>\nwhois the guide --OR-- whois role:admin")]
		//[RequireContext(ContextType.DM)]
		public async Task Whois(string username, [Remainder][Summary("the username/nickname or predicate")]string rem = null)
		{
			var client = (Context.Client as DiscordSocketClient);
			if (username.StartsWith("role:", StringComparison.CurrentCultureIgnoreCase))
			{
				var role = username.Substring(5);
				// Get all users in guilds with this role
				var users =
					client?.Guilds
						.AsParallel()
						.WithDegreeOfParallelism(3)
						.Select(g =>
							g.Users.Where(u =>
								g.Roles.Any(r =>
									!r.IsEveryone
									&& r.Name.ToUpper().Contains(role.ToUpper())
									&& u.RoleIds.Contains(r.Id))))
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
							              $"Roles: {(client?.Guilds.ElementAtOrDefault(i).Roles.Where(urole => !urole.IsEveryone && u.RoleIds.Contains(urole.Id))).PrintRoles()}\n"));
					}

					// Prints all roles together
					//var sb2 = new StringBuilder();
					//client?.Guilds.ToList().ForEach(g => g.Roles.ToList().Where(srole => users.Any(user => user.Any(p => p.RoleIds.Contains(srole.Id))) && !srole.IsEveryone).ToList().ForEach(r => sb2.Append($"{r.Name}, ")));
					//sb.AppendLine($"\n**All roles on users:**\n{sb2}".Truncate(2));
					await ReplyAsync($"{sb}");
				}
				else await ReplyAsync($"No users found matching {predicate} predicate.");
			}
			else
			{
				var users =
					client?.Guilds
						.AsParallel()
						.WithDegreeOfParallelism(2)
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

		//[Command("links")]
		//[Summary("Shows a list of useful links")]
		//[Remarks("links")]
		//public async Task Links([Remainder] string rem = null)
		//{
		//	var updateschannel = (Context.Guild as SocketGuild)?.Channels.FirstOrDefault(x => x.Name.ToUpper() == "TMODLOADER-UPDATES") as ITextChannel;
		//	var faqchannel = (Context.Guild as SocketGuild)?.Channels.FirstOrDefault(x => x.Name.ToUpper() == "FAQ") as ITextChannel;
		//	await ReplyAsync(
		//		$"**tML thead**: <http://forums.terraria.org/index.php?threads/1-3-tmodloader-a-modding-api.23726/>\n" +
		//		$"**tML github**: <https://github.com/bluemagic123/tModLoader/>\n" +
		//		$"**tML releases**: <https://github.com/bluemagic123/tModLoader/releases>\n" +
		//		$"**tML updates**: " + (updateschannel != null ? updateschannel.Mention : "#tmodloader-updates") + "\n" +
		//		$"**FAQ**: " + (faqchannel != null ? faqchannel.Mention : "#faq"));
		//}

		public class UsageAttribute : Attribute
		{
			public UsageAttribute(string text)
			{
				Text = text;
			}

			public string Text { get; }
		}

		// sorry im evil, this code sucks
		private const string queryUrl = "http://javid.ddns.net/tModLoader/tools/querymoddownloadurl.php?modname=";
		private const string widgetUrl = "http://javid.ddns.net/tModLoader/widget/widgetimage/";
		
		[Command("widget")]
		[Alias("widgetimg", "widgetimage")]
		[Summary("Generates a widget image of specified mod")]
		[Remarks("widget <internal modname>\nwidget examplemod")]
		public async Task Widget([Remainder][Summary("the mod name")]string mod = "")
		{
			if (mod.Length <= 0)
			{
				await ReplyAsync("You must enter a mod name.");
				return;
			}
			Task<IUserMessage> sendMessageAsync = Context.Channel?.SendMessageAsync($"Please wait while the widget is generated...");
			if (sendMessageAsync != null)
			{
				var waitMsg = await sendMessageAsync;
				var request = WebRequest.Create($"{queryUrl}{mod}");
				request.Proxy = null;
				using (var response = await request.GetResponseAsync())
				using (var reader = new StreamReader(response.GetResponseStream()))
				{
					string readString = await reader.ReadToEndAsync();
					if (readString.StartsWith("Failed"))
					{
						await JsonHandler.MaintainContent(Context.Client);

						var sb = new StringBuilder();
						sb.AppendLine($"Mod with that name doesn\'t exist\nDid you possibly mean any of these?\n");

						JsonHandler.modnames.Where(n => n.ToUpper().Contains(mod?.ToUpper()))
							.ToList().ForEach(n => sb.Append($"``{n}``, "));

						if (sb.ToString().EndsWith(Environment.NewLine))
							sb.Append("No mods found...");

						await ReplyAsync($"{sb.ToString().Truncate(2)}");
						await waitMsg.DeleteAsync();
						return;
					}
					using (var widgetresponse = await WebRequest.Create($"{widgetUrl}{mod}.png").GetResponseAsync())
					using (var widgetstream = widgetresponse.GetResponseStream())
					{
						var sendFileAsync = Context.Channel?.SendFileAsync(widgetstream, $"{mod}.png", $"Widget for ``{mod}``");
						if (sendFileAsync != null)
							await sendFileAsync;
					}
				}
				await waitMsg.DeleteAsync();
			}
		}

		[Command("github")]
		[Alias("gh")]
		[Summary("Returns a search link for github matching your predicate")]
		[Remarks("github <search predicate>\ngithub tmodloader,mod in:name,description,topic")]
		public async Task Github([Remainder][Summary("the predicate")]string rem = "")
		{
			await ReplyAsync($"Uri: https://github.com/search?q={Uri.EscapeDataString(rem)}&type=Repositories");
		}

		// Note: This example is obsolete, a boolean type reader is bundled with Discord.Commands

		[Command("itemid")]
		[Alias("item")]
		[Summary("Searches for an item")]
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
			//var builder = new EmbedBuilder();

			//var urlSpace = kvp.Key.AddSpacesToSentence().ReplaceWhitespace("_");
			//builder.WithUrl($"http://terraria.gamepedia.com/{urlSpace}");
			//HtmlDocument doc = new HtmlDocument();
			//HttpWebRequest objRequest = (HttpWebRequest)WebRequest.Create($"http://terraria.gamepedia.com/index.php?title={urlSpace}&mobileaction=toggle_view_mobile");
			//var objResponse = await objRequest.GetResponseAsync();
			//using (StreamReader sr =
			//   new StreamReader((objResponse as HttpWebResponse)?.GetResponseStream()))
			//{
			//	doc.LoadHtml(sr.ReadToEnd());
			//}
			//builder.WithTitle($"{kvp.Key} ({kvp.Value})");
			//builder.WithImageUrl($"http://37.139.15.41/discord/items/img/Item_{kvp.Value}.png");
			//builder.WithColor(Color.Default);
			//builder.WithCurrentTimestamp();
			//var element =
			//	doc.DocumentNode.Descendants("table").First(t => t.Attributes["class"].Value.Contains("infobox"));
			//var elementSplit = element.InnerText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
			//var tmpList = elementSplit.ToList();
			//tmpList.RemoveRange(0, elementSplit.Length - 6);
			//elementSplit = tmpList.ToArray();
			//builder.WithDescription(string.Join("\n", elementSplit));
			//await ReplyAsync(replyText, false, builder.Build());
			await ReplyAsync(replyText);
		}

		[Command("dustid")]
		[Alias("dust")]
		[Summary("Searches for a dust")]
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

		[Command("chainid")]
		[Alias("chain")]
		[Summary("Searches for a chain")]
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

		[Command("ammoid")]
		[Alias("ammo")]
		[Summary("Searches for an ammo")]
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

		[Command("buffid")]
		[Alias("buff")]
		[Summary("Searches for an buff")]
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

		[Command("mod")]
		[Alias("modinfo")]
		[Summary("Shows info about a mod")]
		[Remarks("mod <internal modname> --OR-- mod <part of name>\nmod examplemod")]
		public async Task Mod([Remainder][Summary("The mod name or part of it")] string mod = "")
		{
			// Name must have some content
			if (mod.Length <= 0)
			{
				await ReplyAsync("You must enter a mod name.");
				return;
			}
			// Maintain json
			await JsonHandler.MaintainContent(Context.Client);
			// Use mod string
			var usemod = mod.RemoveWhitespace();
			var sb = new StringBuilder();
			var usestr = "";

			// If there is no mod found with this name
			if (!JsonHandler.modnames.Any(m 
				=> string.Equals(m.ToUpper(), usemod.ToUpper(), StringComparison.CurrentCulture)))
			{
				sb.AppendLine($"Mod with that name doesn\'t exist\nDid you possibly mean any of these?\n");

				// Get all mod names which contain the input, then append it to the strinbuilder in ``name``, format
				JsonHandler.modnames.Where(n => n.Contains(mod, StringComparison.CurrentCultureIgnoreCase))
					.ToList().ForEach(n => sb.Append($"``{n}``, "));

				// No mods were found.
				if (sb.ToString().EndsWith(Environment.NewLine))
					sb.Append("No mods found...");

				// Cap to 2000 chars, max chars for a discord message
				usestr = sb.ToString().Cap(2000);
				// If msg is capped and doesn't end with ``, we truncate past the last comma
				if (usestr.Length >= 2000 && !usestr.EndsWith("``"))
				{
					int index = usestr.LastIndexOf(',');
					if (index > 0)
						usestr = usestr.Substring(0, index);
				}

				// Reply
				await ReplyAsync($"{usestr}");
				return;
			}

			// Some mod is found continue.
			int count = 0;
			string truncated = "";

			// for every JObject in the modlist JArray, where mod "name" are equal to the input or the "name" contains the input
			// showcases 2 ways of getting "name", x.Value<string>("name") 
			// you can also do (x as JObject)?.SelectToken("name") then cast it to string, or use .ToObject<string>()
			foreach (
				JObject jToken in JsonHandler.modlist.Where(x =>
				string.Equals((string)(x as JObject)?.SelectToken("name").ToObject<string>(), usemod, StringComparison.CurrentCultureIgnoreCase)
				|| x.Value<string>("name").Contains(usemod, StringComparison.CurrentCultureIgnoreCase)))
			{
				// Already displayed 3 mod info, truncate the rest
				if (count >= 3)
				{
					truncated += $"``{jToken.Property("name").Value}``, ";
					continue;
				}

				// Append all property info of token to stringbuilder (name and value)
				foreach (JProperty prop in jToken.Properties())
				{
					string propvalue = prop.Name == "name" || prop.Name == "displayname" ? $"``{prop.Value}``" : $"{prop.Value}";
					sb.AppendLine($"**{prop.Name}**: {propvalue}");
				}
				// Custom 'property'
				sb.AppendLine($"**widget**: {widgetUrl}{jToken.Property("name").Value}.png\n");
				count++;
			}
			if (count >= 3)
				sb.Append($"**Truncated mods**: {truncated.Truncate(2)}");

			// Cap to 2000 chars, max chars for a discord message
		    usestr = sb.ToString().Cap(2000);
			// If msg is capped and doesn't end with ``, we truncate past the last comma
			if (!usestr.EndsWith("``"))
			{
				int index = usestr.LastIndexOf(',');
				if (index > 0)
					usestr = usestr.Substring(0, index);
			}

			await ReplyAsync($"{usestr}");
		}

		// forgive me for this piece of crap, I don't feel motivated to make this a proper piece of code atm.
		// also, I believe it does not check aliases
		[Command("help")]
		[Alias("guide")]
		[Summary("Shows info about commands")]
		[Remarks("help ([module]) [command]\nhelp whois --OR-- help tag create")]
		public async Task Help([Remainder] string rem = null)
		{
			string total = "";
			if (rem != null)
			{
				if (rem.Any(char.IsWhiteSpace))
				{
					var split = rem.Split(' ');
					if (split.Any())
					{
						var module = split[0];
						var command = split[1];
						var cmd = service.Commands.FirstOrDefault(x => x.Module.Aliases.First().ToUpper() == module.ToUpper() && x.Name.ToUpper() == command.ToUpper());
						if (cmd != null)
						{
							string summ = cmd.Summary == null || cmd.Summary.Length > 0 ? cmd.Summary : "No summary";
							var remarkSplit = cmd.Remarks.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
							var remarks = remarkSplit.Length > 1
								? $"``{CommandHandler.prefixChar}{remarkSplit[0]}``\n**Example:** ``{CommandHandler.prefixChar}{remarkSplit[1]}``"
								: $"``{CommandHandler.prefixChar}{cmd.Remarks}``";
							await ReplyAsync($"**Command info for** ``{module} {command}``\n" +
								$"**Module**: ``{module}``\n**Command**: ``{command}``\n**Summary**: ``{summ}``\n**Usage**: {remarks}");
						}
						else
							await ReplyAsync($"Command ``{command}`` from module ``{module}`` not found.");
						return;
					}
				}
				var cmds = service.Commands.Where(x => x.Module.Aliases.First().ToUpper() == rem.ToUpper());
				if (cmds.Any())
				{
					foreach (var command in cmds)
					{
						var result = await command.CheckPreconditionsAsync(Context, map);
						if (result.IsSuccess)
							total += command.Name + ", ";
					}
					if (total.Length > 2)
						total = total.Truncate(2);
					await ReplyAsync($"**Usable commands for {Context.User.Username}**\n" +
						$"**{rem}**: {total}");
				}
				else
				{
					var cmd = service.Commands.FirstOrDefault(x => x.Name.ToUpper() == rem.ToUpper());
					if (cmd != null && cmd.Module.Aliases.First() == "")
					{
						string summ = cmd.Summary == null || cmd.Summary.Length > 0 ? cmd.Summary : "No summary";
						var remarkSplit = cmd.Remarks.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
						var remarks = remarkSplit.Length > 1
								? $"``{CommandHandler.prefixChar}{remarkSplit[0]}``\n**Example:** ``{CommandHandler.prefixChar}{remarkSplit[1]}``"
								: $"``{CommandHandler.prefixChar}{cmd.Remarks}``";
						await ReplyAsync($"**Command info for ``{cmd.Name}``**\n" +
						$"**Aliases**: {cmd.Aliases.PrettyPrint()}\n**Module**: ``{cmd.Module}``\n**Summary**: ``{summ}``\n**Usage**: {remarks}");
					}
					else
					{
						await ReplyAsync($"Command ``{rem}`` not found");
					}
				}
				return;
			}
			foreach (var command in service.Commands)
			{
				if (command.Module.Aliases.First() == "")
				{
					var result = await command.CheckPreconditionsAsync(Context, map);
					if (result.IsSuccess)
						total += command.Name + ", ";
				}
			}
			if (total.Length > 2)
				total = total.Truncate(2);
			string modules = "";
			foreach (var module in service.Modules)
			{
				if (new string[] { "COMMANDS", "OWNER" }.All(x => x != module.Name.ToUpper()) && module.Commands.Any())
				{
					modules += $"\n_{module.Name}_: ";
					foreach (var command in module.Commands)
					{
						var result = await command.CheckPreconditionsAsync(Context, map);
						if (result.IsSuccess)
							modules += $"{command.Name}, ";
					}
					modules = modules.Truncate(2);
				}
			}

			await ReplyAsync($"**Usable commands for {Context.User.Username}**\n" +
				$"{total}" +
				$"{modules}\n\n" +
				$"Get command specific help: ``{CommandHandler.prefixChar}help <name>`` or ``{CommandHandler.prefixChar}help <module> <name>``");
		}
	}
}
