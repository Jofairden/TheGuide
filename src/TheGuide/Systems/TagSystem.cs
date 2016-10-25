using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheGuide.Systems
{
    public class TagSystem
    {
        public class TagJson
        {
            public string name;
            public string output;
        }

        private static DirectoryInfo dirInfo;
        private static string rootDir;
        private static bool hasAssembled;
        private static object locker;

        internal static Dictionary<string, string> data;

        public TagSystem(string _rootDir)
        {
            rootDir = _rootDir;
            data = new Dictionary<string, string> ();
            hasAssembled = false;
            locker = new object();
        }

        public void AssembleDirs()
        {
            try
            {
                dirInfo = Directory.CreateDirectory(Path.Combine(rootDir, "tags"));
                _doLoadTags();
                hasAssembled = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void CreateTag(string name, TagJson input)
        {
            if (dirInfo != null)
            {
                string path = Path.Combine(dirInfo.FullName, name);
                string filePath = Path.Combine(path, "tag.json");
                Directory.CreateDirectory(path);
                string json = JsonConvert.SerializeObject(input);
                lock (locker)
                {
                    File.WriteAllText(filePath, json);
                }
                _saveTag(filePath);
            }
        }

        public string ListTags()
        {
            if (data.Any())
            {
                StringBuilder builder = new StringBuilder();
                foreach (var item in data)
                {
                    builder.Append(item.Key).Append(", ");
                }
                string msg = Tools.TruncateString(builder.ToString(), builder.ToString().Length - 2);
                return msg;
            }
            return "no tags found";
        }

        public string GetTag(string name)
        {
            return data[name];
        }

        public bool HasTag(string name)
        {
            return data.ContainsKey(name);
        }

        public bool DeleteTag(string name)
        {
            return _deleteTag(name);
        }

        private void _doLoadTags()
        {
            if (dirInfo != null)
            {
                data.Clear();
                foreach (var dir in dirInfo.GetDirectories())
                {
                    foreach (var file in dir.GetFiles("tag.json", SearchOption.TopDirectoryOnly))
                    {
                        _saveTag(file.FullName);
                    }
                }
            }
        }

        private void _saveTag(string path)
        {
            var json = LoadJson(path);
            if (json != null)
            {
                data[json.name] = json.output;
            }
        }

        private bool _deleteTag(string name)
        {
            if (data.ContainsKey(name))
            {
                data.Remove(name);
                string path = Path.Combine(dirInfo.FullName, name);
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
            lock (locker)
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
