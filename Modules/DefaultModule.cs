using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Preconditions;
using Discord.Commands;
using Discord.WebSocket;
using NCalc;

namespace TheGuide.Modules
{
	public class DefaultModule : GuideModuleBase<SocketCommandContext>
	{
		[Command("ping")]
		[Alias("status")]
		[Summary("Returns the bot response time")]
		[Remarks("ping")]
		[Ratelimit(2, 1, Measure.Minutes)]
		public async Task Ping([Remainder] string rem = null)
		{
			var sw = Stopwatch.StartNew();
			var latency = Client.Latency;
			var color = Helpers.Colors.GetLatencyColor(latency);

			var embed = new EmbedBuilder()
				.WithTitle("Bot response time")
				.WithDescription($"Latency: `{latency} ms`")
				.WithColor(color);

			var msg = await ReplyAsync(string.Empty, false, embed.Build());
			await msg.ModifyAsync(
				x =>
					x.Embed = embed.WithDescription($"Heartrate: `{60d / latency * 1000:####} bpm`" +
													$"\nLatency: `{latency} ms`" +
													$"\nMessage: `{sw.ElapsedMilliseconds} ms`" +
													$"\nDelta: `{Math.Abs(sw.ElapsedMilliseconds - latency)} ms`").Build());
		}

		[Command("version")]
		[Summary("Returns the bot version")]
		[Remarks("version")]
		[Ratelimit(2, 1, Measure.Minutes)]
		public async Task Version([Remainder] string rem = null) =>
			await ReplyAsync(
				$"I am running on `{ConfigManager.Properties.Version}` using `Discord.NET {DiscordConfig.Version} [API:{DiscordConfig.APIVersion}]`");

		[Command("source")]
		[Alias("src", "source-code")]
		[Summary("Returns a link to the github repository of the bot")]
		[Remarks("source")]
		[Ratelimit(2, 1, Measure.Minutes)]
		public async Task Source([Remainder] string rem = null) =>
			await ReplyAsync("Here's how I am made! <https://github.com/Jofairden/TheGuide>");

		[Command("info")]
		[Alias("about")]
		[Summary("Shows elaborate bot info")]
		[Remarks("info")]
		[Ratelimit(2, 1, Measure.Minutes)]
		//[RequireContext(ContextType.DM)]
		public async Task Info([Remainder] string rem = null)
		{
			var application = await Context.Client.GetApplicationInfoAsync();
			var client = Context.Client;

			await ReplyAsync(string.Empty, false, new EmbedBuilder()
				.AddInlineField("Author", $"{application.Owner.GenFullName()}\n[ID: {application.Owner.Id}]")
				.AddInlineField("Library", $"Discord.Net ({DiscordConfig.Version}) [API: {DiscordConfig.APIVersion}]")
				.AddInlineField("Uptime", $"{Helpers.GetUptime()}\n(dd\\.hh\\:mm\\:ss)")
				.AddInlineField("Bot version", ConfigManager.Properties.Version)
				.AddInlineField("Heap Size", $"{Helpers.GetHeapSize()} MB")
				.AddField("Other", $"Guilds: {client?.Guilds.Count}" +
				                         $"\nChannels: {client?.Guilds.Sum(g => g.Channels.Count)}" +
				                         $" [Text: {client?.Guilds.Sum(g => g.TextChannels.Count)}]" +
				                         $" [Voice: {client?.Guilds.Sum(g => g.VoiceChannels.Count)}]\n" +
				                         $"Roles: {client?.Guilds.Sum(g => g.Roles.Count)}\n" +
				                         $"Emojis: {client?.Guilds.Sum(g => g.Emojis.Count)}\n" +
				                         $"Users: {client?.Guilds.Sum(g => g.MemberCount)}" +
				                         $" [Cached: {client?.Guilds.Sum(g => g.Users.Count)}]")
				.Build());
		}

		[Command("github")]
		[Alias("gh")]
		[Summary("Returns a search link for github searching for repositories matching your predicate")]
		[Remarks("github <search predicate>\ngithub tmodloader,mod in:name,description,topic")]
		[Ratelimit(20, 1, Measure.Hours)]
		public async Task Github([Remainder]string rem) =>
			await ReplyAsync($"Generated: <https://github.com/search?q={Uri.EscapeDataString(rem)}&type=Repositories>");

		[Command("evaluate")]
		[Alias("eval")]
		[Ratelimit(20, 1, Measure.Hours)]
		public async Task Evaluate([Remainder] string data)
		{
			var sw = Stopwatch.StartNew();
			var dt = new Expression(data, EvaluateOptions.IgnoreCase);
			dynamic eval = dt.Evaluate();
			await ReplyAsync(string.Empty, false, new EmbedBuilder()
				.WithTitle("Evaluation")
				.WithDescription($"{data}")
				.WithColor(Helpers.Colors.GetLatencyColor(sw.ElapsedMilliseconds))
				.AddField(x => x.WithName("Result").WithValue(eval.ToString()))
				.Build());
		}
	}
}
