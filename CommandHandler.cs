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
		public const int cooldownDelay = 2500;
		public const int deleteDelay = 5000;
		public const char prefixChar = '?';
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

			if ((Program.maintenanceMode && context.Guild.Id != 216276491544166401)
				|| message == null
				|| !message.Content.Except("?").Any()
				|| message.Content.Trim().Length <= 1
				|| message.Content.Trim()[1] == '?'
				|| (!(message.HasMentionPrefix(client.CurrentUser, ref argPos) || message.HasCharPrefix(prefixChar, ref argPos)))
				|| message.Author.Id == client.CurrentUser.Id)
				return;

			var cooldownTime = cooldowns.FirstOrDefault(x => x.Key == message.Author.Id);
			if (cooldownTime.Key != default(ulong))
			{
				if (cooldownTime.Value > DateTime.Now)
				{
					await message?.DeleteAsync();
					return;
				}
				else
					cooldowns.Remove(cooldownTime.Key);
			}

			var result = await service.ExecuteAsync(context, argPos, map);

			if (!result.IsSuccess)
			{
				//var channel = await context.User?.CreateDMChannelAsync();

				// Attempt to find a tag with this name
				var result2 = await service.ExecuteAsync(context, $"tag get [blankAttempt]:{message.Content.Substring(1)}", map);
				if ((result2 as ExecuteResult?)?.Exception == null && result2.IsSuccess)
					return;


				if (result.ToString() != "UnknownCommand: Unknown command.")
					await context.Channel.SendMessageAsync($"**Warning** on ``{message.Content}``\n{result}");

				await AddCooldown(message);
			}
		}

		private async Task AddCooldown(IUserMessage message)
		{
			await Task.Yield();
			cooldowns.Add(message.Author.Id, DateTime.Now.AddMilliseconds(cooldownDelay));
			//string[] opt = await SplitOpt(message.ToString());
			if (message.Content.EndsWith("-d"))
				await message.DeleteAsync();
		}

		// old
		//private static async Task<string[]> SplitOpt(string opt)
		//{
		//	await Task.Yield();
		//	string[] opts = opt.Split('-');
		//	opts = opts.Where(x => !string.IsNullOrEmpty(x)).ToArray();
		//	return opts;
		//}
	}
}
