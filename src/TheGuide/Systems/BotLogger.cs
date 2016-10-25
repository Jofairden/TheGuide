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

namespace TheGuide.Systems
{
    public class BotLogger
    {
        private static string rootDir;
        //                       name           path              date     path
        internal static Dictionary<string, Tuple<string, Dictionary<string, string>>> paths;
        //                                      path to folder             path to date folder
        // ie: logs/console/23-10-2016

        private static string[] logtypeNames = new string[]
        {
            "console.log", "exception.log", "writer_exception.log", "moderation.log", "server.log"
        };

        private static string[] typeNames = new string[]
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

        private class DateHelper
        {
            public static string GetLogDate()
            {
                return DateTime.Now.ToUniversalTime().ToString() + " UCT";
            }

            public static string GetDate()
            {
                return DateTime.Now.ToString("dd-MM-yyyy");
            }
        }

        private class NameHelper
        {
            public static string GetName(Type type)
            {
                return typeNames[(int)type];
            }

            public static string GetLogName(Log type)
            {
                return logtypeNames[(int)type];
            }
        }

        private class PathHelper
        {
            public static string GetRootPath(string type)
            {
                return paths[type].Item1;
            }

            public static Dictionary<string, string> GetDict(string type)
            {
                return paths[type].Item2;
            }

            public static void QuickAdd(string type)
            {
                string logType = NameHelper.GetName(Type.logs);
                string path = type == logType ? Path.Combine(rootDir, logType) : Path.Combine(rootDir, logType, type);
                paths.Add(type, new Tuple<string, Dictionary<string, string>>(path, new Dictionary<string, string>()));
            }

            public static void AddDate(string type, string date)
            {
                paths[type].Item2.Add(date, Path.Combine(paths[type].Item1, date));
            }
        }

        private static bool hasAssembled;
        private static object locker;

        public BotLogger(string _rootDir)
        {
            rootDir = _rootDir;
            paths = new Dictionary<string, Tuple<string, Dictionary<string, string>>>();

            foreach (var item in typeNames)
            {
                PathHelper.QuickAdd(item);
            }

            hasAssembled = false;
            locker = new object();
        }

        private string AssembleLine(string message)
        {
            return $"[{DateHelper.GetLogDate()} ~ $ [{message}]";
        }

        public async Task DynamicLog(Type type, object message, Log log)
        {
            var log_type = NameHelper.GetName(type);
            string msg;
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
            if (msg.Where(x => !Char.IsWhiteSpace(x)).ToString().Length > 0)
                await _log(PathHelper.GetDict(log_type), msg, NameHelper.GetLogName(log));
        }

        private async Task _log(Dictionary<string, string> dict, string message, string fileName)
        {
            await Task.Yield();
            string dateValue;
            if (dict.TryGetValue(DateHelper.GetDate(), out dateValue))
            {
                string path = Path.Combine(PathHelper.GetRootPath(NameHelper.GetName(Type.logs)), dateValue);
                path = Path.Combine(path, fileName);
                await _doLog(path, AssembleLine(message));
            }
        }

        // should be merged into _log
        private async Task _doLog(string path, string message)
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
                string date = DateHelper.GetDate();

                var values = Enum.GetValues(typeof(Type)).Cast<Type>();
                foreach (var item in values)
                {
                    var name = NameHelper.GetName(item);
                    var info = Directory.CreateDirectory(PathHelper.GetRootPath(name));
                    if (name != NameHelper.GetName(Type.logs))
                    {
                        info.CreateSubdirectory(date);
                        PathHelper.AddDate(name, date);
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
