using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TheGuide.Systems.Helpers
{
    public static class AsyncHelper
    {
		public static Task CreateFileAsync(string path)
		{
			if (File.Exists(path))
				return Task.FromResult(true);

			var tcs = new TaskCompletionSource<bool>();
			FileSystemWatcher watcher = new FileSystemWatcher(Path.GetDirectoryName(path));

			FileSystemEventHandler createdHandler = null;
			RenamedEventHandler renamedHandler = null;
			createdHandler = (s, e) =>
			{
				if (e.Name == Path.GetFileName(path))
				{
					tcs.TrySetResult(true);
					watcher.Created -= createdHandler;
					watcher.Dispose();
				}
			};

			renamedHandler = (s, e) =>
			{
				if (e.Name == Path.GetFileName(path))
				{
					tcs.TrySetResult(true);
					watcher.Renamed -= renamedHandler;
					watcher.Dispose();
				}
			};

			watcher.Created += createdHandler;
			watcher.Renamed += renamedHandler;

			watcher.EnableRaisingEvents = true;

			return tcs.Task;
		}

		public static async Task CopyDirsAsync(DirectoryInfo StartDirectory, DirectoryInfo EndDirectory)
		{
			foreach (DirectoryInfo dirInfo in StartDirectory.GetDirectories("*", SearchOption.AllDirectories))
			{
				string dirPath = dirInfo.FullName;
				string outputPath = dirPath.Replace(StartDirectory.FullName, EndDirectory.FullName);
				Directory.CreateDirectory(outputPath);

				foreach (FileInfo file in dirInfo.EnumerateFiles())
				{
					using (FileStream SourceStream = file.OpenRead())
					{
						using (FileStream DestinationStream = File.Create(outputPath + file.Name))
						{
							await SourceStream.CopyToAsync(DestinationStream);
						}
					}
				}
			}
		}
	}
}
