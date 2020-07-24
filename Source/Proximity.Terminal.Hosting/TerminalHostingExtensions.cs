using System;
using Microsoft.Extensions.Hosting;

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

			builder.ConfigureServices((context, services) =>
			{
				var Registry = TerminalRegistry.Global;

				if (configureRegistry == null)
				{
					Registry.ScanLoaded();
					Registry.ScanOnLoad();
				}
				else
				{
					configureRegistry(Registry);
				}
			});

			return builder;
		}
	}
}
