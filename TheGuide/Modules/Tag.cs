using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using TheGuide.Preconditions;
using TheGuide.Systems;

namespace TheGuide.Modules
{
	[Group("tag")]
	[Name("tag")]
	public class Tag : ModuleBase
	{
		private readonly CommandService service;
		private readonly IDependencyMap map;

		public Tag(CommandService service, IDependencyMap map)
		{
			this.service = service;
			this.map = map;
		}

		
	}

}
