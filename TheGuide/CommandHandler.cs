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
		private CommandService _service;
		private DiscordSocketClient _client;
		private IDependencyMap _map;
		private Dictionary<ulong, DateTime> _cooldowns;

		// Install dependency map
		public async Task Install(DiscordSocketClient client, IDependencyMap map)
		{
			_service = new CommandService();

			_client = client;
			_map = map;
			_cooldowns = map.Get<Dictionary<ulong, DateTime>>();
			// redundant as of 00642
			//_map.Add(service);

			await _service.AddModulesAsync(Assembly.GetEntryAssembly());

			_client.MessageReceived += Client_MessageReceived;
		}

		private async Task Client_MessageReceived(SocketMessage arg)
		{
			var message = arg as SocketUserMessage;
			var context = new CommandContext(_client, message);
			int argPos = 0;

			// Checks for when command should not be ran.
			if (arg.Author.IsBot
				|| arg.IsWebhook
				|| message == null
				|| !message.Content.Except("?").Any()
				|| message.Content.Trim().Length <= 1
				|| message.Content.Trim()[1] == '?'
				|| (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix(prefixChar, ref argPos)))
				// Sort of redundant now, as we check IsBot
				|| message.Author.Id == _client.CurrentUser.Id)
				return;

			// server specific commands do not work in dm channels
			if (message.Channel is SocketDMChannel)
			{
				await message.Channel.SendMessageAsync($"Please use my commands in a server.");
				return;
			}

			// Check for cooldowns
			var cooldownTime = _cooldowns.FirstOrDefault(x => x.Key == message.Author.Id);
			if (cooldownTime.Key != default(ulong))
			{
				if (cooldownTime.Value > DateTime.Now) // user is on cooldown
				{
					await message.DeleteAsync();
					return;
				}
				_cooldowns.Remove(cooldownTime.Key);
			}

			// Try to execute the command
			var result = await _service.ExecuteAsync(context, argPos, _map);

			// Command failed to execute
			if (!result.IsSuccess)
			{
				//var channel = await context.User?.CreateDMChannelAsync();

				// Attempt to find a tag with this name
				var result2 = await _service.ExecuteAsync(context, $"tag get [AttemptExecute:]{message.Content.Substring(1)}", _map);
				if ((result2 as ExecuteResult?)?.Exception == null
					&& result2.IsSuccess) // Tag was found
					return;
				else
				{
					// Tag not found

					if (result.ToString() == "BadArgCount: The input text has too few parameters.")
						await _service.ExecuteAsync(context, $"help {message.Content.Substring(1)}", _map);
					else if (result.ToString() != "UnknownCommand: Unknown command.") // We do not want to display this error
						await context.Channel.SendMessageAsync($"{Format.Bold("Error")} on ``{message.Content}``\n{result}");
				}

				// Add cooldown
				AddCooldown(message);
			}
		}

		// Add cooldown for user
		private void AddCooldown(IMessage message) =>
			_cooldowns.Add(message.Author.Id, DateTime.Now.AddMilliseconds(cooldownDelay));
	}
}
