using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Security.Cryptography;
//using System.Drawing;

namespace TheGuide
{
    public static class Tools
    {
        public static Random Rand = new Random();

        public static string GetUptime() => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");
        public static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString();

        public static string TruncateString(this string str, int maxLength)
        {
            return str.Substring(0, Math.Min(str.Length, maxLength));
        }

        //public static IEnumerable<T> GetRow<T>(T[,] array, int index)
        //{
        //    for (int i = 0; i < array.GetLength(1); i++)
        //    {
        //        yield return array[index, i];
        //    }
        //}

        //public static int ComputeReliableHash(this string input)
        //{
        //    //make a reliable has code from GUID md5 computed has
        //    using (MD5 md5 = MD5.Create())
        //    {
        //        byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        //        return hash.GetHashCode();
        //    }
        //}

        //public static char RandomLetter()
        //{
        //    int num = rand.Next(0, 26);
        //    char let = (char)('a' + num);
        //    return let;
        //}

        //public static IEnumerable<int> MakeKeys(this string keyword, int row_length)
        //{
        //    Random random = new Random(keyword.ComputeReliableHash());
        //    var keys = new List<int>();
        //    var exclude = new HashSet<int>();

        //    //generate random set of column order
        //    while (keys.Count < row_length)
        //    {
        //        int rnd = random.Next(1, row_length);
        //        var range = Enumerable.Range(0, row_length).Where(i => !exclude.Contains(i));
        //        int index = random.Next(0, row_length - exclude.Count);
        //        int element = range.ElementAt(index);
        //        keys.Add(element);
        //        exclude.Add(element);
        //    }

        //    return keys as IEnumerable<int>;
        //}
    }
}
