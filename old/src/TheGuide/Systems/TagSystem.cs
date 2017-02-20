using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using TheGuide.Modules;

namespace TheGuide.Systems
{
    public class TagSystem
    {
        public class TagJson
        {
            public string Name;
            public string Output;
        }

        private static DirectoryInfo _dirInfo;
        private static string _rootDir;
        private static bool _hasAssembled;
        private static object _locker;

        internal static Dictionary<ulong, Dictionary<string, string>> Data;

        public TagSystem(string _rootDir)
        {
            TagSystem._rootDir = _rootDir;
	        Data = new Dictionary<ulong, Dictionary<string, string>>();
            _hasAssembled = false;
            _locker = new object();
        }

        public async Task AssembleDirs(DiscordSocketClient client)
        {
            try
            {
                _dirInfo = Directory.CreateDirectory(Path.Combine(_rootDir, "tags"));
	            await client.WaitForGuildsAsync();
	            foreach (SocketGuild guild in client.Guilds)
	            {
		            Data.Add(guild.Id, new Dictionary<string, string>());
		            Directory.CreateDirectory(Path.Combine(_dirInfo.FullName, guild.Name));
	            }
                _doLoadTags(client);
                _hasAssembled = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void CreateTag(string name, TagJson input, IGuild guild)
        {
            if (_dirInfo != null)
            {
                string path = Path.Combine(_dirInfo.FullName, guild.Name, name);
                string filePath = Path.Combine(path, "tag.json");
                Directory.CreateDirectory(path);
                string json = JsonConvert.SerializeObject(input);
                lock (_locker)
                {
                    File.WriteAllText(filePath, json);
                }
                _saveTag(guild.Id, filePath);
            }
        }

		public string ListTags()
        {
            if (Data.Any())
            {
                StringBuilder builder = new StringBuilder();
                foreach (var item in Data)
                {
	                foreach (var kvp in item.Value)
	                {
						builder.Append(kvp.Value).Append(", ");
					}
				}
                string msg = builder.ToString().TruncateString(builder.ToString().Length - 2);
                return msg;
            }
            return "no tags found";
        }

	    internal async Task<bool> AttemptExecute(CommandService service, IDependencyMap map, CommandContext context, string name)
	    {
			string data = GetTag(context.Guild.Id, name);
			// command:github jquery in:name,description
			if (!data.StartsWith("command:"))
				return false;

			//var newContext = new DummyContext(context.Client, context.Guild, context.Channel, context.User);
			await service.ExecuteAsync(context, $"{data.Substring(8)}", map, MultiMatchHandling.Best);

			return true;
	    }

		public class DummyContext : ICommandContext
		{
			public DummyContext(IDiscordClient client, IGuild guild, IMessageChannel channel, IUser user, IUserMessage message)
			{
				Client = client;
				Guild = guild;
				Channel = channel;
				User = user;
				Message = message;
			}
			public IDiscordClient Client { get; }
			public IGuild Guild { get; }
			public IMessageChannel Channel { get; }
			public IUser User { get; }
			public IUserMessage Message { get; }
		}

		public string GetTag(ulong guildid, string name)
        {
            return Data.FirstOrDefault(x => x.Key == guildid).Value[name];
        }

        public bool HasTag(ulong guildid, string name)
        {
            return Data.FirstOrDefault(x => x.Key == guildid).Value.ContainsKey(name);
        }

        public bool DeleteTag(IGuild guild, string name)
        {
            return _deleteTag(guild, name);
        }

        private void _doLoadTags(IDiscordClient client)
        {
            if (_dirInfo != null)
            {
                Data.Clear();

	            foreach (SocketGuild guild in (client as DiscordSocketClient)?.Guilds)
	            {
		            string path = Path.Combine(_dirInfo.FullName, guild.Name);
					if (!Directory.Exists(path))
						return;

		            var info = new DirectoryInfo(path);

					foreach (var dir in info.GetDirectories())
					{
						foreach (var file in dir.GetFiles($"tag.json", SearchOption.TopDirectoryOnly))
						{
							_saveTag(guild.Id, file.FullName);
						}
					}
				}
            }
        }

        private void _saveTag(ulong guildid, string path)
        {
            var json = LoadJson(path);
            if (json != null)
            {
				Data.FirstOrDefault(x => x.Key == guildid).Value[json.Name] = json.Output;
            }
        }

        private bool _deleteTag(IGuild guild, string name)
        {
            if (Data.FirstOrDefault(x => x.Key == guild.Id).Value.ContainsKey(name))
            {
				Data.FirstOrDefault(x => x.Key == guild.Id).Value.Remove(name);
                string path = Path.Combine(_dirInfo.FullName, guild.Name, name);
                string filePath = Path.Combine(path, "tag.json");
                File.Delete(filePath);
                Directory.Delete(path);
                return true;
            }
            return false;
        }

        private TagJson LoadJson(string file)
        {
            TagJson objects;
            lock (_locker)
            {
                using (StreamReader r = new StreamReader(File.OpenRead(file)))
                {
                    string json = r.ReadToEnd();
                    objects = JsonConvert.DeserializeObject<TagJson>(json);
                }
            }
            return objects;
        }

        //public void Test()
        //{
        //    foreach (var item in data)
        //    {
        //        Console.WriteLine(item.Key);
        //        Console.WriteLine(item.Value);
        //    }
        //}
    }
}
