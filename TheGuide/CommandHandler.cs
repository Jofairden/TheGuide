using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace TheGuide
{
	public class CommandHandler
	{
		// variables
		public const int cooldownDelay = 2500; // in ms
		public const char prefixChar = '?'; // prefix char for commands
		private CommandService service;
		private DiscordSocketClient client;
		private IDependencyMap map;
		private Dictionary<ulong, DateTime> cooldowns;

		// Install dependency map
		public async Task Install(IDependencyMap _map)
		{
			service = new CommandService();

			client = _map.Get<DiscordSocketClient>();
			cooldowns = _map.Get<Dictionary<ulong, DateTime>>();
			_map.Add(service);

			map = _map;

			await service.AddModulesAsync(Assembly.GetEntryAssembly()).ConfigureAwait(false);

			client.MessageReceived += Client_MessageReceived; ;
		}

		private async Task Client_MessageReceived(SocketMessage arg)
		{
			var message = arg as SocketUserMessage;
			var context = new CommandContext(client, message);
			int argPos = 0;

			// Checks for when command should not be ran.
			if (message == null
				|| !message.Content.Except("?").Any()
				|| message.Content.Trim().Length <= 1
				|| message.Content.Trim()[1] == '?'
				|| (!(message.HasMentionPrefix(client.CurrentUser, ref argPos) || message.HasCharPrefix(prefixChar, ref argPos)))
				|| message.Author.Id == client.CurrentUser.Id)
				return;

			// Check for cooldowns
			var cooldownTime = cooldowns.FirstOrDefault(x => x.Key == message.Author.Id);
			if (cooldownTime.Key != default(ulong))
			{
				if (cooldownTime.Value > DateTime.Now) // user is on cooldown
				{
					await message?.DeleteAsync();
					return;
				}
				cooldowns.Remove(cooldownTime.Key);
			}

			// Try to execute the command
			var result = await service.ExecuteAsync(context, argPos, map);

			// Command failed to execute
			if (!result.IsSuccess)
			{
				//var channel = await context.User?.CreateDMChannelAsync();

				// Attempt to find a tag with this name
				var result2 = await service.ExecuteAsync(context, $"tag get [AttemptExecute:]{message.Content.Substring(1)}", map);
				if ((result2 as ExecuteResult?)?.Exception == null && result2.IsSuccess) // Tag was found
					return;
				else
				{
					// Tag not found

					if (result.ToString() != "UnknownCommand: Unknown command.") // We do not want to display this error
						await context.Channel.SendMessageAsync($"{Format.Bold("Error")} on ``{message.Content}``\n{result}");
				}


				// Add cooldown
				await AddCooldown(message);
			}
		}

		// Add cooldown for user
		private async Task AddCooldown(IUserMessage message)
		{
			await Task.Yield();
			cooldowns.Add(message.Author.Id, DateTime.Now.AddMilliseconds(cooldownDelay));
		}
	}
}
