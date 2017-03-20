using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json.Linq;

namespace TheGuide.Systems
{
	public static class ModSystem
	{
		internal const string queryUrl = "http://javid.ddns.net/tModLoader/tools/querymoddownloadurl.php?modname=";
		internal const string widgetUrl = "http://javid.ddns.net/tModLoader/widget/widgetimage/";
		internal const string xmlUrl = "http://javid.ddns.net/tModLoader/listmods.php";
		internal const string homepageUrl = "http://javid.ddns.net/tModLoader/moddescription.php";
		internal const string popularUrl = "http://javid.ddns.net/tModLoader/popularmods.php";
		internal const string hotUrl = "http://javid.ddns.net/tModLoader/tools/hottop10.php";

		private static string rootDir =>
			Path.Combine(AppContext.BaseDirectory, "dist");

		private static string modDir =>
			Path.Combine(rootDir, "mods");

		public static string modPath(string modname) =>
			Path.Combine(modDir, $"{modname}.json");

		public static IEnumerable<string> mods =>
			Directory.GetFiles(modDir, "*.json")
				.Select(x => Path.GetFileNameWithoutExtension(x).RemoveWhitespace());

		/// <summary>
		/// Maintains mod data
		/// </summary>
		public static async Task Maintain(IDiscordClient client)
		{
			// Create dirs
			//Directory.CreateDirectory(rootDir); // redundant
			Directory.CreateDirectory(modDir);

			var path = Path.Combine(modDir, "date.txt");
			var dateDiff = TimeSpan.MinValue;

			// Data.txt present, read
			if (File.Exists(path))
			{
				var savedBinary = File.ReadAllText(path);
				var savedBinaryDate = Tools.DateTimeFromUnixTimestampSeconds(long.Parse(savedBinary));
				dateDiff = Tools.DateTimeFromUnixTimestampSeconds(Tools.GetCurrentUnixTimestampSeconds()) - savedBinaryDate;
			}

			// Needs to maintain data
			if (dateDiff == TimeSpan.MinValue 
				|| dateDiff.TotalDays > 1d)
			{
				var data = await DownloadData();
				var modlist = JObject.Parse(data).SelectToken("modlist").ToObject<JArray>();

				foreach (var jtoken in modlist)
				{
					var name = jtoken.SelectToken("name").ToObject<string>().RemoveWhitespace();
					File.WriteAllText(Path.Combine(modDir, $"{name}.json"), jtoken.ToString());
				}

				File.WriteAllText(path, $"{Tools.GetCurrentUnixTimestampSeconds()}");
			}
		}

		/// <summary>
		/// Will download mod json data
		/// </summary>
		private static async Task<string> DownloadData()
		{
			using (var client = new System.Net.Http.HttpClient())
			{
				// Possibly redundant, jopo mentioned just posting 0.9.2 as verison should always work, harcoding 
				//var version = await GetTMLVersion();
				var values = new Dictionary<string, string>
				{
					{"modloaderversion", "tModLoader v0.9.2" }
				};
				var content = new System.Net.Http.FormUrlEncodedContent(values);
				var response = await client.PostAsync(xmlUrl, content);
				var postResponse = await response.Content.ReadAsStringAsync();
				return postResponse;
			}
		}
		
		// Very nasty, needs something better
		private static async Task<string> GetTMLVersion()
		{
			using (var client = new System.Net.Http.HttpClient())
			{
				var response =
					await client.GetAsync(
						"https://raw.githubusercontent.com/bluemagic123/tModLoader/master/solutions/CompleteRelease.bat");
				var postResponse = await response.Content.ReadAsStringAsync();
				return postResponse.Split('\n')[3].Split('=')[1];
			}
		}
	}
}
