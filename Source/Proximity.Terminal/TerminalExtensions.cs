using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Proximity.Terminal
{
	public static class TerminalExtensions
	{
		/// <summary>
		/// Formats and writes a debug log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
		public static void LogMilestone(this ILogger logger, EventId eventId, Exception exception, string message, params object[] args)
		{
			using var _ = logger.BeginScope(TerminalScope.Milestone);

			logger.Log(LogLevel.Information, eventId, exception, message, args);
		}

		/// <summary>
		/// Formats and writes a debug log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>logger.LogDebug(0, "Processing request from {Address}", address)</example>
		public static void LogMilestone(this ILogger logger, EventId eventId, string message, params object[] args)
		{
			using var _ = logger.BeginScope(TerminalScope.Milestone);

			logger.Log(LogLevel.Information, eventId, message, args);
		}

		/// <summary>
		/// Formats and writes a debug log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
		public static void LogMilestone(this ILogger logger, Exception exception, string message, params object[] args)
		{
			using var _ = logger.BeginScope(TerminalScope.Milestone);

			logger.Log(LogLevel.Information, exception, message, args);
		}

		/// <summary>
		/// Formats and writes a debug log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>logger.LogDebug("Processing request from {Address}", address)</example>
		public static void LogMilestone(this ILogger logger, string message, params object[] args)
		{
			using var _ = logger.BeginScope(TerminalScope.Milestone);

			logger.Log(LogLevel.Information, message, args);
		}
	}
}
