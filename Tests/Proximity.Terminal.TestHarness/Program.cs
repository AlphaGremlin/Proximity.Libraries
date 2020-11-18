using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Proximity.Terminal.TestHarness
{
	[TerminalProvider(true)]
	public sealed class Program : IHostedService
	{ //****************************************
		private static readonly CancellationTokenSource Exited = new CancellationTokenSource();

		private readonly ILogger<Program> _Logger;
		//****************************************

		public Program(TerminalRegistry registry, ILogger<Program> logger)
		{
			registry.Add(this);

			_Logger = logger;
		}

		//****************************************

		public static async Task<int> Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("Select Base Implementation: S - Serilog, s - Serilog (no Command Line)");

				var Key = Console.ReadKey(true);

				return await MainSelector(Key.KeyChar);
			}
			else
			{
				return await MainSelector(args[0][0]);
			}
		}

		//****************************************

		private static Task<int> MainSelector(char code)
		{
			switch (code)
			{
			case 's':
				Console.WriteLine("Using Serilog Intermediary (no Command Line)");

				return MainSerilog(false);

			case 'S':
				Console.WriteLine("Using Serilog Intermediary");

				return MainSerilog(true);

			default:
				Console.WriteLine("Unknown Implementation: {0}", code);
				return Task.FromResult(-1);
			}
		}

		private static async Task<int> MainSerilog(bool commandLine)
		{
			try
			{
				TerminalConsole.Initialise(commandLine);

				var builder = new HostBuilder()
					.ConfigureAppConfiguration((hostContext, config) =>
					{
						config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

						config.AddEnvironmentVariables();
					})
					.ConfigureLogging((hostContext, logging) =>
					{
						var LoggerConfig = new LoggerConfiguration()
							.MinimumLevel.Is(LogEventLevel.Verbose)
							.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
							.MinimumLevel.Override("System", LogEventLevel.Warning)
							.Enrich.FromLogContext()
							.WriteTo.Terminal(TerminalConsole.View) // All log entries route to the console
							;

						Log.Logger = LoggerConfig.CreateLogger();

						logging.AddSerilog();
					})
					.ConfigureServices((hostContext, services) =>
					{
						var Configuration = hostContext.Configuration;

						services.AddSingleton<ITerminal>(TerminalConsole.View);
						services.AddSingleton<Program>();
						services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<Program>());
					})
					.UseSerilog()
					.UseTerminal(registry =>
					{
						// Load the Program type and Standard Commands
						registry.Scan(typeof(Program));
						registry.Scan(typeof(StandardCommands));
					})
					.UseConsoleLifetime()
					;

				var Host = builder.Build();

				await Host.RunAsync(Exited.Token);

				return 0;
			}
			catch (OperationCanceledException)
			{
				return 0;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				Log.Logger.Fatal(e, "Host terminated unexpectedly");

				return -1;
			}
			finally
			{
				Log.CloseAndFlush();

				TerminalConsole.Terminate();
			}
		}

		//****************************************


		Task IHostedService.StartAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		Task IHostedService.StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		//****************************************

		[TerminalBinding("Exit", "Exits the application")]
		public static void Quit()
		{
			Exited.Cancel();
		}

		[TerminalBinding("Performs a full garbage collection")]
		public static void Cleanup()
		{
			GC.Collect();
		}

		//****************************************

		[TerminalBinding("Writes some spam to the log")]
		public void Spam()
		{
			var Random = new Random();
			Span<byte> RandomData = stackalloc byte[8];

			for (var Index = 0; Index < 10; Index++)
			{
				Random.NextBytes(RandomData);

				_Logger.LogInformation(Convert.ToBase64String(RandomData));
			}
		}
	}
}
