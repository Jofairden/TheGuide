using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheGuide.Systems;

namespace TheGuide
{
    // (c) Jofairden - 2016/2017 - For Discord: tModLoader
    public class Program
    {
        public static void Main(string[] args)
        {
            new Program().Start().GetAwaiter().GetResult();
        }

        public const bool devMode = true;
        public static string AssemblyDirectory;

        public const string version = "r-1.1";
        //private static string rootDir;
        public static DateTime time;

        private ulong[] allowedGuilds;
        private const ulong guildid = 103110554649894912;
        private const ulong clientid = 239075803290271745;
        private const ulong permissions = 536345663;
        private DiscordSocketClient client;
        private CommandHandler handler;
        private BotLogger logger;
        private BotHelper helper;
        private TagSystem tags;
        private static string token
        {
            get { return "MjM5MDc1ODAzMjkwMjcxNzQ1.Cu1ZAA.ixd5nuV-l6_cacqiHzcZxI0QXCY"; }
        }

        private Dictionary<ulong, DateTime> cooldowns = new Dictionary<ulong, DateTime>();

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

        public async Task Start()
        {
            time = DateTime.Now;
            string codeBase = Assembly.GetEntryAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            AssemblyDirectory = Path.GetDirectoryName(path);
            //rootDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().CodeBase);
            //Console.WriteLine(AssemblyDirectory);
            Console.Title = $"TheGuide - Discord Bot - Auth: Jofairden - {version} - {AssemblyDirectory}";

            //https://discordapp.com/oauth2/authorize?client_id=239075803290271745&scope=bot&permissions=536345663
            await Console.Out.WriteLineAsync($"https://discordapp.com/oauth2/authorize?client_id={clientid}&scope=bot&permissions={permissions}");
            await Console.Out.WriteLineAsync($"Start date: {time}");

            allowedGuilds = new ulong[]
            {
                guildid, 216276491544166401
            };

            client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Verbose
            });

            helper = new BotHelper(client);
			logger = new BotLogger(AssemblyDirectory);
			tags = new TagSystem(AssemblyDirectory);

			client.Log += Log;
            client.JoinedGuild += JoinGuild;
            client.MessageReceived += ListenLogs;

            await client.LoginAsync(TokenType.Bot, token);
            await client.ConnectAsync();

	        await Task.Delay(1000);

			await tags.AssembleDirs(client);
			logger.AssembleDirs();

			var map = new DependencyMap();
            map.Add(client);
            map.Add(cooldowns);
            map.Add(tags);

            handler = new CommandHandler();
            await handler.Install(map);

            await Update(client.Latency, client.Latency);
            client.LatencyUpdated += Update;

			await Task.Delay(-1);
        }

        private async Task ListenLogs(SocketMessage arg)
        {
            if (new ulong[] { 215275694807187459 , 183893387584208896 }.Any(x => x == arg.Channel.Id))
            {
                if (arg.Channel.Id == 215275694807187459)
                {
                    await logger.DynamicLog(BotLogger.Type.modbot, arg.Content, BotLogger.Log.server);
                }
                else
                {
                    await logger.DynamicLog(BotLogger.Type.modbot, arg.Content, BotLogger.Log.moderation);
                }
            }
        }

        private async Task JoinGuild(SocketGuild arg)
        {
            if (!allowedGuilds.Any(x => x == arg.Id))
            {
                var channel = arg.GetChannel(arg.DefaultChannel.Id);
                await (channel as SocketTextChannel)?.SendMessageAsync($"Unauthorized access. Terminating connection with guild.");
	            await arg.LeaveAsync();
            }
        }

        private async Task Log(LogMessage e)
        {
            await Task.Yield();
            if (e.Exception != null)
            {
                Console.WriteLine($"$~{e.Exception,-10}");
                await logger.DynamicLog(BotLogger.Type.console, "EXCEPTION!", BotLogger.Log.console);
                await logger.DynamicLog(BotLogger.Type.console, e.Exception, BotLogger.Log.exception);
                
                return;
            }
            var txt = String.Join(" $~", $"{e.Severity, -10}", $"{e.Source, -10}", $"{e.Message, -10}");
            Console.WriteLine($"$~{txt}");
            await logger.DynamicLog(BotLogger.Type.console, txt, BotLogger.Log.console);
        }

        private async Task Update(int i, int j)
        {
            if (DateTime.Now.ToUniversalTime().Date > time.ToUniversalTime().Date)
            {
                logger.AssembleDirs();
            }

            var status = UserStatus.Online;
            if (client?.ConnectionState == ConnectionState.Connecting || j > 250)
            {
                status = UserStatus.Idle;
            }
            else if (client?.ConnectionState == ConnectionState.Disconnected || j > 500)
            {
                status = UserStatus.DoNotDisturb;
            }
            //await client.CurrentUser.ModifyStatusAsync(x =>
            //{
            //    x.Status = status;
            //    x.Game = new Discord.API.Game() { Name = "Terraria" };
            //});

            string game = devMode ? "maintenance in progress" : "Terraria";
            status = devMode ? UserStatus.DoNotDisturb : status;

            await client?.SetGameAsync(game);
            await client?.SetStatusAsync(status);
        }
    }
}
