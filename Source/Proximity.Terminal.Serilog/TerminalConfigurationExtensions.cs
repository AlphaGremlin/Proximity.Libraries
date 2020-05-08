using System;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Proximity.Terminal.Serilog
{
	/// <summary>
	/// Adds the WriteTo.Terminal() extension method
	/// </summary>
	public static class TerminalConfigurationExtensions
	{
		internal const string DefaultTerminalOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

		/// <summary>
		/// Attaches a Serilog event sink that writes to a Terminal View
		/// </summary>
		/// <param name="sinkConfiguration"></param>
		/// <param name="view"></param>
		/// <param name="restrictedToMinimumLevel"></param>
		/// <param name="outputTemplate"></param>
		/// <param name="formatProvider"></param>
		/// <param name="levelSwitch"></param>
		/// <returns></returns>
		public static LoggerConfiguration Terminal(
			this LoggerSinkConfiguration sinkConfiguration,
			ITerminal terminal,
			LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
			string outputTemplate = DefaultTerminalOutputTemplate,
			IFormatProvider? formatProvider = null,
			LoggingLevelSwitch? levelSwitch = null
			)
		{
			var Formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

			return sinkConfiguration.Sink(new TerminalSink(view, Formatter), restrictedToMinimumLevel, levelSwitch);
		}
	}
}
