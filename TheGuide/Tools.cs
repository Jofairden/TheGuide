using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace TheGuide
{
	// Some helpful methods etc.
	public static class Tools
	{
		public static Random Rand = new Random();

		public static bool ICEquals(this string source, string comparison) =>
			string.Equals(source, comparison, StringComparison.OrdinalIgnoreCase);

		public static bool AreSorted<T>(IEnumerable<T> ids)
		{
			var enumerable = ids as T[] ?? ids.ToArray();
			return enumerable.SequenceEqual(enumerable.OrderBy(id => id));
		}

		public static bool AreUnique<T>(IEnumerable<T> ids)
		{
			var enumerable = ids as T[] ?? ids.ToArray();
			return enumerable.Distinct().Count() == enumerable.Count();
		}

		public static IEnumerable<string> ChunksUpto(this string str, int maxChunkSize)
		{
			for (int i = 0; i < str.Length; i += maxChunkSize)
				yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
		}

		public static string FirstCharToUpper(this string input)
		{
			if (string.IsNullOrEmpty(input))
				throw new ArgumentNullException();
			return input.First().ToString().ToUpper() + input.Substring(1);
		}

		public static string ToJson<T>(this T any, Formatting formatting = Formatting.Indented) =>
			JsonConvert.SerializeObject(any, formatting);

		public static int KiB(this int value) => value * 1024;
		public static int KB(this int value) => value * 1000;

		public static int MiB(this int value) => value.KiB() * 1024;
		public static int MB(this int value) => value.KB() * 1000;

		public static int GiB(this int value) => value.MiB() * 1024;
		public static int GB(this int value) => value.MB() * 1000;

		public static ulong KiB(this ulong value) => value * 1024;
		public static ulong KB(this ulong value) => value * 1000;

		public static ulong MiB(this ulong value) => value.KiB() * 1024;
		public static ulong MB(this ulong value) => value.KB() * 1000;

		public static ulong GiB(this ulong value) => value.MiB() * 1024;
		public static ulong GB(this ulong value) => value.MB() * 1000;

		public static string Unmention(this string str) => 
			str.Replace("@​everyone", "@every\x200Bone").Replace("@here", "@he\x200Bre");

		public static string GenFullName(string username, string discriminator) =>
			$"{username}#{discriminator}";

		public static string GenFullName(string username, ulong discriminator) =>
			$"{username}#{discriminator}";

		public static string GenFullName(this IUser user) =>
			GenFullName(user.Username, user.Discriminator);

		public static string GetUptime() =>
			(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");

		public static string GetHeapSize() =>
			Math.Round(GC.GetTotalMemory(true) / (1024.0d * 1024.0d), 2).ToString(CultureInfo.InvariantCulture);

		public static string Truncate(this string value, int length) =>
			value?.Substring(0, Math.Min(value.Length, value.Length - length));

		public static string PrintRoles(this IEnumerable<SocketRole> roles) =>
			roles.ToList().Select(r => r.Name).PrettyPrint();

		public static string PrettyPrint(this IEnumerable<string> list) =>
			string.Join(", ", list.Select(v => $"``{v}``"));

		public static string SurroundWith(this string text, string surrounder) =>
			$"{surrounder}{text}{surrounder}";

		public static string Cap(this string value, int length) =>
			value?.Substring(0, Math.Abs(Math.Min(value.Length, length)));

		public static bool Contains(this string source, string toCheck, StringComparison comp) =>
			source.IndexOf(toCheck, comp) >= 0;

		public static string RemoveWhitespace(this string input) =>
			new string(input.ToCharArray()
				.Where(c => !char.IsWhiteSpace(c))
				.ToArray());

		private static readonly DateTime UnixEpoch =
			new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static long GetCurrentUnixTimestampMillis() =>
			(long)GetCurrentUnixTimespan().TotalMilliseconds;

		public static DateTime DateTimeFromUnixTimestampMillis(long millis) =>
			UnixEpoch.AddMilliseconds(millis);

		public static long GetCurrentUnixTimestampSeconds() =>
			(long)GetCurrentUnixTimespan().TotalSeconds;

		public static TimeSpan GetCurrentUnixTimespan() =>
			DateTime.UtcNow - UnixEpoch;

		public static DateTime DateTimeFromUnixTimestampSeconds(long seconds) =>
			UnixEpoch.AddSeconds(seconds);

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

		public static string Uncapitalize(this string text, int index = 0) =>
			new string(text.Select((c, i) => (i == index) ? c : char.ToLower(c)).ToArray());

		private static readonly Regex filterRegex = new Regex(@"(?:discord(?:\.gg|.me|app\.com\/invite)\/(?<id>([\w]{16}|(?:[\w]+-?){3})))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public static bool IsDiscordInvite(this string str)
			=> filterRegex.IsMatch(str);

		public static string RealAvatarUrl(this IUser usr)
		{
			return usr.AvatarId.StartsWith("a_")
				? $"{DiscordConfig.CDNUrl}avatars/{usr.Id}/{usr.AvatarId}.gif"
				: usr.AvatarId;
		}

		public static int LevenshteinDistance(this string s, string t)
		{
			var n = s.Length;
			var m = t.Length;
			var d = new int[n + 1, m + 1];

			// Step 1
			if (n == 0)
			{
				return m;
			}

			if (m == 0)
			{
				return n;
			}

			// Step 2
			for (var i = 0; i <= n; d[i, 0] = i++)
			{
			}

			for (var j = 0; j <= m; d[0, j] = j++)
			{
			}

			// Step 3
			for (var i = 1; i <= n; i++)
			{
				//Step 4
				for (var j = 1; j <= m; j++)
				{
					// Step 5
					var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

					// Step 6
					d[i, j] = Math.Min(
						Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
						d[i - 1, j - 1] + cost);
				}
			}
			// Step 7
			return d[n, m];
		}

		//unused
		/// <summary>
		/// returns an IEnumerable with randomized element order
		/// </summary>
		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> items)
		{
			using (var provider = RandomNumberGenerator.Create())
			{
				var list = items.ToList();
				var n = list.Count;
				while (n > 1)
				{
					var box = new byte[(n / byte.MaxValue) + 1];
					int boxSum;
					do
					{
						provider.GetBytes(box);
						boxSum = box.Sum(b => b);
					}
					while (!(boxSum < n * ((byte.MaxValue * box.Length) / n)));
					var k = (boxSum % n);
					n--;
					var value = list[k];
					list[k] = list[n];
					list[n] = value;
				}
				return list;
			}
		}

		//unused
		public static IMessage DeleteAfter(this IUserMessage msg, int ms)
		{
			Task.Run(async () =>
			{
				await Task.Delay(ms);
				try { await msg.DeleteAsync().ConfigureAwait(false); }
				catch
				{
					// ignored
				}
			});
			return msg;
		}
	}
}
