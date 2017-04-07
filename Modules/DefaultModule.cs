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
			var color =
				latency >= 500
					? Helpers.Colors.SoftRed
					: latency >= 250
						? Helpers.Colors.SoftYellow
						: Helpers.Colors.SoftGreen;

			var embed = new EmbedBuilder()
				.WithTitle("Bot response time")
				.WithDescription($"Latency: `{latency} ms`")
				.WithColor(color);

			var msg = await ReplyAsync(string.Empty, false, embed.Build());
			await msg.ModifyAsync(
				x =>
					x.Embed = embed.WithDescription($"Heartrate: `{60d / latency * 1000} bpm`" +
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
		public async Task Src([Remainder] string rem = null) =>
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

			var msg = $"{Format.Bold("Info")}\n" +
						$"- Author: {application.Owner.GenFullName()}" +
						$" [ID: {application.Owner.Id}]\n" +
						$"- Library: Discord.Net ({DiscordConfig.Version})" +
						$" [API: {DiscordConfig.APIVersion}]\n" +
						$"- Runtime: {AppContext.TargetFrameworkName} (r-{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion}) {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture} \n" +
						$"- Uptime: {Helpers.GetUptime()} (dd\\.hh\\:mm\\:ss)\n" +
						$"- Bot version: {ConfigManager.Properties.Version}" +
						$"\n\n{Format.Bold("Stats")}\n" +
						$"- Heap Size: {Helpers.GetHeapSize()} MB\n" +
						$"- Guilds: {client?.Guilds.Count}\n" +
						$"- Channels: {client?.Guilds.Sum(g => g.Channels.Count)}" +
						$" [Text: {client?.Guilds.Sum(g => g.TextChannels.Count)}]" +
						$" [Voice: {client?.Guilds.Sum(g => g.VoiceChannels.Count)}]\n" +
						$"- Roles: {client?.Guilds.Sum(g => g.Roles.Count)}\n" +
						$"- Emojis: {client?.Guilds.Sum(g => g.Emojis.Count)}\n" +
						$"- Users: {client?.Guilds.Sum(g => g.MemberCount)}" +
						$" [Cached: {client?.Guilds.Sum(g => g.Users.Count)}]";

			await ReplyAsync(msg);
		}
	}
}
