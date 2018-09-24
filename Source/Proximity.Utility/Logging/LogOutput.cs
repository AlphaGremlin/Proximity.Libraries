using System;
using Proximity.Utility.Logging.Config;
using System.Xml;
using System.Security;
//****************************************

namespace Proximity.Utility.Logging
{
	/// <summary>
	/// Defines an Output for logging information
	/// </summary>
	public abstract class LogOutput
	{
		/// <summary>
		/// Creates a new Log Output
		/// </summary>
		protected LogOutput() : this(LogManager.Default)
		{
		}

		/// <summary>
		/// Creates a new Log Output
		/// </summary>
		/// <param name="target">The log target we're receiving from</param>
		protected LogOutput(LogTarget target)
		{
			Target = target;
		}

		//****************************************

		/// <summary>
		/// Applies the configuration for this log output
		/// </summary>
		/// <param name="config">The configuration to use</param>
		[SecurityCritical]
		protected internal virtual void Configure(OutputElement config)
		{
		}

		//****************************************

		/// <summary>
		/// Starts the logging output process
		/// </summary>
		protected internal abstract void Start();

		/// <summary>
		/// Starts a logging section for this logical call
		/// </summary>
		/// <param name="newSection">The details of the new logging section</param>
		protected internal abstract void StartSection(LogSection newSection);

		/// <summary>
		/// Writes an entry to the log
		/// </summary>
		/// <param name="newEntry">The log entry to write</param>
		protected internal abstract void Write(LogEntry newEntry);

		/// <summary>
		/// Flushes the output, ensuring all previously written log entries have been stored
		/// </summary>
		protected internal abstract void Flush();

		/// <summary>
		/// Ends a logging section for this logical call
		/// </summary>
		/// <param name="oldSection">The details of the old logging section</param>
		protected internal abstract void FinishSection(LogSection oldSection);

		/// <summary>
		/// Ends the logging output process
		/// </summary>
		protected internal abstract void Finish();

		//****************************************

		/// <summary>
		/// Gets the log target we're receiving from
		/// </summary>
		public LogTarget Target { get; }
	}
}