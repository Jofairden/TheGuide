using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace TheGuide
{
	public class CommandHandler
	{
		public static char CharPrefix = '?';
		public CommandService Service;
		private DiscordSocketClient _client;
		private IDependencyMap _map;

		public async Task Install(IDependencyMap map)
		{
			// Create Command Service, inject it into Dependency Map
			_client = map.Get<DiscordSocketClient>();
			_map = map;
			// Creating a CommandServiceConfig is far from required here, just added it for completion's sake
			Service =
				new CommandService(new CommandServiceConfig
				{
					CaseSensitiveCommands = false,
					DefaultRunMode = RunMode.Async,
					LogLevel = LogSeverity.Verbose
				});
			Service.Log += Service_Log; 

			// Finds modules in our assembly and adds them to our command service
			await Service.AddModulesAsync(Assembly.GetEntryAssembly());

			_client.MessageReceived += HandleCommand;
		}

		private Task Service_Log(LogMessage arg)
		{
			if (arg.Exception != null)
			{
				Console.WriteLine(arg.Exception.ToString());
			}
			return Task.CompletedTask;
		}

		public async Task HandleCommand(SocketMessage parameterMessage)
		{
			// Don't handle the command if it is a system message
			var message = parameterMessage as SocketUserMessage;

			// Mark where the prefix ends and the command begins
			int argPos = 0;
			// Determine if the message has a valid prefix, adjust argPos 
			if (message == null
				|| message.Author is IWebhookUser
				|| message.Author.IsBot
				|| !(message.HasMentionPrefix(_client.CurrentUser, ref argPos)
				|| message.HasCharPrefix(CharPrefix, ref argPos)))
				return;

			// Create a Socket Command Context
			var context = new SocketCommandContext(_client, message);
			// Execute the Command, store the result
			var result = await Service.ExecuteAsync(context, argPos, _map, MultiMatchHandling.Exception);

			// If the command failed, notify the user
			if (!result.IsSuccess)
				await message.Channel.SendMessageAsync($"{Format.Bold("Error:")} {result.ErrorReason}");
		}
	}
}
