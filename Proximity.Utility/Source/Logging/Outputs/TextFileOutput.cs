/****************************************\
 TextFileOutput.cs
 Created: 2-06-2009
\****************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
//****************************************

namespace Proximity.Utility.Logging.Outputs
{
	/// <summary>
	/// Writes log entries to a text file
	/// </summary>
	public class TextFileOutput : FileOutput
	{	//****************************************
		private TextWriter _Writer;
		
		private int _IndentSize = 2;
		private bool _IndentTabs = true;

		private Encoding _Encoding;
		
		private string OutputFormatPre;
		private string OutputFormatPost;
		
		private readonly ThreadLocal<StringBuilder> _OutputBuilder;
		//****************************************
		
		/// <summary>
		/// Creates a new Text File Output
		/// </summary>
		/// <param name="reader">Configuration settings</param>
		public TextFileOutput(XmlReader reader) : base(reader)
		{
			_Encoding = Encoding.Default;
			
			_OutputBuilder = new ThreadLocal<StringBuilder>(() => new StringBuilder());
			
			OutputFormatPre = "{0:yyyy-MM-dd HH:mm:ss.fff}:\t";
			OutputFormatPost = "{4:16}: {7}";
		}
		
		//****************************************
		
		/// <inheritdoc />
		protected internal override void Start()
		{
			base.Start();
			
			_Writer = TextWriter.Synchronized(new StreamWriter(Stream, _Encoding));
			_Writer.WriteLine("Log Started {0} (Process {1})", LogManager.GetTimestamp(), LogManager.StartTime);
		}
		
		/// <inheritdoc />
		protected internal override void StartSection(LogSection newSection)
		{
			Write(newSection.Entry);
		}
		
		/// <inheritdoc />
		protected internal override void Write(LogEntry newEntry)
		{	//****************************************
			string SourceAssembly = null, SourceFullType = null, SourceShortType = null, SourceMethod = null;
			object[] Arguments;
			
			int IndentLevel = LogManager.Context.Count;
			var OutputBuilder = _OutputBuilder.Value;
			var MyWriter = _Writer;
			//****************************************
			
			if (MyWriter == null)
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
				MyWriter.WriteLine("Got negative Relative Time: {0} ({1} - {2})", newEntry.RelativeTime, newEntry.Timestamp, LogManager.StartTime);
				Console.WriteLine("Got negative Relative Time: {0} ({1} - {2})", newEntry.RelativeTime, newEntry.Timestamp, LogManager.StartTime);
			}
			
			Arguments = new object[] {newEntry.Timestamp, DateTime.MinValue.Add(newEntry.RelativeTime > TimeSpan.Zero ? newEntry.RelativeTime : TimeSpan.Zero), SourceAssembly, SourceFullType, SourceShortType, SourceMethod, newEntry.Severity, newEntry.Text};
			
			OutputBuilder.AppendFormat(OutputFormatPre, Arguments);
			
			if (_IndentTabs)
				OutputBuilder.Append('\t', IndentLevel);
			else
				OutputBuilder.Append(' ', IndentLevel * _IndentSize);
			
			OutputBuilder.AppendFormat(CultureInfo.InvariantCulture, OutputFormatPost, Arguments);
			
			if (newEntry is ExceptionLogEntry)
			{
				foreach(string EntryLine in ((ExceptionLogEntry)newEntry).Exception.ToString().Split(new string[] {Environment.NewLine}, StringSplitOptions.None))
				{
					OutputBuilder.AppendLine();
					
					OutputBuilder.AppendFormat(OutputFormatPre, Arguments);
					
					if (_IndentTabs)
						OutputBuilder.Append('\t', IndentLevel);
					else
						OutputBuilder.Append(' ', IndentLevel * _IndentSize);
					
					OutputBuilder.Append(EntryLine);
				}
			}
			
			MyWriter.WriteLine(OutputBuilder.ToString());
			
			OutputBuilder.Length = 0;
			
			MyWriter.Flush();
			Stream.Flush();
		}
		
		/// <inheritdoc />
		protected internal override void FinishSection(LogSection oldSection)
		{
		}
		
		/// <inheritdoc />
		protected internal override void Finish()
		{
			if (_Writer != null)
				_Writer.Close();
			
			Interlocked.Exchange(ref _Writer, null);
			
			base.Finish();
		}
		
		//****************************************
		
		/// <inheritdoc />
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
		
		/// <inheritdoc />
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
	}
}
