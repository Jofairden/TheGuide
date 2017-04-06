using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace TheGuide
{
	internal class Program
	{
		// It is recommended to run async outside of Main
		internal static void Main(string[] args) =>
			new Program().Run(args).GetAwaiter().GetResult();

		// With these variables, we can cancel tasks
		internal static CancellationTokenSource Cts;
		internal static CancellationToken Ct;
		internal static CommandHandler Handler;

		// This holds our bot client
		// If we distribute our bot, we might need to use a DiscordShardedClient
		// This is for bots that are in many guilds.
		// It is recommeneded you start using a DiscordShardedClient upon reaching 1500 guilds
		// Each shard can handle up to 2500 guilds
		private DiscordSocketClient _client;

		public async Task Run(string[] args)
		{
			// Init vars
			Cts = new CancellationTokenSource();
			Ct = Cts.Token;

			// Setup objects
			_client = new DiscordSocketClient(new DiscordSocketConfig()
			{
				AlwaysDownloadUsers = true,
				LogLevel = LogSeverity.Debug,
				MessageCacheSize = 250
			});

			// Setup event handlers
			_client.Log += ClientLog;
			_client.JoinedGuild += ClientJoinGuild;

			// awaitable login
			await Login();

			var map = new DependencyMap();
			map.Add(_client);

			Handler = new CommandHandler();
			await Handler.Install(map);

			Console.Title = "The Guide - " + ConfigManager.Properties.Version;

			// Block program until it's closed or task is canceled
			await Task.Delay(-1, Ct);
		}

		internal async Task Login()
		{
			var Waiter = new TaskCompletionSource<bool>();
#pragma warning disable 1998
			_client.Ready += async delegate
#pragma warning restore 1998
			{
				Waiter.SetResult(true);
			};

			// Login and start bot
			// For a locally running bot, it's fine to use an environment variable, for ease of use
			// If you distribute your bot, using a config.json is recommended.
			// Since we will showcase more options, we will use a config.json
			// To use an env variable, you can do something like this: Properties.Resources.ResourceManager.GetString("BOT_TOKEN")
			// Our ConfigManager is static, see ConfigManager.cs
			// For your config.json file, make sure Copy to output directory is set to Always
			await ConfigManager.Read();

			// First login, then start
			// Similar order reverse: StopAsync, LogoutAsync
			await _client.LoginAsync(TokenType.Bot, ConfigManager.Properties.Token);
			await _client.StartAsync();

			await Waiter.Task;
		}

		// We can do something when our bot joins a guild
		// For this example, we will send a DM to the bot application owner
		private async Task ClientJoinGuild(SocketGuild guild)
		{
			var appInfo = await _client.GetApplicationInfoAsync(); // Get app info of our bot
			var channel = await appInfo.Owner.CreateDMChannelAsync(); // Get the DM channel with our app owner, create it if it doesn't exist
			await channel.SendMessageAsync($"I just joined a guild: `{guild.Name}` ({guild.Id})"); // Send a new message in our DM channel
		}

		// Our own log handling.
		private Task ClientLog(LogMessage e)
		{
			// We use our own DateTime formatting, which already uncludes time, so we strip it from the LogMessage by calling substring(8)
			// Default LogMessage.ToString() format is: HH:mm:ss Type Source:Message/Exception
			string msg = $"[{DateTime.Now:dd/MM/yyyy H:mm:ss zzzz}]" + e.ToString().Substring(8);
			Console.WriteLine(msg);
			return Task.CompletedTask;
		}
	}
}