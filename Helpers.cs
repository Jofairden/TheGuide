using System;
using System.Collections.Generic;
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
		}

	}
}
