using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TheGuide
{
    public class BotLogger
    {
        private static string rootDir;
        //                       name           path              date     path
        internal static Dictionary<string, Tuple<string, Dictionary<string, string>>> _paths;
        //                                      path to folder             path to date folder
        // ie: logs/console/23-10-2016

        private static string[] lognames = new string[]
        {
            "console.log", "exception.log", "writer_exception.log", "moderation.log", "server.log"
        };

        private static string[] names = new string[]
        {
            "logs", "console", "modbot"
        };

        public enum Type
        {
            logs,
            console,
            modbot
        }

        public enum Log
        {
            console,
            exception,
            writer_exception,
            moderation,
            server
        }

        private class NameHelper
        {
            public static string GetName(Type type)
            {
                return names[(int)type];
            }

            public static string GetLogName(Log type)
            {
                return lognames[(int)type];
            }
        }

        private class PathHelper
        {
            public static string GetRootPath(string type)
            {
                return _paths[type].Item1;
            }

            public static Dictionary<string, string> GetDict(string type)
            {
                return _paths[type].Item2;
            }

            public static void QuickAdd(string type)
            {
                var log_type = NameHelper.GetName(Type.logs);
                var path = type == log_type ? Path.Combine(rootDir, log_type) : Path.Combine(rootDir, log_type, type);
                _paths.Add(type, new Tuple<string, Dictionary<string, string>>(path, new Dictionary<string, string>()));
            }

            public static void AddDate(string type, string date)
            {
                _paths[type].Item2.Add(date, Path.Combine(_paths[type].Item1, date));
            }
        }

        private static bool hasAssembled;
        private static object locker;

        public BotLogger(string _rootDir)
        {
            rootDir = _rootDir;
            _paths = new Dictionary<string, Tuple<string, Dictionary<string, string>>>();

            foreach (var item in names)
            {
                PathHelper.QuickAdd(item);
            }

            hasAssembled = false;
            locker = new object();
        }

        private string AssembleLine(string message)
        {
            return string.Concat($"[{DateTime.Now.ToUniversalTime()} UCT] ~ $ ", $"[{message}]");
        }

        private string GetDateFormat()
        {
            return DateTime.Now.ToString("dd-MM-yyyy");
        }

        public async Task DynamicLog(Type type, object message, Log log)
        {
            var log_type = NameHelper.GetName(type);
            string msg = "";
            switch (log)
            {
                case Log.exception:
                case Log.writer_exception:
                    msg = (message as Exception)?.ToString();
                    break;
                default:
                    msg = (message as string)?.ToString();
                    break;
            }
            await DynamicDictLog(PathHelper.GetDict(log_type), msg, NameHelper.GetLogName(log));
        }

        private async Task DynamicDictLog(Dictionary<string, string> dict, string message, string fileName)
        {
            await Task.Yield();
            string dateValue;
            if (dict.TryGetValue(GetDateFormat(), out dateValue))
            {
                string path = Path.Combine(PathHelper.GetRootPath(NameHelper.GetName(Type.logs)), dateValue);
                path = Path.Combine(path, fileName);
                await _DynamicLog(path, AssembleLine(message));
            }
        }

        private async Task _DynamicLog(string path, string message)
        {
            try
            {
                lock (locker)
                {
                    using (FileStream file = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (StreamWriter writer = new StreamWriter(file, Encoding.Unicode))
                    {
                        writer.WriteLine(message);
                    }
                }
            }
            catch (Exception e)
            {
                await DynamicLog(Type.console, e.ToString(), Log.writer_exception);
            }
        }

        public void AssembleDirs()
        {
            try
            {
                string dateFormat = GetDateFormat();

                var values = Enum.GetValues(typeof(Type)).Cast<Type>();
                foreach (var item in values)
                {
                    var name = NameHelper.GetName(item);
                    var info = Directory.CreateDirectory(PathHelper.GetRootPath(name));
                    if (name != NameHelper.GetName(Type.logs))
                    {
                        info.CreateSubdirectory(dateFormat);
                        PathHelper.AddDate(name, dateFormat);
                    }
                }
                hasAssembled = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
