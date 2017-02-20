using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;

namespace TheGuide
{
    public static class JsonHandler
    {
	    public static JObject modlistData;
	    public static JArray modlist;
	    public static List<string> modnames;
		private const string xmlUrl = "http://javid.ddns.net/tModLoader/listmods.php";
		private static bool needsToMaintain = false;

		private static string path = Path.Combine(Program.AssemblyDirectory, "dist");
	    private static string tagPath = Path.Combine(path, "tags");
		private static string datePath = Path.Combine(Program.AssemblyDirectory, "dist", "date.txt");
		private static string modlistPath = Path.Combine(path, "modlist.json");

		public static async Task Setup(IDiscordClient client)
	    {
			await Task.Yield();

		    Directory.CreateDirectory(path);
		    Directory.CreateDirectory(tagPath);

		    (client as DiscordSocketClient)?.Guilds.ToList()
			    .ForEach(g =>
					    Directory.CreateDirectory(Path.Combine(tagPath, $"{g.Id}")));

		    if (File.Exists(modlistPath))
		    {
			    modlistData = JObject.Parse(File.ReadAllText(modlistPath));
			    modlist = (JArray) modlistData["modlist"];
			    modnames = new List<string>();
			    modlist.ToList().ForEach(o => modnames.Add((string) (o as JObject)?.SelectToken("name")));
		    }
	    }

	    public static async Task CreateTagDir(ulong id)
	    {
		    await Task.Yield();
		    Directory.CreateDirectory(Path.Combine(tagPath, $"{id}"));
	    }

		private static async Task CreateModList(IDiscordClient client)
		{
			await Task.Yield();

		    if (needsToMaintain)
		    {
				Directory.CreateDirectory(path);
				var modData = await ReadMods(xmlUrl);
				File.WriteAllText(modlistPath, modData);
			    await Setup(client);
				File.WriteAllText(Path.Combine(path, "date.txt"), $"{DateTime.Now.ToBinary()}");
				needsToMaintain = false;
		    }
	    }

	    public static async Task MaintainContent(IDiscordClient client)
	    {
		    if (File.Exists(modlistPath) && File.Exists(datePath))
		    {
			    string date = File.ReadAllText(datePath);
			    var dateTime = DateTime.FromBinary(long.Parse(date));
			    var diff = DateTime.Now - dateTime;
			    needsToMaintain = diff.TotalDays >= 1d;
		    }
		    else
		    {
			    needsToMaintain = true;
		    }
		    await CreateModList(client);
	    }

		private static async Task<string> ReadMods(string url)
		{
			string result = "";
			string strPost = "modloaderversion=tModLoader v0.9.1.1";
			StreamWriter myWriter = null;

			HttpWebRequest objRequest = (HttpWebRequest)WebRequest.Create($"{url}");
			objRequest.Method = "POST";
			objRequest.ContentType = "application/x-www-form-urlencoded";

			try
			{
				myWriter = new StreamWriter(await objRequest.GetRequestStreamAsync());
				myWriter.Write(strPost);
			}
			catch (Exception e)
			{
				return e.Message;
			}
			finally
			{
				await myWriter.FlushAsync();
			}

			var objResponse = await objRequest.GetResponseAsync();
			using (StreamReader sr =
			   new StreamReader((objResponse as HttpWebResponse)?.GetResponseStream()))
			{
				result = sr.ReadToEnd();
			}
			return result;
		}
	}
}
