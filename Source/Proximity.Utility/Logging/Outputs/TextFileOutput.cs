/****************************************\
 TextFileOutput.cs
 Created: 2-06-2009
\****************************************/
#if !MOBILE && !PORTABLE
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml;
using Proximity.Utility.Collections;
using Proximity.Utility.Configuration;
using Proximity.Utility.Logging.Config;
//****************************************

namespace Proximity.Utility.Logging.Outputs
{
	/// <summary>
	/// Writes log entries to a text file
	/// </summary>
	[TypedElement(typeof(TextFileOutputElement))]
	public class TextFileOutput : FileOutput
	{	//****************************************
		private readonly StringBuilder _OutputBuilder;
		private TextWriter _Writer;
		
		private int _IndentSize = 2;
		private bool _IndentTabs = true;

		private Encoding _Encoding;
		
		private string OutputFormatPre;
		private string OutputFormatPost;
		//****************************************
		
		/// <summary>
		/// Creates a new Text File Output
		/// </summary>
		public TextFileOutput() : base()
		{
			_Encoding = Encoding.Default;
			
			_OutputBuilder = new StringBuilder();
			
			OutputFormatPre = "{0:yyyy-MM-dd HH:mm:ss.fff}:\t";
			OutputFormatPost = "{4:16}: {7}";
		}
		
		//****************************************
		
		/// <inheritdoc />
		protected internal override void Configure(OutputElement config)
		{	//****************************************
			var MyConfig = (TextFileOutputElement)config;
			//****************************************
			
			base.Configure(config);
			
			//****************************************
			
			_IndentTabs = MyConfig.IndentTabs;
			_IndentSize = MyConfig.IndentSize;
			
			_Encoding = Encoding.GetEncoding(MyConfig.Encoding);
		}
		
		/// <inheritdoc />
		protected override void OnStreamChanging(Stream newStream)
		{
			if (_Writer != null)
			{
				_Writer.WriteLine("Log Ended {0}", LogManager.GetTimestamp());
				_Writer.Close();
				
				_Writer = null;
			}
			
			if (newStream != null)
			{
				_Writer = new StreamWriter(Stream, _Encoding, 1024);
				_Writer.WriteLine("Log Started {0} (Process {1})", LogManager.GetTimestamp(), LogManager.StartTime);
			}
		}
		
		/// <inheritdoc />
		protected override void OnWrite(LogEntry entry, ImmutableCountedStack<LogSection> context)
		{	//****************************************
			string SourceAssembly = null, SourceFullType = null, SourceShortType = null, SourceMethod = null;
			object[] Arguments;
			
			var IndentLevel = context.Count;
			var OutputBuilder = _OutputBuilder;
			//****************************************
			
			if (entry.Source != null)
			{
				var DeclaringType = entry.Source.DeclaringType;
				
				while (DeclaringType != null && DeclaringType.IsNestedPrivate && DeclaringType.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length != 0)
					DeclaringType = DeclaringType.DeclaringType;

				if (DeclaringType == null)
					DeclaringType = entry.Source.DeclaringType;
				
				SourceAssembly = DeclaringType.Assembly.GetName().Name;
				SourceFullType = DeclaringType.FullName;
				SourceShortType = DeclaringType.Name;
				SourceMethod = entry.Source.Name;
			}
			
			Arguments = new object[] {entry.Timestamp, DateTime.MinValue.Add(entry.RelativeTime > TimeSpan.Zero ? entry.RelativeTime : TimeSpan.Zero), SourceAssembly, SourceFullType, SourceShortType, SourceMethod, entry.Severity, entry.Text};
			
			OutputBuilder.AppendFormat(OutputFormatPre, Arguments);
			
			IndentTo(IndentLevel);
			
			OutputBuilder.AppendFormat(CultureInfo.InvariantCulture, OutputFormatPost, Arguments);
			
			if (entry is ExceptionLogEntry)
			{
				foreach(string EntryLine in ((ExceptionLogEntry)entry).Exception.ToString().Split(new string[] {Environment.NewLine}, StringSplitOptions.None))
				{
					OutputBuilder.AppendLine();
					
					OutputBuilder.AppendFormat(OutputFormatPre, Arguments);
					
					IndentTo(IndentLevel);
					
					OutputBuilder.Append(EntryLine);
				}
			}
			
			_Writer.WriteLine(OutputBuilder.ToString());
			
			OutputBuilder.Length = 0;
			
			_Writer.Flush();
		}
		
		//****************************************
		
		/// <inheritdoc />
		protected override string GetExtension()
		{
			return "log";
		}
		
		//****************************************
		
		private void IndentTo(int indentLevel)
		{
			if (_IndentTabs)
				_OutputBuilder.Append('\t', indentLevel);
			else if (_IndentSize != 0)
				_OutputBuilder.Append(' ', indentLevel * _IndentSize);
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
#endif