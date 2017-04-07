using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TheGuide
{
	// Our ConfigProperties class
	// Opt in is important, it makes us have to define which properties we want to include
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	internal sealed class ConfigProperties
	{
		// A token is always required
		[JsonProperty(Required = Required.Always, PropertyName = "token")]
		public string Token { get; set; }
		// A version is not always required, but shouldn't be null either
		[JsonProperty(Required = Required.DisallowNull, PropertyName = "version")]
		public string Version { get; set; }

		private string _configPath { get; set; }
		public string ConfigPath => Path.Combine(AppContext.BaseDirectory, _configPath);

		[JsonConstructor]
		public ConfigProperties(string token = null, string version = null, string configName = null)
		{
			Token = token ?? string.Empty;
			Version = version ?? string.Empty;
			_configPath = configName ?? "config.json";
		}

		// Quick method for parsing
		public static ConfigProperties Parse(string value) =>
			JsonConvert.DeserializeObject<ConfigProperties>(value);
		// Quick method for serializing
		public string Serialize() =>
			JsonConvert.SerializeObject(this, Formatting.Indented);
	}

	internal static class ConfigManager
	{
		internal static ConfigProperties Properties = new ConfigProperties();

		public static Task Read()
		{
			// Throw an exception if our config file does not exist
			if (!File.Exists(Properties.ConfigPath))
				throw new FileNotFoundException($"File {Properties.ConfigPath} not found.");

			// Read our config json
			var json = File.ReadAllText(Properties.ConfigPath);
			// Parse read json
			var data = ConfigProperties.Parse(json);

			// Set values
			Properties.Token = data.Token;
			Properties.Version = data.Version;

			return Task.CompletedTask;
		}

		// Try to write new config data, you shouldn't really use this, just here for completion's sake
		public static Task Write()
		{
			var data = Properties.Serialize();
			File.WriteAllText(Properties.ConfigPath, data);
			return Task.CompletedTask;
		}
	}
}
