using System;
using System.Collections.Generic;
using System.Diagnostics;
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
				$"I am running on `{ConfigManager.Properties.Version}`\n");

	}
}
