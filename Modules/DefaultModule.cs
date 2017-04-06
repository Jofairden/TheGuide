using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace TheGuide.Modules
{
	public class DefaultModule : GuideModuleBase<SocketCommandContext>
	{
		//https://i.imgur.com/iPEfTwz.png
		//can be removed in some future build
		//property injection issue
		public DefaultModule(IDependencyMap map, DiscordSocketClient client, CommandService service) : base()
		{
			DependencyMap = map;
			Client = client;
			CommandService = service;
		}

		[Command("ping")]
		[Alias("status")]
		[Summary("Returns the bot response time")]
		public async Task Ping([Remainder] string rem = null)
		{
			var sw = Stopwatch.StartNew();
			var latency = Client.Latency;
			var color = latency >= 500
				? Helpers.Colors.SoftRed
				: latency >= 250 ? Helpers.Colors.SoftYellow
				: Helpers.Colors.SoftGreen;

			var embed = new EmbedBuilder()
				.WithTitle("Bot response time")
				.WithDescription($"Latency: `{latency} ms`")
				.WithColor(color);

			var msg = await ReplyAsync(string.Empty, false, embed.Build());
			await msg.ModifyAsync(
				x => x.Embed = embed.WithDescription($"Latency: `{latency} ms`\nMessage: `{sw.ElapsedMilliseconds} ms`\nDelta: `{sw.ElapsedMilliseconds - latency} ms`").Build());
		}
	}
}
