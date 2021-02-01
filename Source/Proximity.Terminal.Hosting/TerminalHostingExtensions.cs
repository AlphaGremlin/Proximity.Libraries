using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Proximity.Terminal.Hosting;

namespace Proximity.Terminal
{
	/// <summary>
	/// Provides hosting extensions for the Terminal
	/// </summary>
	public static class TerminalHostingExtensions
	{
		/// <summary>
		/// Initialises the Global Terminal Registry
		/// </summary>
		/// <param name="builder">The Host Builder to attach to</param>
		/// <param name="configureRegistry">A callback to configure the registry. If null, automatically scans loaded and future assemblies</param>
		/// <returns>The Host Builder</returns>
		public static IHostBuilder UseTerminal(this IHostBuilder builder, Action<TerminalRegistry>? configureRegistry = null)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));

			if (configureRegistry == null)
				return builder.ConfigureServices(UseDefaultTerminal);

			return builder.ConfigureServices((context, services) =>
			{
				var Registry = TerminalRegistry.Global;

				services.AddSingleton(Registry);

				configureRegistry(Registry);
			});
		}

		/// <summary>
		/// Initialises the Global Terminal Registry
		/// </summary>
		/// <param name="builder">The Host Builder to attach to</param>
		/// <param name="configureRegistry">A callback to configure the registry. If null, automatically scans loaded and future assemblies</param>
		/// <returns>The Host Builder</returns>
		public static IHostBuilder UseTerminal(this IHostBuilder builder, Action<TerminalRegistry, IServiceProvider> configureRegistry)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));

			if (configureRegistry == null)
				return builder.ConfigureServices(UseDefaultTerminal);

			return builder.ConfigureServices((context, services) =>
			{
				services.AddSingleton(TerminalRegistry.Global);
				services.AddHostedService(provider => new TerminalHostService(provider, configureRegistry));
			});
		}

		//****************************************

		private static void UseDefaultTerminal(HostBuilderContext context, IServiceCollection services)
		{
			var Registry = TerminalRegistry.Global;

			services.AddSingleton(Registry);

			Registry.ScanLoaded();
			Registry.ScanOnLoad();
		}
	}
}
