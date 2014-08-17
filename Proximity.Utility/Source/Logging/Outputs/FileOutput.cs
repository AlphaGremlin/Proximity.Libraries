/****************************************\
 FileOutput.cs
 Created: 2-06-2009
\****************************************/
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility.Collections;
using Proximity.Utility.Logging.Config;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Logging.Outputs
{
	/// <summary>
	/// Base class for all outputs that write to a local file
	/// </summary>
	public abstract class FileOutput : LogOutput
	{	//****************************************
		private readonly AsyncCollection<FullLogEntry> _Entries = new AsyncCollection<FullLogEntry>();
		private Task _LogTask;
		
		private FileStream _Stream;
		
		private string _FileName;
		private RolloverType _RolloverOn;
		private long _MaxSize;
		private int? _KeepHistory;
		
		private DateTime _LastLogEntry;
		//****************************************
		
		/// <summary>
		/// Creates a new File Outout
		/// </summary>
		protected FileOutput() : base()
		{
		}
		
		//****************************************
		
		/// <inheritdoc />
		protected internal override void Configure(OutputElement config)
		{	//****************************************
			var MyConfig = (FileOutputElement)config;
			//****************************************
			
			_FileName = Path.Combine(LogManager.OutputPath, MyConfig.Prefix);
			_RolloverOn = MyConfig.RolloverOn;
			
			_MaxSize = MyConfig.MaximumSize;
			_KeepHistory = MyConfig.KeepHistory != -1 ? (int?)MyConfig.KeepHistory : null;
		}
		
		
		/// <inheritdoc />
		protected internal sealed override void Start()
		{
			_LogTask = PerformLogging();
		}
		
		/// <inheritdoc />
		protected internal override void StartSection(LogSection newSection)
		{
			Write(newSection.Entry);
		}
		
		/// <inheritdoc />
		protected internal sealed override void Write(LogEntry newEntry)
		{
			try
			{
				_Entries.Add(new FullLogEntry(newEntry, LogManager.Context)).Wait();
			}
			catch (OperationCanceledException)
			{
			}
		}
		
		/// <inheritdoc />
		protected internal override void Flush()
		{
			var MyCompletionSource = new TaskCompletionSource<VoidStruct>();
			
			try
			{
				_Entries.Add(new FullLogEntry(null, null, () => MyCompletionSource.SetResult(VoidStruct.Empty))).Wait();
				
				MyCompletionSource.Task.Wait();
			}
			catch (OperationCanceledException)
			{
			}
		}
		
		/// <inheritdoc />
		protected internal override void FinishSection(LogSection oldSection)
		{
		}
		
		/// <inheritdoc />
		protected internal sealed override void Finish()
		{
			_Entries.CompleteAdding();
			
			_LogTask.Wait();
		}
		
		//****************************************
		
		/// <summary>
		/// Retrieves the extension of the file to create
		/// </summary>
		/// <returns>The extension  of the file</returns>
		protected virtual string GetExtension()
		{
			return "txt";
		}
		
		/// <summary>
		/// Notifies implementers that the underlying stream has switched
		/// </summary>
		/// <param name="newStream">The new stream to write to. May be null if we're closing</param>
		protected abstract void OnStreamChanging(Stream newStream);
		
		/// <summary>
		/// Notifies implementers that they can write to the stream
		/// </summary>
		/// <param name="entry">The log entry to write</param>
		/// <param name="context">The context when the entry was recorded</param>
		protected abstract void OnWrite(LogEntry entry, ImmutableCountedStack<LogSection> context);
		
		//****************************************
		
		private async Task PerformLogging()
		{	//****************************************
			FullLogEntry MyEntry;
			//****************************************
			
			try
			{
				CheckOutput();
				
				foreach (var MyEntryTask in _Entries.GetConsumingEnumerable())
				{
					if (MyEntryTask.Status == TaskStatus.RanToCompletion)
					{
						MyEntry = MyEntryTask.Result;
					}
					else
					{
						MyEntry = await MyEntryTask;
						
						// Check the output file status, since we've been waiting
						CheckOutput();
					}
					
					if (MyEntry.Entry != null)
						OnWrite(MyEntry.Entry, MyEntry.Context);
					
					_Stream.Flush();
					
					if (MyEntry.Callback != null)
						MyEntry.Callback();
				}
			}
			catch (OperationCanceledException)
			{
			}
			finally
			{
				// Close the log file
				OnStreamChanging(null);
				
				if (_Stream != null)
				{
					_Stream.Flush();
					_Stream.Close();
					
					_Stream = null;
				}
			}
		}
		
		private void CheckOutput()
		{	//****************************************
			bool CloseOld = false;
			var CurrentTime = DateTime.Now;
			var CurrentStream = _Stream;
			//****************************************
			
			// Do we already have an open file?
			if (_Stream != null)
			{
				// Yes, check if the close conditions have been met
				switch (_RolloverOn)
				{
				case RolloverType.Daily:
					if (_LastLogEntry.Date != CurrentTime.Date)
						CloseOld = true;
					break;
					
				case RolloverType.Weekly:
					var MyCalendar = CultureInfo.InvariantCulture.Calendar;
					if (MyCalendar.GetWeekOfYear(_LastLogEntry, CalendarWeekRule.FirstDay, DayOfWeek.Sunday) != MyCalendar.GetWeekOfYear(CurrentTime, CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
						CloseOld = true;
					break;
					
				case RolloverType.Monthly:
					if (_LastLogEntry.Year != CurrentTime.Year || _LastLogEntry.Month != CurrentTime.Month)
						CloseOld = true;
					break;
				
				case RolloverType.Size:
					if (_Stream.Length >= _MaxSize)
						CloseOld = true;
					break;
					
				case RolloverType.Startup:
				default:
					// Do nothing
					break;
				}
				
				if (!CloseOld)
					return;
			}
			
			//****************************************
			
			// We either have no stream, or we're performing a rollover
			string FullPath;
			bool CanAppend = false;
			
			// Figure out the appropriate file name
			switch (_RolloverOn)
			{
			case RolloverType.Daily:
			case RolloverType.Weekly:
			case RolloverType.Monthly:
				FullPath = string.Format("{0} {1:yyyyMMdd}.{2}", _FileName, CurrentTime, GetExtension());
				CanAppend = true;
				break;
				
			case RolloverType.Startup:
			case RolloverType.Size:
			default:
				FullPath = string.Format("{0} {1:yyyyMMdd'T'HHmmss}.{2}", _FileName, CurrentTime, GetExtension());
				break;
			}
			
			FullPath = Path.Combine(LogManager.OutputPath, FullPath);
			
			//****************************************
			
			// Try and open/append to the log file
			try
			{
				_Stream = File.Open(FullPath, CanAppend ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
			}
			catch(Exception)
			{
				// Failed to create the file. Add our PID to the end and try again
				FullPath = string.Format("{0} ({1}).{2}", Path.Combine(LogManager.OutputPath, Path.GetFileNameWithoutExtension(FullPath)), Process.GetCurrentProcess().Id, GetExtension());
				
				try
				{
					_Stream = File.Open(FullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
				}
				catch (Exception e)
				{
					_Stream = null;
					
					Debug.Print(e.Message);
				}
			}
			
			//****************************************
			
			// Update the time we rolled over
			_LastLogEntry = DateTime.Now;
			
			// Let the inherited class know we've changed stream
			OnStreamChanging(_Stream);
			
			// If we've changed Stream, close the old one
			if (CurrentStream != null && CurrentStream != _Stream)
			{
				CurrentStream.Flush();
				
				CurrentStream.Close();
			}
			
			//****************************************
			
			Debug.Print(string.Format("Logging to {0}", FullPath));
			
			// If there's a history limit, perform some cleanup
			if (_KeepHistory.HasValue)
			{
				try
				{
					var DeleteFiles = Directory.EnumerateFiles(LogManager.OutputPath, string.Format("{0} *.{1}", _FileName, GetExtension()))
						.Where(name => name != FullPath) // Ignore the file we've currently got open
						.OrderByDescending(name => File.GetCreationTime(name)) // Order by most recently created
						.Skip(_KeepHistory.Value); // Skip the allowed history limit
						
					// Enumerate the directory, finding all the files to delete
					foreach (var FileName in DeleteFiles)
					{
						try
						{
							File.Delete(FileName);
							
							Debug.Print(string.Format("Removed old log file {0}", FileName));
						}
						catch (Exception) // Ignore any errors that happen (file in use, no delete permissions, etc)
						{
						}
					}
				}
				catch (IOException)
				{
				}
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets the filename to write to, minus the extension
		/// </summary>
		public string FileName
		{
			get { return _FileName; }
			set
			{
				if (_Stream != null)
					throw new InvalidOperationException("Cannot change output file whilst logging is running");

				_FileName = value;
			}
		}

		/// <summary>
		/// Gets the stream to be written to
		/// </summary>
		protected Stream Stream
		{
			get { return _Stream; }
		}
		
		//****************************************
		
		private struct FullLogEntry
		{
			public readonly ImmutableCountedStack<LogSection> Context;
			public readonly LogEntry Entry;
			public readonly Action Callback;
			
			public FullLogEntry(LogEntry entry, ImmutableCountedStack<LogSection> context)
			{
				this.Entry = entry;
				this.Context = context;
				this.Callback = null;
			}
			
			public FullLogEntry(LogEntry entry, ImmutableCountedStack<LogSection> context, Action callback)
			{
				this.Entry = entry;
				this.Context = context;
				this.Callback = callback;
			}
		}
	}
}
