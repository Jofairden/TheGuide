using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
		public static void Main(string[] args) => 
			new Program().Start().GetAwaiter().GetResult();

		// Variables
		//public const bool maintenanceMode = false;
	    internal object _locker = new object();
		private const ulong clientid = 282831244083855360;
		private const ulong permissions = 536345663;
		public const string version = "r-3.5";

		// Cache
		public static stringShortDict itemConsts;
		public static stringShortDict dustConsts;
		public static stringShortDict chainConsts;
		public static stringIntDict ammoConsts;
		public static stringIntDict buffConsts;

		// Variables 
		internal CancellationTokenSource SystemTC;
		internal CancellationToken SystemCT;

		private readonly Dictionary<ulong, DateTime> cooldowns = new Dictionary<ulong, DateTime>();
		private string oath2Url = "https://discordapp.com/api/oauth2/authorize";
		private DiscordSocketClient client;
		private CommandHandler handler;
		private DateTime time { get; } = DateTime.Now;

		// Start of App
		public async Task Start()
		{
			// Setup, cache
			itemConsts = new stringShortDict(typeof(ItemID).GetAllPublicConstants<short>(), StringComparer.CurrentCultureIgnoreCase);
			dustConsts = new stringShortDict(typeof(DustID).GetAllPublicConstants<short>(), StringComparer.CurrentCultureIgnoreCase);
			chainConsts = new stringShortDict(typeof(ChainID).GetAllPublicConstants<short>(), StringComparer.CurrentCultureIgnoreCase);
			ammoConsts = new stringIntDict(typeof(AmmoID).GetAllPublicConstants<int>(), StringComparer.CurrentCultureIgnoreCase);
			buffConsts = new stringIntDict(typeof(BuffID).GetAllPublicConstants<int>(), StringComparer.CurrentCultureIgnoreCase);

			// Begin app
			Console.Title = $"The Guide {version} - {AppContext.BaseDirectory}";
			await Console.Out.WriteLineAsync($"{oath2Url}?client_id={clientid}&scope=bot");
			await Console.Out.WriteLineAsync($"Start date: {time}");

			// Client
			client = new DiscordSocketClient(new DiscordSocketConfig
			{
				LogLevel = LogSeverity.Verbose,
				AlwaysDownloadUsers = true,
				MessageCacheSize = 50,
			});

			client.Log += Client_Log;
			client.LatencyUpdated += Client_LatencyUpdated;
			client.GuildMemberUpdated += Client_GuildMemberUpdated;
			client.UserJoined += Client_UserJoined;
			client.UserLeft += Client_UserLeft;
			client.ChannelDestroyed += Client_ChannelDestroyed;
			client.Ready += Client_Ready;

			// Connection
			// Token.cs is left out intentionally
			await client.LoginAsync(TokenType.Bot, Token.TestToken);
			await client.StartAsync();
			//await Task.Delay(5000); // Give some time to connect

			// After connection


			// Map
			var map = new DependencyMap();
			map.Add(cooldowns);

			// Handler
			handler = new CommandHandler();
			await handler.Install(client, map);


			SystemTC = new CancellationTokenSource();
			SystemCT = SystemTC.Token;

			await Task.Run(async () =>
			{
				while (true)
				{
					try
					{
						if (SystemCT.IsCancellationRequested) break;
						await ConfigSystem.Maintain(client);
						await TagSystem.Maintain(client);
						await SubSystem.Maintain(client);
						await ModSystem.Maintain(client);
						await Task.Delay(900000, SystemCT);
					}
					catch (Exception e)
					{
						await Client_Log(new LogMessage(LogSeverity.Critical, "SystemMain", "", e));
					}
				}
			}, SystemCT);

			await Task.Delay(-1);
		}

		private async Task Client_Ready()
		{
			if (!client.CurrentUser.Game.HasValue)
			{
				await client.SetGameAsync("READY " + Program.version);
				await Task.Delay(5000);
				await client.SetGameAsync("Terraria");
			}
		}

		private async Task Client_ChannelDestroyed(SocketChannel c)
		{
			var ch = c as SocketGuildChannel;
			if (ch != null)
			{
				await SubSystem.MaintainServer(ch.Guild);
			}
		}

		private async Task Client_UserLeft(SocketGuildUser u)
		{
			var result = await SubSystem.DeleteUserSub(u.Guild.Id, u.Id);
			if (!result.IsSuccess)
				await Client_Log(new LogMessage(LogSeverity.Error, "Client:SubSystem", result.ErrorReason));
		}

		private async Task Client_UserJoined(SocketGuildUser u)
		{
			await SubSystem.MaintainUser(u.Guild, u);
			var ch = await u.CreateDMChannelAsync();
			await ch.SendMessageAsync(
				$"Hey there {u.Username}!\n" +
				$"I see you just joined our server, how lovely!\n" +
				$"Send ``{CommandHandler.prefixChar}help`` in the server and I will show you how I work!\n" +
				$"\n" +
				$"Our server uses a channel subscription system!\n" +
				$"Type ``{CommandHandler.prefixChar}sub list`` to see existing subscriptions or ``{CommandHandler.prefixChar}help module:sub`` for more info.\n" +
				$"\n" +
				$"Use ``{CommandHandler.prefixChar}changelog`` to see my most recent changes!" +
				$"\n" +
				$"\n" +
				$"Have a nice stay! :wave:");
		}

		private async Task Client_GuildMemberUpdated(SocketGuildUser i, SocketGuildUser j)
		{
			await SubSystem.MaintainUser(j.Guild, j);
		}

		private const int offset = -10;

	    private async Task Client_Log(LogMessage e)
	    {
	        var time = DateTime.Now.ToString("MM-dd-yyy", CultureInfo.InvariantCulture);
	        var path = Path.Combine(AppContext.BaseDirectory, "dist", "logs");
	        var filepath = Path.Combine(path, time + ".txt");
            var msg = $"~{$"[{e.Severity}]",offset}{$"[{e.Source}]",offset}{$"[{e.Message}]",offset}~";
           
	        lock (_locker)
	        {
                Directory.CreateDirectory(path);
                if (!File.Exists(filepath))
                    File.Create(filepath);
                File.AppendAllText(filepath, msg + "\r\n");
            }       
            await Console.Out.WriteLineAsync(msg);
        }
			

		private async Task Client_LatencyUpdated(int i, int j)
		{
			await client.SetStatusAsync(
				//maintenanceMode ? UserStatus.DoNotDisturb :
				(client.ConnectionState == ConnectionState.Disconnected || j > 500) ? UserStatus.DoNotDisturb
				: (client.ConnectionState == ConnectionState.Connecting || j > 250) ? UserStatus.Idle
				: UserStatus.Online);
		}
	}
}
