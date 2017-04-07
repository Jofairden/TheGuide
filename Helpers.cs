using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Discord;

namespace TheGuide
{
	public static class Helpers
	{
		public static class Colors
		{
			public static Color Red => new Color(1f, 0f, 0f);
			public static Color Green => new Color(0f, 1f, 0f);
			public static Color Blue => new Color(0f, 0f, 1f);
			public static Color Yellow => new Color(1f, 1f, 0f);
			public static Color SoftRed => new Color((byte)242, 152, 140);
			public static Color SoftGreen => new Color((byte)184, 242, 140);
			public static Color SoftYellow => new Color((byte)242, 235, 140);

			public static Color GetLatencyColor(float latency) =>
				latency >= 500
					? SoftRed
					: latency >= 250
						? SoftYellow
						: SoftGreen;
		}

		public static string Truncate(this string value, int length) =>
			value?.Substring(0, Math.Min(value.Length, value.Length - length));

		public static string PrettyPrint(this IEnumerable<string> list) =>
			string.Join(", ", list.Select(v => $"`{v}`"));

		public static string RemoveWhitespace(this string input) =>
			new string(input.ToCharArray()
				.Where(c => !char.IsWhiteSpace(c))
				.ToArray());

		public static string Cap(this string value, int length) =>
			value?.Substring(0, Math.Abs(Math.Min(value.Length, length)));

		public static bool ICEquals(this string source, string comparison) =>
			string.Equals(source, comparison, StringComparison.OrdinalIgnoreCase);

		public static bool ICStartsWith(this string source, string comparison) =>
			source.StartsWith(comparison, StringComparison.OrdinalIgnoreCase);

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

		public static void Swap<T>(ref T arg1, ref T arg2)
		{
			var temp = arg1;
			arg1 = arg2;
			arg2 = temp;
		}

		/// <summary>
		/// Computes the Damerau-Levenshtein Distance between two strings, represented as arrays of
		/// integers, where each integer represents the code point of a character in the source string.
		/// Includes an optional threshhold which can be used to indicate the maximum allowable distance.
		/// </summary>
		/// <param name="source">An array of the code points of the first string</param>
		/// <param name="target">An array of the code points of the second string</param>
		/// <param name="threshold">Maximum allowable distance</param>
		/// <returns>Int.MaxValue if threshhold exceeded; otherwise the Damerau-Leveshteim distance between the strings</returns>
		public static int DamerauLevenshteinDistance(string source, string target, int threshold)
		{
			int length1 = source.Length,
				length2 = target.Length;

			// Return trivial case - difference in string lengths exceeds threshhold
			if (Math.Abs(length1 - length2) > threshold) { return int.MaxValue; }

			// Ensure arrays [i] / length1 use shorter length 
			if (length1 > length2)
			{
				Swap(ref target, ref source);
				Swap(ref length1, ref length2);
			}

			int maxi = length1,
				maxj = length2;

			int[] dCurrent = new int[maxi + 1],
				  dMinus1 = new int[maxi + 1],
				  dMinus2 = new int[maxi + 1];

			for (var i = 0; i <= maxi; i++) { dCurrent[i] = i; }

			int jm1 = 0;

			for (var j = 1; j <= maxj; j++)
			{

				// Rotate
				var dSwap = dMinus2;
				dMinus2 = dMinus1;
				dMinus1 = dCurrent;
				dCurrent = dSwap;

				// Initialize
				var minDistance = int.MaxValue;
				dCurrent[0] = j;
				int im1 = 0,
					im2 = -1;

				for (var i = 1; i <= maxi; i++)
				{
					var cost = source[im1] == target[jm1] ? 0 : 1;

					int del = dCurrent[im1] + 1,
						ins = dMinus1[i] + 1,
						sub = dMinus1[im1] + cost;

					//Fastest execution for min value of 3 integers
					var min = del > ins ? (ins > sub ? sub : ins) : (del > sub ? sub : del);

					if (i > 1 && j > 1 && source[im2] == target[jm1] && source[im1] == target[j - 2])
						min = Math.Min(min, dMinus2[im2] + cost);

					dCurrent[i] = min;
					if (min < minDistance) { minDistance = min; }
					im1++;
					im2++;
				}
				jm1++;
				if (minDistance > threshold) { return int.MaxValue; }
			}

			var result = dCurrent[maxi];
			return result > threshold ? int.MaxValue : result;
		}
	}
}
