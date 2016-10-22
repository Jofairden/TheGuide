using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Threading;

namespace TheGuide
{
    // (c) Gorateron - 2016 - For Discord: tModLoader
    public class Program
    {
        public static void Main(string[] args)
        {
            new Program().Start().GetAwaiter().GetResult();
        }

        public static string version = "r-1.0.3.1";

        private static ulong guildid = 103110554649894912;
        private static ulong clientid = 239075803290271745;
        private static ulong permissions = 536345663;
        private DiscordSocketClient client;
        private CommandHandler handler;
        private string token
        {
            get { return "FUCKHERRIGHTINTHEPUSSY"; }
        }
        private static Timer[] timers = new Timer[Enum.GetNames(typeof(TimerType)).Length];
        private Dictionary<ulong, DateTime> cooldowns = new Dictionary<ulong, DateTime>();

        enum TimerType
        {
            GlobalMsg
        }

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
            Console.Title = $"TheGuide - Discord Bot - By: Gorateron - {version}";
            Console.InputEncoding = System.Text.Encoding.UTF8;

            //https://discordapp.com/oauth2/authorize?client_id=239075803290271745&scope=bot&permissions=536345663
            await Console.Out.WriteLineAsync($"https://discordapp.com/oauth2/authorize?client_id={clientid}&scope=bot&permissions={permissions}");
            await Console.Out.WriteLineAsync($"Start date: {DateTime.Now}");

            client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Debug
            });

            client.Log += Log;


            await client.LoginAsync(TokenType.Bot, token);
            await client.ConnectAsync();

            client.LatencyUpdated += Update;

            var map = new DependencyMap();
            map.Add(client);
            map.Add(cooldowns);

            handler = new CommandHandler();
            await handler.Install(map);

            await Update(client.Latency, client.Latency);

            timers[(int)TimerType.GlobalMsg] = new Timer(async s =>
            {
                var guild = client.Guilds.FirstOrDefault(x => x.Id == guildid);
                if (guild != null)
                {
                    var channel = guild?.GetChannel(guild.DefaultChannelId);
                    await (channel as SocketTextChannel)?.SendMessageAsync($"Hey there, I am the guide! :wave: I am here to be useful. I am made by gorateron, use ``{CommandHandler.prefixChar}help`` to view commands.", false);
                }
            },
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(30));

            await Task.Delay(-1);
        }

        private Task Log(LogMessage e)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            var txt = String.Join("] [", $"{e.Severity.ToString(), -10}", $"{e.Source.ToString(), -10}", $"{e.Message.ToString(), -75}");
            Console.Write($"[{txt}]");
            Console.WriteLine();
            if (e.Exception != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{e.ToString(),-10}]");
            }
            return Task.CompletedTask;
        }

        private async Task Update(int i, int j)
        {
            var status = UserStatus.Online;
            if (client.ConnectionState == ConnectionState.Connecting || j > 1250 || (i - j) >= 500)
            {
                status = UserStatus.Idle;
            }
            else if (client.ConnectionState == ConnectionState.Disconnected || j > 2500 || (i - j) >= 1000)
            {
                status = UserStatus.DoNotDisturb;
            }
            await client.CurrentUser.ModifyStatusAsync(x =>
            {
                x.Status = status;
                x.Game = new Discord.API.Game() { Name = "Terraria" };
            });

            //await client.SetGame("Terraria");
            //await client.SetStatus(status);
        }
    }
}
