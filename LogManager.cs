using System;
using System.IO;
using System.Threading.Tasks;

namespace TheGuide
{
	internal sealed class LogManager
	{
		private static readonly string BaseDir = Path.Combine(AppContext.BaseDirectory, "dist");
		private static readonly string LogDir = Path.Combine(BaseDir, "logs");
		private string LogPath => Path.Combine(LogDir, LogFilePath());
		private readonly string _suffix;
		private readonly string _prefix;
		private readonly object _locker = new object();
		private DateTime CurrentDate() => DateTime.Now;
		private string LogFilePath() => $"{_prefix ?? ""}{CurrentDate():dd-MM-yyyy}{_suffix ?? ""}.txt";

		public LogManager(string prefix = null, string suffix = null)
		{
			this._prefix = prefix;
			this._suffix = suffix;
		}

		private Task Maintain()
		{
			Directory.CreateDirectory(LogDir);
			if (!File.Exists(LogPath))
				File.Create(LogPath).Dispose();
			return Task.CompletedTask;
		}

		public async Task Write(string logMessage)
		{
			await Maintain();
			FileAppend(_locker, LogPath, logMessage + "\r\n");
		}

		public static string FileReadToEnd(object locker, string filePath)
		{
			string buffer;
			lock (locker)
			{
				using (var stream = File.Open(filePath, FileMode.Open))
				using (var reader = new StreamReader(stream))
					buffer = reader.ReadToEnd();
			}
			return buffer;
		}

		public static void FileWrite(object locker, string path, string content)
		{
			lock (locker)
			{
				using (var stream = File.Open(path, FileMode.Create))
				using (var writer = new StreamWriter(stream))
					writer.Write(content);
			}
		}

		public static void FileWriteLine(object locker, string path, string content)
		{
			lock (locker)
			{
				using (var stream = File.Open(path, FileMode.Create))
				using (var writer = new StreamWriter(stream))
					writer.WriteLine(content);
			}
		}

		public static void FileAppend(object locker, string path, string content)
		{
			lock (locker)
			{
				using (var stream = File.Open(path, FileMode.Append))
				using (var writer = new StreamWriter(stream))
					writer.Write(content);
			}
		}

		public static void FileAppendLine(object locker, string path, string content)
		{
			lock (locker)
			{
				using (var stream = File.Open(path, FileMode.Append))
				using (var writer = new StreamWriter(stream))
					writer.WriteLine(content);
			}
		}
	}
}

