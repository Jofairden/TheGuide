using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using TheGuide.System;

namespace TheGuide.Modules
{
	public class GuideModuleBase<T> : ModuleBase<T> where T : class, ICommandContext
	{
		//property injection fixed in 00681
		public IDependencyMap DependencyMap { get; set; }
		public DiscordSocketClient Client { get; set; }
		public CommandService CommandService { get; set; }
		public ModSystem ModSystem { get; set; }

		// You can override ReplyAsync, do some stuff.
		protected override Task<IUserMessage> ReplyAsync(string message, bool isTTS = false, Embed embed = null,
			RequestOptions options = null)
		{
			// Try to replace @everyone or @here if present
			// \x200B is a no-width blank space
			// By inserting it we basically sabotage the mentions
			return base.ReplyAsync(
				message.Replace(Context.Guild.EveryoneRole.Mention, "@every\x200Bone").Replace("@here", "@he\x200Bre"), isTTS,
				embed, options);
		}


		internal async Task<bool> ShowSimilarMods(string mod)
		{
			var sw = Stopwatch.StartNew();
			var mods = ModSystem.ModFiles.Where(m => string.Equals(m, mod, StringComparison.CurrentCultureIgnoreCase));

			if (mods.Any()) return true;
			var cached = await ModSystem.TryCacheMod(mod);
			if (cached) return true;

			var embed = new EmbedBuilder().WithTitle("Mod not found");
			var description = "No similar mods found...";
			mods = ModSystem.ModFiles;
			// Find similar mods
			var similarMods =
				mods
					.Where(m =>
						m.Contains(mod, StringComparison.OrdinalIgnoreCase)
						&& Helpers.DamerauLevenshteinDistance(m, mod, m.Length) <= m.Length - 2) 
						// prevents insane amount of mods found
					.ToArray();

			if (similarMods.Any())
			{
				description = "Did you possibly mean any of these?";
				embed.AddField("Similar mods", similarMods.PrettyPrint());
			}

			embed.WithDescription(description);
			embed.WithColor(Helpers.Colors.GetLatencyColor(sw.ElapsedMilliseconds, 500f));
			embed.WithFooter(x => x.WithText($"{sw.ElapsedMilliseconds} ms"));

			await ReplyAsync(string.Empty, false, embed.Build());
			return false;
		}
	}
}
