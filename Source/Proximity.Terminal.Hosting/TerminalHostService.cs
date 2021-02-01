using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Proximity.Terminal.Hosting
{
	internal sealed class TerminalHostService : IHostedService
	{ //****************************************
		private readonly IServiceProvider _Provider;
		private readonly Action<TerminalRegistry, IServiceProvider> _ConfigureRegistry;
		//****************************************

		public TerminalHostService(IServiceProvider provider, Action<TerminalRegistry, IServiceProvider> configureRegistry)
		{
			_Provider = provider;
			_ConfigureRegistry = configureRegistry;
		}

		//****************************************

		Task IHostedService.StartAsync(CancellationToken cancellationToken)
		{
			var Registry = _Provider.GetRequiredService<TerminalRegistry>();

			_ConfigureRegistry(Registry, _Provider);

			return Task.CompletedTask;
		}

		Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
