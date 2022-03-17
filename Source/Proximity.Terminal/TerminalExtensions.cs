using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Proximity.Terminal;

namespace Microsoft.Extensions.Logging
{
	/// <summary>
	/// Provides logging extensions to support terminal features
	/// </summary>
	public static class TerminalExtensions
	{
		//------------------------------------------DEBUG------------------------------------------//

		/// <summary>
		/// Formats and writes an debug log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogDebug(0, exception, "Error while processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogDebugSection(this ILogger logger, EventId eventId, Exception exception, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Debug, eventId, exception, message, args);
		}

		/// <summary>
		/// Formats and writes an debug log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogDebug(0, "Processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogDebugSection(this ILogger logger, EventId eventId, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Debug, eventId, message, args);
		}

		/// <summary>
		/// Formats and writes an debug log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogDebugSection(exception, "Error while processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogDebugSection(this ILogger logger, Exception exception, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Debug, exception, message, args);
		}

		/// <summary>
		/// Formats and writes an debug log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogDebugSection("Processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogDebugSection(this ILogger logger, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Debug, message, args);
		}

		//------------------------------------------TRACE------------------------------------------//

		/// <summary>
		/// Formats and writes an trace log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogTrace(0, exception, "Error while processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogTraceSection(this ILogger logger, EventId eventId, Exception exception, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Trace, eventId, exception, message, args);
		}

		/// <summary>
		/// Formats and writes an trace log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogTrace(0, "Processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogTraceSection(this ILogger logger, EventId eventId, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Trace, eventId, message, args);
		}

		/// <summary>
		/// Formats and writes an trace log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogTraceSection(exception, "Error while processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogTraceSection(this ILogger logger, Exception exception, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Trace, exception, message, args);
		}

		/// <summary>
		/// Formats and writes an trace log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogTraceSection("Processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogTraceSection(this ILogger logger, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Trace, message, args);
		}

		//------------------------------------------INFORMATION------------------------------------------//

		/// <summary>
		/// Formats and writes an information log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogInformation(0, exception, "Error while processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogInformationSection(this ILogger logger, EventId eventId, Exception exception, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Information, eventId, exception, message, args);
		}

		/// <summary>
		/// Formats and writes an information log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogInformation(0, "Processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogInformationSection(this ILogger logger, EventId eventId, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Information, eventId, message, args);
		}

		/// <summary>
		/// Formats and writes an information log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogInformationSection(exception, "Error while processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogInformationSection(this ILogger logger, Exception exception, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Information, exception, message, args);
		}

		/// <summary>
		/// Formats and writes an information log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogInformationSection("Processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogInformationSection(this ILogger logger, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Information, message, args);
		}

		//----------------------------------------MILESTONE----------------------------------------//

		/// <summary>
		/// Formats and writes an information log message with milestone tagging.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>logger.LogMilestone(0, exception, "Error while processing request from {Address}", address)</example>
		public static void LogMilestone(this ILogger logger, EventId eventId, Exception exception, string message, params object?[] args)
		{
			using (logger.BeginScope(TerminalHighlight.Milestone))
				logger.Log(LogLevel.Information, eventId, exception, message, args);
		}

		/// <summary>
		/// Formats and writes an information log message with milestone tagging.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>logger.LogMilestone(0, "Processing request from {Address}", address)</example>
		public static void LogMilestone(this ILogger logger, EventId eventId, string message, params object?[] args)
		{
			using (logger.BeginScope(TerminalHighlight.Milestone))
				logger.Log(LogLevel.Information, eventId, message, args);
		}

		/// <summary>
		/// Formats and writes an information log message with milestone tagging.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>logger.LogMilestone(exception, "Error while processing request from {Address}", address)</example>
		public static void LogMilestone(this ILogger logger, Exception exception, string message, params object?[] args)
		{
			using (logger.BeginScope(TerminalHighlight.Milestone))
				logger.Log(LogLevel.Information, exception, message, args);
		}

		/// <summary>
		/// Formats and writes an information log message with milestone tagging.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>logger.LogMilestone("Processing request from {Address}", address)</example>
		public static void LogMilestone(this ILogger logger, string message, params object?[] args)
		{
			using (logger.BeginScope(TerminalHighlight.Milestone))
				logger.Log(LogLevel.Information, message, args);
		}

		/// <summary>
		/// Formats and writes an information log message with milestone tagging, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogMilestone(0, exception, "Error while processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogMilestoneSection(this ILogger logger, EventId eventId, Exception exception, string message, params object?[] args)
		{
			logger.LogMilestone(eventId, exception, message, args);

			return TerminalIndentLevel.Increase(logger);
		}

		/// <summary>
		/// Formats and writes an information log message with milestone tagging, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogMilestone(0, "Processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogMilestoneSection(this ILogger logger, EventId eventId, string message, params object?[] args)
		{
			logger.LogMilestone(eventId, message, args);

			return TerminalIndentLevel.Increase(logger);
		}

		/// <summary>
		/// Formats and writes an information log message with milestone tagging, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogMilestoneSection(exception, "Error while processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogMilestoneSection(this ILogger logger, Exception exception, string message, params object?[] args)
		{
			logger.LogMilestone(exception, message, args);

			return TerminalIndentLevel.Increase(logger);
		}

		/// <summary>
		/// Formats and writes an information log message with milestone tagging, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogMilestoneSection("Processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogMilestoneSection(this ILogger logger, string message, params object?[] args)
		{
			logger.LogMilestone(message, args);

			return TerminalIndentLevel.Increase(logger);
		}

		//------------------------------------------WARNING------------------------------------------//

		/// <summary>
		/// Formats and writes an warning log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogWarning(0, exception, "Error while processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogWarningSection(this ILogger logger, EventId eventId, Exception exception, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Warning, eventId, exception, message, args);
		}

		/// <summary>
		/// Formats and writes an warning log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogWarning(0, "Processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogWarningSection(this ILogger logger, EventId eventId, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Warning, eventId, message, args);
		}

		/// <summary>
		/// Formats and writes an warning log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogWarningSection(exception, "Error while processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogWarningSection(this ILogger logger, Exception exception, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Warning, exception, message, args);
		}

		/// <summary>
		/// Formats and writes an warning log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogWarningSection("Processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogWarningSection(this ILogger logger, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Warning, message, args);
		}

		//------------------------------------------ERROR------------------------------------------//

		/// <summary>
		/// Formats and writes an error log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogError(0, exception, "Error while processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogErrorSection(this ILogger logger, EventId eventId, Exception exception, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Error, eventId, exception, message, args);
		}

		/// <summary>
		/// Formats and writes an error log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogError(0, "Processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogErrorSection(this ILogger logger, EventId eventId, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Error, eventId, message, args);
		}

		/// <summary>
		/// Formats and writes an error log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogErrorSection(exception, "Error while processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogErrorSection(this ILogger logger, Exception exception, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Error, exception, message, args);
		}

		/// <summary>
		/// Formats and writes an error log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogErrorSection("Processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogErrorSection(this ILogger logger, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Error, message, args);
		}

		//------------------------------------------CRITICAL------------------------------------------//

		/// <summary>
		/// Formats and writes an critical log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogCritical(0, exception, "Error while processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogCriticalSection(this ILogger logger, EventId eventId, Exception exception, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Critical, eventId, exception, message, args);
		}

		/// <summary>
		/// Formats and writes an critical log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogCritical(0, "Processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogCriticalSection(this ILogger logger, EventId eventId, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Critical, eventId, message, args);
		}

		/// <summary>
		/// Formats and writes an critical log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogCriticalSection(exception, "Error while processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogCriticalSection(this ILogger logger, Exception exception, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Critical, exception, message, args);
		}

		/// <summary>
		/// Formats and writes an critical log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <example>using (logger.LogCriticalSection("Processing request from {Address}", address)) { }</example>
		public static TerminalIndentLevel LogCriticalSection(this ILogger logger, string message, params object?[] args)
		{
			return logger.LogSection(LogLevel.Critical, message, args);
		}

		//-----------------------------------------INDENTED SECTION-----------------------------------------//

		/// <summary>
		/// Formats and writes an information log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="logLevel">Entry will be written on this level.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="exception">The exception related to this entry.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <returns>An Indent Level that can be disposed to end the indented section.</returns>
		public static TerminalIndentLevel LogSection(this ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, string message, params object?[] args)
		{
			logger.Log(logLevel, eventId, exception, message, args);

			return TerminalIndentLevel.Increase(logger);
		}

		/// <summary>
		/// Formats and writes an information log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="logLevel">Entry will be written on this level.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <returns>An Indent Level that can be disposed to end the indented section.</returns>
		public static TerminalIndentLevel LogSection(this ILogger logger, LogLevel logLevel, EventId eventId, string message, params object?[] args)
		{
			logger.Log(logLevel, eventId, message, args);

			return TerminalIndentLevel.Increase(logger);
		}

		/// <summary>
		/// Formats and writes an information log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="logLevel">Entry will be written on this level.</param>
		/// <param name="exception">The exception related to this entry.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <returns>An Indent Level that can be disposed to end the indented section.</returns>
		public static TerminalIndentLevel LogSection(this ILogger logger, LogLevel logLevel, Exception exception, string message, params object?[] args)
		{
			logger.Log(logLevel, exception, message, args);

			return TerminalIndentLevel.Increase(logger);
		}

		/// <summary>
		/// Formats and writes an information log message, beginning an intended section.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="logLevel">Entry will be written on this level.</param>
		/// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <returns>An Indent Level that can be disposed to end the indented section.</returns>
		public static TerminalIndentLevel LogSection(this ILogger logger, LogLevel logLevel, string message, params object?[] args)
		{
			logger.Log(logLevel, message, args);

			return TerminalIndentLevel.Increase(logger);
		}

		/// <summary>
		/// Formats and writes an information log message, beginning an intended section.
		/// </summary>
		/// <typeparam name="T">The type of the object to be written.</typeparam>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="logLevel">Entry will be written on this level.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="state">The entry to be written. Can be also an object.</param>
		/// <param name="exception">The exception related to this entry.</param>
		/// <param name="formatter">Function to create a <see cref="string"/> message of the <paramref name="state"/> and <paramref name="exception"/>.</param>
		/// <returns>An Indent Level that can be disposed to end the indented section.</returns>
		public static TerminalIndentLevel LogSection<T>(this ILogger logger, LogLevel logLevel, EventId eventId, T state, Exception exception, Func<T, Exception, string> formatter)
		{
			logger.Log(logLevel, eventId, state, exception, formatter);

			return TerminalIndentLevel.Increase(logger);
		}
	}
}
