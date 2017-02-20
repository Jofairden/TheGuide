using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace TheGuide
{
    public static class Tools
    {
		public static Random Rand = new Random();

		public static string GetUptime() => 
			(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");

		public static string GetHeapSize() => 
			Math.Round(GC.GetTotalMemory(true) / (1024.0d * 1024.0d), 2).ToString(CultureInfo.InvariantCulture);

		public static string Truncate(this string value, int length) => 
			value?.Substring(0, Math.Min(value.Length, value.Length - length));

		public static string PrintRoles(this IEnumerable<SocketRole> roles) => 
			roles.ToList().Select(r => r.Name).PrettyPrint();

	    public static string PrettyPrint(this IEnumerable<string> list)
	    {
			var sb = new StringBuilder();
		    list.ToList().ForEach(s => sb.Append($"``{s}``, "));
			return $"{sb.ToString().Truncate(2)}";
		}

		public static string Cap(this string value, int length) => 
			value?.Substring(0, Math.Abs(Math.Min(value.Length, length)));

		public static bool Contains(this string source, string toCheck, StringComparison comp) =>
			source.IndexOf(toCheck, comp) >= 0;

		public static string RemoveWhitespace(this string input) =>
			new string(input.ToCharArray()
				.Where(c => !char.IsWhiteSpace(c))
				.ToArray());

	    public static string ReplaceWhitespace(this string input, string replacement) =>
		    input.Replace(" ", replacement);

		public static List<T> GetAllPublicConstantValues<T>(this Type type) =>
			type
				.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
				.Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(T))
				.Select(x => (T)x.GetRawConstantValue())
				.ToList();

		public static List<string> GetAllPublicConstantNames<T>(this Type type) =>
			type
				.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
				.Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(T))
				.Select(field => field.Name)
				.ToList();

		public static Dictionary<string, T> GetAllPublicConstants<T>(this Type type) =>
			type
				.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
				.Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(T))
				.AsEnumerable()
				.Select(fi => new { ConstName = fi.Name, ConstValue = (T)fi.GetRawConstantValue() })
				.ToDictionary(x => x.ConstName, x => x.ConstValue);

		public static string AddSpacesToSentence(this string text) =>
			string.Concat(text.Select(x => char.IsUpper(x) ? $" {x}" : x.ToString())).TrimStart(' ');
	}
}
