/****************************************\
 TextFileOutput.cs
 Created: 2-06-2009
\****************************************/
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Logging.Outputs
{
	/// <summary>
	/// Writes log entries to a text file
	/// </summary>
	public class TextFileOutput : FileOutput
	{	//****************************************
		private Guid UniqueID = Guid.NewGuid();
		//****************************************
		private TextWriter Writer;
		
		private int _IndentSize = 2;
		private bool _IndentTabs = true;

		private Encoding _Encoding;
		
		private string OutputFormatPre;
		private string OutputFormatPost;
		//****************************************
		
		/// <summary>
		/// Creates a new Text File Output
		/// </summary>
		/// <param name="reader">Configuration settings</param>
		public TextFileOutput(XmlReader reader) : base(reader)
		{
			_Encoding = Encoding.Default;
			
			OutputFormatPre = "{0:yyyy-MM-dd HH:mm:ss.fff}:\t";
			OutputFormatPost = "{4:16}: {7}";
		}
		
		//****************************************
		
		/// <summary>
		/// Starts the logging output process
		/// </summary>
		protected internal override void Start()
		{
			base.Start();
			
			Writer = TextWriter.Synchronized(new StreamWriter(Stream, _Encoding));
			Writer.WriteLine("Log Started {0} (Process {1})", LogManager.GetTimestamp(), LogManager.StartTime);
		}
		
		/// <summary>
		/// Starts a logging section for this thread
		/// </summary>
		/// <param name="newSection">The details of the new logging section</param>
		protected internal override void StartSection(LogSection newSection)
		{
			Write(newSection.Entry);
			
			ThreadLocalData.GetLocalData(UniqueID).IndentLevel++;
		}
		
		/// <summary>
		/// Writes an entry to the log
		/// </summary>
		/// <param name="newEntry">The log entry to write</param>
		protected internal override void Write(LogEntry newEntry)
		{	//****************************************
			string SourceAssembly = null, SourceFullType = null, SourceShortType = null, SourceMethod = null;
			object[] Arguments;
			ThreadLocalData MyData = ThreadLocalData.GetLocalData(UniqueID);
			StringBuilder OutputBuilder = MyData.OutputBuilder;
			//****************************************
			
			if (Writer == null)
				return;
			
			if (newEntry.Source != null)
			{
				var DeclaringType = newEntry.Source.DeclaringType;
				
				if (DeclaringType.IsNestedPrivate)
					DeclaringType = DeclaringType.DeclaringType;
				
				SourceAssembly = DeclaringType.Assembly.GetName().Name;
				SourceFullType = DeclaringType.FullName;
				SourceShortType = DeclaringType.Name;
				SourceMethod = newEntry.Source.Name;
			}
			
			if (newEntry.RelativeTime < TimeSpan.Zero)
			{
				Writer.WriteLine("Got negative Relative Time: {0} ({1} - {2})", newEntry.RelativeTime, newEntry.Timestamp, LogManager.StartTime);
				Console.WriteLine("Got negative Relative Time: {0} ({1} - {2})", newEntry.RelativeTime, newEntry.Timestamp, LogManager.StartTime);
			}
			
			Arguments = new object[] {newEntry.Timestamp, DateTime.MinValue.Add(newEntry.RelativeTime > TimeSpan.Zero ? newEntry.RelativeTime : TimeSpan.Zero), SourceAssembly, SourceFullType, SourceShortType, SourceMethod, newEntry.Severity, newEntry.Text};
			
			OutputBuilder.AppendFormat(OutputFormatPre, Arguments);
			
			if (_IndentTabs)
				OutputBuilder.Append('\t', MyData.IndentLevel);
			else
				OutputBuilder.Append(' ', MyData.IndentLevel * _IndentSize);
			
			OutputBuilder.AppendFormat(CultureInfo.InvariantCulture, OutputFormatPost, Arguments);
			
			if (newEntry is ExceptionLogEntry)
			{
				foreach(string EntryLine in ((ExceptionLogEntry)newEntry).Exception.ToString().Split(new string[] {Environment.NewLine}, StringSplitOptions.None))
				{
					OutputBuilder.AppendLine();
					
					OutputBuilder.AppendFormat(OutputFormatPre, Arguments);
					
					if (_IndentTabs)
						OutputBuilder.Append('\t', MyData.IndentLevel);
					else
						OutputBuilder.Append(' ', MyData.IndentLevel * _IndentSize);
					
					OutputBuilder.Append(EntryLine);
				}
			}
			
			Writer.WriteLine(OutputBuilder.ToString());
			
			OutputBuilder.Length = 0;
			
			Writer.Flush();
			Stream.Flush();
		}
		
		/// <summary>
		/// Ends a logging section for this thread
		/// </summary>
		protected internal override void FinishSection()
		{
			ThreadLocalData.GetLocalData(UniqueID).IndentLevel--;
		}
		
		/// <summary>
		/// Ends the logging output process
		/// </summary>
		protected internal override void Finish()
		{
			if (Writer != null)
				Writer.Close();
			
			Writer = null;
			
			base.Finish();
		}
		
		//****************************************
		
		/// <summary>
		/// Reads an attribute from the configuration
		/// </summary>
		/// <param name="name">The name of the attribute</param>
		/// <param name="value">The attribute's value</param>
		/// <returns>True if the Attribute is known, otherwise False</returns>
		protected override bool ReadAttribute(string name, string value)
		{
			switch (name)
			{
			case "IndentTabs":
				bool.TryParse(value, out _IndentTabs);
				break;
				
			case "IndentSize":
				int.TryParse(value, out _IndentSize);
				break;
				
			case "Encoding":
				_Encoding = Encoding.GetEncoding(value);
				break;
				
			default:
				return base.ReadAttribute(name, value);
			}
			
			return true;
		}
		
		/// <summary>
		/// Retrieves the extension of the file to create
		/// </summary>
		/// <returns>The extension  of the file</returns>
		/// <remarks>For this output provider, the value is always 'log'</remarks>
		protected override string GetExtension()
		{
			return "log";
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets whether to use tabs or spaces for indenting
		/// </summary>
		public bool IndentTabs
		{
			get { return _IndentTabs; }
			set { _IndentTabs = value; }
		}
		
		/// <summary>
		/// Gets/Sets the size of each indentation level
		/// </summary>
		public int IndentSize
		{
			get { return _IndentSize; }
			set { _IndentSize = value; }
		}
		
		/// <summary>
		/// Gets/Sets the text encoding to use
		/// </summary>
		public Encoding Encoding
		{
			get { return _Encoding; }
			set { _Encoding = value; }
		}
		
		//****************************************
		
		private class ThreadLocalData
		{	//****************************************
			public StringBuilder OutputBuilder = new StringBuilder();
			public int IndentLevel;
			//****************************************
			[ThreadStatic()] private static Dictionary<Guid, ThreadLocalData> LocalData;
			//****************************************
			
			public static ThreadLocalData GetLocalData(Guid uniqueID)
			{	//****************************************
				ThreadLocalData MyData;
				//****************************************
				
				// Any local data for this thread?
				if (LocalData == null)
					LocalData = new Dictionary<Guid, ThreadLocalData>();
				
				// Any data on this thread for this Output?
				if (!LocalData.TryGetValue(uniqueID, out MyData))
				{
					MyData = new ThreadLocalData();
					
					LocalData.Add(uniqueID, MyData);
				}
				
				return MyData;
			}
		}
	}
}
