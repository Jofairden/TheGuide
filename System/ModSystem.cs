using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TheGuide.System
{
    public class ModSystem
    {
		// helper urls
		internal const string QueryDownloadUrl = "http://javid.ddns.net/tModLoader/tools/querymoddownloadurl.php?modname=";
		internal const string QueryHomepageUrl = "http://javid.ddns.net/tModLoader/tools/querymodhomepage.php?modname=";
		internal const string WidgetUrl = "http://javid.ddns.net/tModLoader/widget/widgetimage/";
		internal const string XmlUrl = "http://javid.ddns.net/tModLoader/listmods.php";
		internal const string ModInfoUrl = "http://javid.ddns.net/tModLoader/tools/modinfo.php";
		internal const string HomepageUrl = "http://javid.ddns.net/tModLoader/moddescription.php";
		internal const string PopularUrl = "http://javid.ddns.net/tModLoader/tools/populartop10.php";
		internal const string HotUrl = "http://javid.ddns.net/tModLoader/tools/hottop10.php";

		// paths
	    internal string RootPath =>
		    Path.Combine(AppContext.BaseDirectory, "dist");

	    internal string ModPath =>
		    Path.Combine(RootPath, "mods");

	    internal string ModJsonPath(string mod) =>
		    Path.Combine(ModPath, $"{mod}.json");

		// mods
	    internal IEnumerable<string> ModFiles;
		   

		// maintains mod data
	    public async Task Maintain(IDiscordClient client)
	    {
		    Directory.CreateDirectory(ModPath);

		    var datePath = Path.Combine(Path.Combine(ModPath, "date.txt"));
		    var dateDiff = TimeSpan.Zero;

		    if (File.Exists(datePath))
		    {
				var dateBinary = await Helpers.ReadTextAsync(datePath);
				var parsedBinary = long.Parse(dateBinary);
				var savedBinary = Helpers.DateTimeFromUnixTimestampSeconds(parsedBinary);
				dateDiff = Helpers.DateTimeFromUnixTimestampSeconds(Helpers.GetCurrentUnixTimestampSeconds()) - savedBinary;
		    }

		    if (dateDiff == TimeSpan.Zero
		        || dateDiff.TotalHours >= 8)
		    {
			    var modData = await DownloadData();
			    var modList = JObject.Parse(modData).SelectToken("modlist").ToObject<JArray>();

				foreach (var jToken in modList)
				{
					var modName = jToken.Value<string>("name").RemoveWhitespace();
					var jsonPath = Path.Combine(ModPath, $"{modName}.json");
					var jsonData = jToken.ToString(Formatting.Indented);
					await Helpers.WriteTextAsync(jsonPath, jsonData);
				}

			    await Helpers.WriteTextAsync(datePath, Helpers.GetCurrentUnixTimestampSeconds().ToString());
		    }

			ModFiles = Directory.GetFiles(ModPath, "*.json")
				.Select(x => Path.GetFileNameWithoutExtension(x).RemoveWhitespace());
	    }

		// tries to cache a single mod by name
		public async Task<bool> TryCacheMod(string name)
		{
			var data = await DownloadSingleData(name);
			if (data.StartsWith("Failed:"))
				return false;

			var modData = JObject.Parse(data);
			var modName = modData.Value<string>("name");
			var jsonPath = Path.Combine(ModPath, $"{modName}.json");
			var jsonData = modData.ToString(Formatting.Indented);
			await Helpers.WriteTextAsync(jsonPath, jsonData);
			ModFiles = Directory.GetFiles(ModPath, "*.json")
				.Select(x => Path.GetFileNameWithoutExtension(x).RemoveWhitespace());
			return true;
		}

		// downloads mod json data
		private async Task<string> DownloadData()
		{
			using (var client = new HttpClient())
			{
				// Possibly redundant, jopo mentioned just posting 0.9.2 as verison should always work, harcoding 
				//var version = await GetTMLVersion();
				var values = new Dictionary<string, string>
				{
					{"modloaderversion", "tModLoader v0.9.2" }
				};
				var content = new FormUrlEncodedContent(values);
				var response = await client.PostAsync(XmlUrl, content);
				var postResponse = await response.Content.ReadAsStringAsync();
				return postResponse;
			}
		}

		// downloads single mod json data
		private static async Task<string> DownloadSingleData(string name)
		{
			using (var client = new HttpClient())
			{
				var response = await client.GetAsync(ModInfoUrl + $"?modname={name}");
				var postResponse = await response.Content.ReadAsStringAsync();
				return postResponse;
			}
		}
	}
}
