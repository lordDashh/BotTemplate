using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Interactivity;

using Newtonsoft.Json;

using BotTemplate.Managers;
using BotTemplate.Objects.Json;

namespace BotTemplate {
	public class Bot {
		public DiscordClient Client { get; private set; }
		public BotConfig Config { get; private set; }
		public Database Db { get; private set; }
		public CancellationTokenSource CancellationTokenSource { get; private set; }

		private void Initialize() {
			/* Your config file */
			this.Config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("./Resources/config.json"));

			this.Client = new DiscordClient(new DiscordConfiguration() {
					Token = this.Config.Token,
					TokenType = TokenType.Bot,

					UseInternalLogHandler = true
				});
		}

		private void PostInitialize() {
			this.Client.Ready += (e) => {
				this.Client.DebugLogger.LogMessage(LogLevel.Info, nameof(this.Client), "Ready", DateTime.Now);
				return Task.CompletedTask;
			};

			this.CancellationTokenSource = new CancellationTokenSource();

			AppDomain.CurrentDomain.ProcessExit += (s, e)
				=> this.CancellationTokenSource.Cancel();

			Console.CancelKeyPress += (s, e)
				=> this.CancellationTokenSource.Cancel();

			/* This way you can access the Bot object without globals */
			var deps = new ServiceCollection()
				.AddSingleton<Bot>(f => this)
				.BuildServiceProvider();
			var cnext = this.Client.UseCommandsNext(new CommandsNextConfiguration() {
					StringPrefixes = this.Config.Prefixes,
					CaseSensitive = false,
					IgnoreExtraArguments = true,
					Services = deps
			});

			/* Basic command handling */
			cnext.CommandErrored += async (e) => {
				var et = e.Exception.GetType();
				if (et == typeof(CommandNotFoundException))
					return;
				if (et == typeof(UserException)) {
					await e.Context.RespondAsync(e.Exception.Message);
				} else {
					if (et == typeof(ArgumentException)
						&& e.Exception.Message == "Could not find a suitable overload for the command.") {
						await e.Context.RespondAsync($"Invalid syntax for `{e.Command.QualifiedName}`. See `{e.Context.Prefix}help {e.Command.QualifiedName}` for info on that.");
						return;
					}
					if (e.Exception.Message == "No matching subcommands were found, and this group is not executable.") {
						await e.Context.RespondAsync($"You either spelled the command wrong or it doesn't exist in specified group.");
						return;
					}
					await e.Context.RespondAsync($"error: ```{e.Exception}```");
				}
			};

			cnext.RegisterCommands(System.Reflection.Assembly.GetExecutingAssembly());

			this.Client.UseInteractivity(new InteractivityConfiguration() {
					Timeout = TimeSpan.FromHours(2)
				});

			/* Put Db initialization code here */
			this.Db = new Database("./Resources/database.db");
			NoteManager.Initialize(this.Db);
		}

		public async Task RunAsync() {
			this.Initialize();
			this.PostInitialize();

			try {
				await this.Client.ConnectAsync();
				await Task.Delay(-1, this.CancellationTokenSource.Token);
			} catch (TaskCanceledException) {
			} catch (Exception ex) {
				this.Client.DebugLogger.LogMessage(LogLevel.Critical, nameof(Bot), "Exception in main loop.", DateTime.Now,
												   ex);
			}

			this.Client.DebugLogger.LogMessage(LogLevel.Info, nameof(Bot), "Disconnecting...", DateTime.Now);
			await this.Client.DisconnectAsync();
		}
	}
}
