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
	[TerminalProvider]
	public static class Program
	{ //****************************************
		private static readonly CancellationTokenSource Exited = new CancellationTokenSource();
		//****************************************

		public static async Task<int> Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("Select Base Implementation: S - Serilog");

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
			switch (char.ToUpperInvariant(code))
			{
			case 'S':
				Console.WriteLine("Using Serilog Intermediary");

				return MainSerilog();

			default:
				Console.WriteLine("Unknown Implementation: {0}", code);
				return Task.FromResult(-1);
			}
		}

		private static async Task<int> MainSerilog()
		{
			try
			{
				TerminalConsole.Initialise(true);

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
	}
}
