using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proximity.Terminal.Hosting;

namespace Proximity.Terminal
{
	/// <summary>
	/// Provides hosting extensions for the Terminal
	/// </summary>
	public static class TerminalHostingExtensions
	{
		public static ILoggingBuilder AddTerminal(this ILoggingBuilder builder, Action<TerminalRegistry>? configureRegistry = null) => builder.AddTerminal(TerminalConsole.View!, configureRegistry);

		public static ILoggingBuilder AddTerminal(this ILoggingBuilder builder, ITerminal terminal, Action<TerminalRegistry>? configureRegistry = null)
		{
			if (terminal == null)
				throw new ArgumentNullException(nameof(terminal), "Terminal is not initialised");

			builder.Services.AddTerminal(terminal, configureRegistry);

			return builder.AddProvider(new TerminalLogProvider(terminal));
		}

		public static ILoggingBuilder AddTerminal(this ILoggingBuilder builder, Action<TerminalRegistry, IServiceProvider> configureRegistry) => builder.AddTerminal(TerminalConsole.View!, configureRegistry);

		public static ILoggingBuilder AddTerminal(this ILoggingBuilder builder, ITerminal terminal, Action<TerminalRegistry, IServiceProvider> configureRegistry)
		{
			if (terminal == null)
				throw new ArgumentNullException(nameof(terminal), "Terminal is not initialised");

			builder.Services.AddTerminal(terminal, configureRegistry);

			return builder.AddProvider(new TerminalLogProvider(terminal));
		}

		public static IServiceCollection AddTerminal(this IServiceCollection services, Action<TerminalRegistry>? configureRegistry = null) => services.AddTerminal(TerminalConsole.View!, configureRegistry);

		public static IServiceCollection AddTerminal(this IServiceCollection services, ITerminal terminal, Action<TerminalRegistry>? configureRegistry = null)
		{
			if (terminal == null)
				throw new ArgumentNullException(nameof(terminal), "Terminal is not initialised");

			var Registry = TerminalRegistry.Global;

			services.AddSingleton(Registry);

			if (configureRegistry == null)
			{
				Registry.ScanLoaded();
				Registry.ScanOnLoad();
			}
			else
			{
				configureRegistry(Registry);
			}

			return services.AddSingleton(terminal);
		}

		public static IServiceCollection AddTerminal(this IServiceCollection services, Action<TerminalRegistry, IServiceProvider> configureRegistry) => services.AddTerminal(TerminalConsole.View!, configureRegistry);

		public static IServiceCollection AddTerminal(this IServiceCollection services, ITerminal terminal, Action<TerminalRegistry, IServiceProvider> configureRegistry)
		{
			if (terminal == null)
				throw new ArgumentNullException(nameof(terminal), "Terminal is not initialised");

			if (configureRegistry == null)
				throw new ArgumentNullException(nameof(configureRegistry));

			var Registry = TerminalRegistry.Global;

			return services
				.AddHostedService(provider => new TerminalHostService(provider, configureRegistry))
				.AddSingleton(terminal);
		}
	}
}
