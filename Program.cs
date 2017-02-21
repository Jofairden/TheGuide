using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using TheGuide.ID;
using TheGuide.Systems;
// Just using this to show off it's possible, and how to.
using stringShortDict = System.Collections.Generic.Dictionary<string, short>;
using stringIntDict = System.Collections.Generic.Dictionary<string, int>;

namespace TheGuide
{
	// simplified:
	// run bot: bash bot.sh
	// kill bot: bash kill.sh
	// list: bash list.sh
	// update bot: run publish.bat in root, ftp published to vps

	// To start bot:
	// open ssh shell to vps and connect
	// cd ./bot
	// screen -S TheGuide dotnet TheGuide.dll
	// to get out of the screen: ctrl + a + ctrl + d
	//
	// To update:
	// open cmd
	// cd to root directory of project
	// dotnet publish
	// ftp ./bin/debug/netcoreapp1.0/publish contents to vps ('bot' folder)
	//
	// To terminate:
	// htop
	// select 'TheGuide' screen which is not lit up green
	// press F9 ('Kill')
	// press Enter
	public class Program
	{
		public static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

		// Variables
		//public const bool maintenanceMode = false;
		private const ulong clientid = 282831244083855360;
		private const ulong permissions = 536345663;
		public const string version = "r-3.0";

		// Cache
		public static stringShortDict itemConsts;
		public static stringShortDict dustConsts;
		public static stringShortDict chainConsts;
		public static stringIntDict ammoConsts;
		public static stringIntDict buffConsts;

		// Variables 
		private readonly Dictionary<ulong, DateTime> cooldowns = new Dictionary<ulong, DateTime>();
		private string oath2Url = "https://discordapp.com/api/oauth2/authorize";
		private DiscordSocketClient client;
		private CommandHandler handler;
		private DateTime time { get; set; } = DateTime.Now;
		public static string AssemblyDirectory => Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetEntryAssembly().CodeBase).Path));

		// Start of App
		public async Task Start()
		{
			// Setup, cache
			time = DateTime.Now;
			itemConsts = new stringShortDict(typeof(ItemID).GetAllPublicConstants<short>(), StringComparer.CurrentCultureIgnoreCase);
			dustConsts = new stringShortDict(typeof(DustID).GetAllPublicConstants<short>(), StringComparer.CurrentCultureIgnoreCase);
			chainConsts = new stringShortDict(typeof(ChainID).GetAllPublicConstants<short>(), StringComparer.CurrentCultureIgnoreCase);
			ammoConsts = new stringIntDict(typeof(AmmoID).GetAllPublicConstants<int>(), StringComparer.CurrentCultureIgnoreCase);
			buffConsts = new stringIntDict(typeof(BuffID).GetAllPublicConstants<int>(), StringComparer.CurrentCultureIgnoreCase);

			// Begin app
			Console.Title = $"The Guide {version} - {AssemblyDirectory}";
			await Console.Out.WriteLineAsync($"{oath2Url}?client_id={clientid}&scope=bot&permissions={permissions}");
			await Console.Out.WriteLineAsync($"Start date: {time}");

			// Client
			client = new DiscordSocketClient(new DiscordSocketConfig()
			{
				LogLevel = LogSeverity.Verbose,
				DefaultRetryMode = RetryMode.AlwaysFail,
				MessageCacheSize = 10,
				ConnectionTimeout = 10000
			});

			client.Log += Client_Log;

			// Connection
			// Token.cs is left out intentionally
			await client.LoginAsync(TokenType.Bot, Token.BotToken);
			await client.ConnectAsync();
			await Task.Delay(1000); // Give some time to connect

			// After connection
			await Client_LatencyUpdated(client.Latency, client.Latency);
			client.LatencyUpdated += Client_LatencyUpdated;
			client.JoinedGuild += Client_JoinedGuild;

			// Map
			var map = new DependencyMap();
			map.Add(client);
			map.Add(cooldowns);

			// json Maintainer
			await JsonSystem.Setup(client);
			var timer = new Timer(async s => await JsonSystem.MaintainContent(client),
			null,
			TimeSpan.FromMinutes(5),
			TimeSpan.FromDays(0.5d));

			// Handler
			handler = new CommandHandler();
			await handler.Install(map);

			await client.SetGameAsync("Terraria");

			await Task.Delay(-1);
		}

		private async Task Client_JoinedGuild(SocketGuild arg)
		{
			await JsonSystem.CreateTagDir(arg.Id);
		}

		private async Task Client_Log(LogMessage e)
		{
			await Console.Out.WriteLineAsync($"~{$"[{e.Severity}]",-10}{$"[{e.Source}]",-10}{$"[{e.Message}]",-10}~");
		}

		private async Task Client_LatencyUpdated(int i, int j)
		{
			if (client == null) return;

			await client.SetStatusAsync(
				//maintenanceMode ? UserStatus.DoNotDisturb :
				(client.ConnectionState == ConnectionState.Connecting || j > 250) ? UserStatus.Idle
				: (client.ConnectionState == ConnectionState.Disconnected || j > 500) ? UserStatus.DoNotDisturb
				: UserStatus.Online);

			//await client.SetGameAsync(/*maintenanceMode ? "maintenance" : */"Terraria");
		}
	}
}
