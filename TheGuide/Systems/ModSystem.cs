using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TheGuide.Systems
{

	public static class ModSystem
    {
		internal const string queryUrl = "http://javid.ddns.net/tModLoader/tools/querymoddownloadurl.php?modname=";
		internal const string widgetUrl = "http://javid.ddns.net/tModLoader/widget/widgetimage/";
		internal const string xmlUrl = "http://javid.ddns.net/tModLoader/listmods.php";

		private static string rootDir =>
			Path.Combine(Program.AssemblyDirectory, "dist");

	    private static string modDir =>
		    Path.Combine(rootDir, "mods");

	    public static string modPath(string modname) =>
		    Path.Combine(modDir, $"{modname}.json");

	    public static IEnumerable<string> mods =>
		    Directory.GetFiles(modDir, "*.json")
			    .Select(Path.GetFileNameWithoutExtension);

		/// <summary>
		/// Maintains mod data
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public static async Task Maintain(IDiscordClient client)
	    {
		    await Task.Run(async () =>
			{
				// Create dirs
				Directory.CreateDirectory(rootDir);
				Directory.CreateDirectory(modDir);

				var path = Path.Combine(modDir, "date.txt");
				TimeSpan dateDiff = TimeSpan.MinValue;

				// Data.txt present, read
				if (File.Exists(path))
				{
					var savedBinary = File.ReadAllText(path);
					var savedBinaryDate = DateTime.FromBinary(long.Parse(savedBinary));
					dateDiff = DateTime.Now - savedBinaryDate;
				}

				// Needs to maintain data
				if (dateDiff == TimeSpan.MinValue || dateDiff.Days > 1)
				{
					var data = await DownloadData();
					var json = JObject.Parse(data);
					var modlist = (JArray) json.SelectToken("modlist");

					foreach (var jtoken in modlist)
					{
						var name = jtoken.SelectToken("name").ToObject<string>();
						File.WriteAllText(Path.Combine(modDir, $"{name}.json"), jtoken.ToString());
					}

					if (mods.Count() == modlist.Count)
						File.WriteAllText(path, $"{DateTime.Now.ToBinary()}");
				}
			});
	    }

		/// <summary>
		/// Will download mod json data
		/// </summary>
		/// <returns></returns>
	    private static async Task<string> DownloadData()
		{
			string postResponse = string.Empty;
		    using (var client = new System.Net.Http.HttpClient())
		    {
			    var values = new Dictionary<string, string>
			    {
					{"modloaderversion", "tModLoader v0.9.1.1" }
			    };
			    var content = new System.Net.Http.FormUrlEncodedContent(values);
			    var response = await client.PostAsync(xmlUrl, content);
			    postResponse = await response.Content.ReadAsStringAsync();
		    }
			return postResponse;
		}
	}
}
