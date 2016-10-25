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
    // (c) Gorateron - 2016 - For Discord: tModLoader
    public class Program
    {
        public static void Main(string[] args)
        {
            new Program().Start().GetAwaiter().GetResult();
        }

        public const bool devMode = false;
        public static string AssemblyDirectory;

        public const string version = "r-1.0.5.2";
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
            get { return ""; }
        }
        private static Timer[] timers = new Timer[Enum.GetNames(typeof(TimerType)).Length];
        private Dictionary<ulong, DateTime> cooldowns = new Dictionary<ulong, DateTime>();

        private enum TimerType
        {
            GlobalMsg,
            Rainbow
        }

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
            Console.Title = $"TheGuide - Discord Bot - By: Gorateron - {version} - {AssemblyDirectory}";

            //https://discordapp.com/oauth2/authorize?client_id=239075803290271745&scope=bot&permissions=536345663
            await Console.Out.WriteLineAsync($"https://discordapp.com/oauth2/authorize?client_id={clientid}&scope=bot&permissions={permissions}");
            await Console.Out.WriteLineAsync($"Start date: {time}");

            allowedGuilds = new ulong[]
            {
                guildid, 216276491544166401
            };

            logger = new BotLogger(AssemblyDirectory);
            logger.AssembleDirs();
            tags = new TagSystem(AssemblyDirectory);
            tags.AssembleDirs();

            client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Verbose
            });

            helper = new BotHelper(client);

            client.Log += Log;
            client.JoinedGuild += JoinGuild;
            client.MessageReceived += ListenLogs;

            await client.LoginAsync(TokenType.Bot, token);
            await client.ConnectAsync();

            var map = new DependencyMap();
            map.Add(client);
            map.Add(cooldowns);
            map.Add(tags);

            handler = new CommandHandler();
            await handler.Install(map);

            await Update(client.Latency, client.Latency);
            client.LatencyUpdated += Update;

            timers[(int)TimerType.GlobalMsg] = new Timer(async s =>
            {
                var guild = client.GetGuild(guildid);
                var channel = (guild?.GetChannel(guild.DefaultChannelId)) as SocketTextChannel;
                var msg = await channel?.GetMessagesAsync(10).Flatten();
                var timeNow = DateTime.Now.ToUniversalTime();
                var diffTime = new TimeSpan(0, 3, 0, 0, 0);
                if (!msg.Any(x => x.Author.Id == client.CurrentUser.Id && (x.Timestamp.ToUniversalTime() - timeNow) > diffTime))
                {
                    await channel?.SendMessageAsync($"Hey there, I am the guide! :wave: Where are my fellow Terrarians at? Use ``{CommandHandler.prefixChar}help`` to view commands.", false);
                }
            },
            null,
            TimeSpan.FromMinutes(30),
            TimeSpan.FromMinutes(30));

            // Makes role color rainbow. Change time to 50ms to make it fast.
            // Seems to bug out (?)
            // Too many server requests, best not used.
            // Edit: I removed the Tools methods for this, keeping it in as reference.

            //timers[(int)TimerType.Rainbow] = new Timer(async s =>
            //{
            //    var guild = client.Guilds.FirstOrDefault(x => x.Id == guildid);
            //    if (guild != null)
            //    {
            //        var diff = DateTime.Now - startTime;
            //        var role = guild?.Roles.FirstOrDefault(x => x.Name.ToUpper() == "OFFICIAL BOT");
            //        var rSine = Math.Abs(Math.Sin((double)diff.Milliseconds / 1000d - 1000d));
            //        var color = Tools.HSL2RGB(rSine, 0.5, 0.5);
            //        await role.ModifyAsync(x => x.Color = new Color((byte)color.R, (byte)color.G, (byte)color.B).RawValue);
            //    }
            //},
            //null,
            //TimeSpan.FromMilliseconds(1000),
            //TimeSpan.FromMilliseconds(1000));

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
                var channel = arg.GetChannel(arg.DefaultChannelId);
                await (channel as SocketTextChannel)?.SendMessageAsync($"Unauthorized access. Terminating connection with guild.");
                await arg.LeaveAsync();
            }
        }

        private async Task Log(LogMessage e)
        {
            await Task.Yield();
            if (e.Exception != null)
            {
                Console.WriteLine($"$~{e.Exception.ToString(),-10}");
                await logger.DynamicLog(BotLogger.Type.console, "EXCEPTION!", BotLogger.Log.console);
                await logger.DynamicLog(BotLogger.Type.console, e.Exception, BotLogger.Log.exception);
                
                return;
            }
            var txt = String.Join(" $~", $"{e.Severity.ToString(), -10}", $"{e.Source.ToString(), -10}", $"{e.Message.ToString(), -10}");
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

            await client?.SetGame(game);
            await client?.SetStatus(status);
        }
    }
}
