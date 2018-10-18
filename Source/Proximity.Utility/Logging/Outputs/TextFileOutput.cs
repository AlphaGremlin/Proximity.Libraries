using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
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
		//****************************************

		/// <summary>
		/// Creates a new Text File Output for the default Target
		/// </summary>
		public TextFileOutput() : this(LogManager.Default)
		{
		}

		/// <summary>
		/// Creates a new Text File Output
		/// </summary>
		/// <param name="target">The Logging Target to use</param>
		public TextFileOutput(LogTarget target) : base(target)
		{
			Encoding = Encoding.Default;
			
			_OutputBuilder = new StringBuilder();
			
			OutputFormatPre = "{0:yyyy-MM-dd HH:mm:ss.fff}:\t";
			OutputFormatPost = "{4,16}: {7}";
		}
		
		//****************************************
		
		/// <inheritdoc />
		[SecurityCritical]
		protected internal override void Configure(OutputElement config)
		{	//****************************************
			var MyConfig = (TextFileOutputElement)config;
			//****************************************
			
			base.Configure(config);
			
			//****************************************
			
			IndentTabs = MyConfig.IndentTabs;
			IndentSize = MyConfig.IndentSize;
			
			Encoding = Encoding.GetEncoding(MyConfig.Encoding);
		}
		
		/// <inheritdoc />
		[SecuritySafeCritical]
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
				_Writer = new StreamWriter(Stream, Encoding, 1024);
				_Writer.WriteLine("Log Started {0} (Process {1})", LogManager.GetTimestamp(), LogManager.StartTime);
			}
		}
		
		/// <inheritdoc />
		[SecuritySafeCritical]
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

			var RelativeTime = entry.Timestamp - LogManager.StartTime;

			Arguments = new object[] {entry.Timestamp, DateTime.MinValue.Add(RelativeTime > TimeSpan.Zero ? RelativeTime : TimeSpan.Zero), SourceAssembly, SourceFullType, SourceShortType, SourceMethod, entry.Severity, entry.Text};
			
			OutputBuilder.AppendFormat(CultureInfo.InvariantCulture, OutputFormatPre, Arguments);
			
			IndentTo(IndentLevel);
			
			OutputBuilder.AppendFormat(CultureInfo.InvariantCulture, OutputFormatPost, Arguments);
			
			if (entry is ExceptionLogEntry)
			{
				foreach(string EntryLine in ((ExceptionLogEntry)entry).Exception.ToString().Split(new string[] {Environment.NewLine}, StringSplitOptions.None))
				{
					OutputBuilder.AppendLine();
					
					OutputBuilder.AppendFormat(CultureInfo.InvariantCulture, OutputFormatPre, Arguments);
					
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
			if (IndentTabs)
				_OutputBuilder.Append('\t', indentLevel);
			else if (IndentSize != 0)
				_OutputBuilder.Append(' ', indentLevel * IndentSize);
		}

		//****************************************

		/// <summary>
		/// Gets/Sets whether to use tabs or spaces for indenting
		/// </summary>
		public bool IndentTabs { get; set; } = true;

		/// <summary>
		/// Gets/Sets the size of each indentation level
		/// </summary>
		public int IndentSize { get; set; } = 2;

		/// <summary>
		/// Gets/Sets the text encoding to use
		/// </summary>
		public Encoding Encoding { get; set; }

		/// <summary>
		/// Gets/Sets the output line to display before any indentation
		/// </summary>
		public string OutputFormatPre { get; set; }

		/// <summary>
		/// Gets/Sets the output line format to display after the indentation
		/// </summary>
		/// <remarks>Should not include a terminating newline</remarks>
		public string OutputFormatPost { get; set; }
	}
}