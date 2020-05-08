using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using Proximity.Logging.Config;
using Proximity.Logging.Outputs;
//****************************************

namespace Proximity.Logging
{
	/// <summary>
	/// Manages a target for Logged data
	/// </summary>
	public sealed class LogTarget
	{ //****************************************
		private readonly List<LogOutput> _Outputs = new List<LogOutput>();
		private readonly AsyncLocal<ImmutableCountedStack<LogSection>> _Context = new AsyncLocal<ImmutableCountedStack<LogSection>>();
		//****************************************

		/// <summary>
		/// Creates a new Logging Target
		/// </summary>
		public LogTarget()
		{
			StartTime = LogManager.GetTimestamp();
		}

		//****************************************

		/// <summary>
		/// Starts the logging framework
		/// </summary>
		/// <param name="config">The configuration to use. Null for defaults</param>
		public void Start(LoggingConfig config)
		{
			if (OutputPath == null)
				OutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Name);

			if (!Directory.Exists(OutputPath))
				Directory.CreateDirectory(OutputPath);

			//****************************************
			// Initialise Log Outputs

			IsStarted = true;

			foreach (LogOutput MyOutput in _Outputs)
			{
				MyOutput.Start();
			}

			if (config != null)
			{
				foreach (var MyConfig in config.Outputs)
				{
					var ResolvedOutput = MyConfig.Populate<OutputElement>(ResolveType(MyConfig.Type));

					AddOutput(ResolvedOutput.ToOutput());
				}
			}
		}

		/// <summary>
		/// Adds a new logging output
		/// </summary>
		/// <param name="output">A log output to receive logging information</param>
		public void AddOutput(LogOutput output)
		{
			_Outputs.Add(output);

			if (IsStarted)
				output.Start();
		}

		/// <summary>
		/// Starts a new logging section
		/// </summary>
		/// <param name="entry">The logging entry to start with</param>
		/// <returns>A new logging section</returns>
		public LogSection StartSection(LogEntry entry) => StartSection(entry, 0);

		/// <summary>
		/// Starts a new logging section
		/// </summary>
		/// <param name="entry">The logging entry to start with</param>
		/// <param name="priority">The priority of this entry</param>
		/// <returns>A new logging section</returns>
		public LogSection StartSection(LogEntry entry, int priority)
		{
			var NewSection = new LogSection(this, entry, priority);

			foreach (var MyOutput in _Outputs)
				MyOutput.StartSection(NewSection);

			Context = Context.Push(NewSection);

			return NewSection;
		}

		/// <summary>
		/// Resets the section context for this logical call, so future log operations are isolated
		/// </summary>
		public void ClearContext() => Context = ImmutableCountedStack<LogSection>.Empty;

		/// <summary>
		/// Flushes all outputs, ensuring all previously written log messages have been stored
		/// </summary>
		public void Flush()
		{
			foreach (var MyOutput in _Outputs)
				MyOutput.Flush();
		}

		/// <summary>
		/// Ends the logging framework
		/// </summary>
		public void Finish()
		{
			foreach (var MyOutput in _Outputs)
				MyOutput.Finish();

			IsStarted = false;
		}

		/// <summary>
		/// Writes an entry to the log
		/// </summary>
		/// <param name="newEntry">The entry to write</param>
		public void Write(LogEntry newEntry)
		{
			foreach (var MyOutput in _Outputs)
				MyOutput.Write(newEntry);
		}

		//****************************************

		/// <summary>
		/// Finishes a logging section
		/// </summary>
		/// <param name="oldSection">The section to finish</param>
		internal void FinishSection(LogSection oldSection)
		{ //****************************************
			var MyContext = Context;
			var MyOutputs = _Outputs;
			//****************************************

			oldSection.IsDisposed = true;

			if (MyContext.IsEmpty || MyContext.Peek() != oldSection)
				return;

			Context = MyContext.Pop();

			foreach (var MyOutput in MyOutputs)
				MyOutput.FinishSection(oldSection);
		}

		//****************************************

		/// <summary>
		/// Gets a list of all the logging outputs
		/// </summary>
		public IReadOnlyCollection<LogOutput> Outputs => _Outputs;

		/// <summary>
		/// Gets/Sets the path logging outputs will place their results
		/// </summary>
		public string OutputPath { get; set; }

		/// <summary>
		/// Gets the time the logging framework was started
		/// </summary>
		public DateTime StartTime { get; }

		/// <summary>
		/// Gets the current section stack for this logical context
		/// </summary>
		public ImmutableCountedStack<LogSection> Context
		{
			get => _Context.Value ?? ImmutableCountedStack<LogSection>.Empty;
			private set => _Context.Value = value;
		}

		/// <summary>
		/// Gets the current depth of the section stack
		/// </summary>
		public int SectionDepth => Context.Count;

		/// <summary>
		/// Gets a logger targeting this Log Target
		/// </summary>
		public Logger Logger => new Logger(this);

		/// <summary>
		/// Gets whether the log target is active
		/// </summary>
		public bool IsStarted { get; private set; }

		//****************************************

		private static Type ResolveType(string typeName)
		{
			if (typeName.IndexOf(',') == -1)
			{
				// No Assembly Definition, just a Type
				if (typeName.IndexOf('.') == -1)
				{
					// No namespace either. Add the default namespace
					typeName = typeof(FileOutput).Namespace + System.Type.Delimiter + typeName;
				}

				return typeof(FileOutput).Assembly.GetType(typeName);
			}

			return Type.GetType(typeName);
		}
	}
}
