using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using Proximity.Utility.Logging;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Writes entries to the Default Logging Infrastructure
	/// </summary>
	public static class Log
	{
		/// <summary>
		/// Writes an entry at debugging level
		/// </summary>
		/// <param name="text">The text to write</param>
		public static void Debug(string text) => LogManager.Default.Logger.Debug(text);

		/// <summary>
		/// Writes a formatted entry at debugging level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public static void Debug(string text, params object[] args) => LogManager.Default.Logger.Debug(text, args);

		//****************************************

		/// <summary>
		/// Writes an entry at the verbose level
		/// </summary>
		/// <param name="text">The text to write</param>
		public static void Verbose(string text) => LogManager.Default.Logger.Verbose(text);

		/// <summary>
		/// Writes a formatted entry at the verbose level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public static void Verbose(string text, params object[] args) => LogManager.Default.Logger.Verbose(text, args);

		/// <summary>
		/// Begins a new section at the verbose level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public static LogSection VerboseSection(string text) => LogManager.Default.Logger.VerboseSection(text);

		/// <summary>
		/// Begins a new section at the verbose level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public static LogSection VerboseSection(string text, params object[] args) => LogManager.Default.Logger.VerboseSection(text, args);

		//****************************************

		/// <summary>
		/// Writes an entry at the information level
		/// </summary>
		/// <param name="text">The text to write</param>
		public static void Info(string text) => LogManager.Default.Logger.Info(text);

		/// <summary>
		/// Writes a formatted entry at the information level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public static void Info(string text, params object[] args) => LogManager.Default.Logger.Info(text, args);

		/// <summary>
		/// Begins a new section at the information level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public static LogSection InfoSection(string text) => LogManager.Default.Logger.InfoSection(text);

		/// <summary>
		/// Begins a new section at the information level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public static LogSection InfoSection(string text, params object[] args) => LogManager.Default.Logger.InfoSection(text, args);

		//****************************************

		/// <summary>
		/// Writes an entry at the milestone level
		/// </summary>
		/// <param name="text">The text to write</param>
		public static void Milestone(string text) => LogManager.Default.Logger.Milestone(text);

		/// <summary>
		/// Writes a formatted entry at the milestone level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public static void Milestone(string text, params object[] args) => LogManager.Default.Logger.Milestone(text, args);

		/// <summary>
		/// Begins a new section at the milestone level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public static LogSection MilestoneSection(string text) => LogManager.Default.Logger.MilestoneSection(text);

		/// <summary>
		/// Begins a new section at the milestone level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public static LogSection MilestoneSection(string text, params object[] args) => LogManager.Default.Logger.MilestoneSection(text, args);

		//****************************************

		/// <summary>
		/// Writes an entry at the warning level
		/// </summary>
		/// <param name="text">The text to write</param>
		public static void Warning(string text) => LogManager.Default.Logger.Warning(text);

		/// <summary>
		/// Writes a formatted entry at the warning level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public static void Warning(string text, params object[] args) => LogManager.Default.Logger.Warning(text, args);

		/// <summary>
		/// Begins a new section at the warning level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public static LogSection WarningSection(string text) => LogManager.Default.Logger.WarningSection(text);

		/// <summary>
		/// Begins a new section at the warning level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public static LogSection WarningSection(string text, params object[] args) => LogManager.Default.Logger.WarningSection(text, args);

		//****************************************

		/// <summary>
		/// Writes an entry at the error level
		/// </summary>
		/// <param name="text">The text to write</param>
		public static void Error(string text) => LogManager.Default.Logger.Error(text);

		/// <summary>
		/// Writes a formatted entry at the error level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public static void Error(string text, params object[] args) => LogManager.Default.Logger.Error(text, args);

		/// <summary>
		/// Begins a new section at the error level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public static LogSection ErrorSection(string text) => LogManager.Default.Logger.ErrorSection(text);

		/// <summary>
		/// Begins a new section at the error level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public static LogSection ErrorSection(string text, params object[] args) => LogManager.Default.Logger.ErrorSection(text, args);

		//****************************************

		/// <summary>
		/// Writes an exception
		/// </summary>
		/// <param name="newException">The exception to write</param>
		/// <param name="text">The text to write explaining the exception</param>
		/// <remarks>Automatically calls <see cref="LogManager.Flush" /></remarks>
		public static void Exception(Exception newException, string text) => LogManager.Default.Logger.Exception(newException, text);

		/// <summary>
		/// Writes a formatted exception
		/// </summary>
		/// <param name="newException">The exception to write</param>
		/// <param name="text">The text to write explaining the exception</param>
		/// <param name="args">The arguments to format the text with</param>
		/// <remarks>Automatically calls <see cref="LogManager.Flush" /></remarks>
		public static void Exception(Exception newException, string text, params object[] args) => LogManager.Default.Logger.Exception(newException, text, args);

		//****************************************

		/// <summary>
		/// Writes an entry at the critical level
		/// </summary>
		/// <param name="text">The text to write</param>
		public static void Critical(string text) => LogManager.Default.Logger.Critical(text);

		/// <summary>
		/// Writes a formatted entry at the critical level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public static void Critical(string text, params object[] args) => LogManager.Default.Logger.Critical(text, args);

		//****************************************

		/// <summary>
		/// Resets the section context for this logical call, so future log operations are isolated
		/// </summary>
		public static void ClearContext() => LogManager.Default.ClearContext();

		/// <summary>
		/// Writes an entry to the log
		/// </summary>
		/// <param name="newEntry">The entry to write</param>
		public static void Write(LogEntry newEntry) => LogManager.Default.Write(newEntry);

		/// <summary>
		/// Flushes all outputs, ensuring all previously written log messages have been stored
		/// </summary>
		public static void Flush() => LogManager.Default.Flush();
	}
}