/****************************************\
 XmlFileOutput.cs
 Created: 2-06-2009
\****************************************/
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Logging.Outputs
{
	/// <summary>
	/// Writes log output to an XML file
	/// </summary>
	public class XmlFileOutput : FileOutput
	{	//****************************************
		private Guid UniqueID = Guid.NewGuid();
		
		private XmlWriter Writer;
		private XmlWriterSettings WriterSettings;
		//****************************************
		
		/// <summary>
		/// Creates a new Xml Output writer
		/// </summary>
		/// <param name="reader">Configuration settings</param>
		public XmlFileOutput(XmlReader reader) : base(reader)
		{
			WriterSettings = new XmlWriterSettings();
			WriterSettings.Indent = true;
		}
		
		//****************************************
		
		/// <summary>
		/// Starts the logging output process
		/// </summary>
		protected internal override void Start() 
		{
			base.Start();
			
			Writer = XmlWriter.Create(Stream, WriterSettings);
			
			Writer.WriteProcessingInstruction("xml-stylesheet", "href=\"Style.css\" type=\"text/css\"");
			
			Writer.WriteStartElement("Log");
			
			Writer.WriteAttributeString("StartTime", LogManager.StartTime.ToString("O", CultureInfo.InvariantCulture));
		}
		
		/// <summary>
		/// Starts a logging section for this thread
		/// </summary>
		/// <param name="newSection">The details of the new logging section</param>
		protected internal override void StartSection(LogSection newSection)
		{	//****************************************
			ThreadLocalData MyData = ThreadLocalData.GetLocalData(UniqueID);
			XmlWriter LocalWriter = MyData.SectionWriter;
			//****************************************
			
			if (MyData.SectionLevel++ == 0)
			{
				MyData.DataStream = new MemoryStream();
				
				MyData.SectionWriter = XmlWriter.Create(MyData.DataStream);
				LocalWriter = MyData.SectionWriter;
			}
			
			LocalWriter.WriteStartElement("Section");
			LocalWriter.WriteAttributeString("Time", newSection.Entry.RelativeTime.ToString());
			LocalWriter.WriteAttributeString("Severity", newSection.Entry.Severity.ToString());
			LocalWriter.WriteElementString("Title", newSection.Text.SanitiseForDisplay());
		}
		
		/// <summary>
		/// Writes an entry to the log
		/// </summary>
		/// <param name="newEntry">The log entry to write</param>
		protected internal override void Write(LogEntry newEntry)
		{	//****************************************
			ThreadLocalData MyData = ThreadLocalData.GetLocalData(UniqueID);
			XmlWriter LocalWriter;
			//****************************************
			
			try
			{
				if (MyData.SectionLevel == 0)
				{
					Monitor.Enter(Writer);
					LocalWriter = Writer;
				}
				else
					LocalWriter = MyData.SectionWriter;
				
				LocalWriter.WriteStartElement("Entry");
				
				LocalWriter.WriteAttributeString("Time", newEntry.RelativeTime.ToString());
				LocalWriter.WriteAttributeString("Severity", newEntry.Severity.ToString());
				
				LocalWriter.WriteString(newEntry.Text.SanitiseForDisplay());
	
				LocalWriter.WriteEndElement();
			}
			finally
			{
				if (MyData.SectionLevel == 0)
					Monitor.Exit(Writer);
			}
		}
		
		/// <summary>
		/// Ends a logging section for this thread
		/// </summary>
		protected internal override void FinishSection()
		{	//****************************************
			ThreadLocalData MyData = ThreadLocalData.GetLocalData(UniqueID);
			//****************************************
			
			MyData.SectionWriter.WriteEndElement();
				
			if (--MyData.SectionLevel == 0)
			{
				MyData.SectionWriter.Close();
					
				MyData.DataStream.Position = 0;
				lock (Writer)
				{
					XmlReader MyReader = XmlReader.Create(MyData.DataStream);
					
					MyReader.ReadToFollowing("Section");
					
					Writer.WriteNode(MyReader, false);
					
					Writer.Flush();
				}
			}
		}
		
		/// <summary>
		/// Ends the logging output process
		/// </summary>
		protected internal override void Finish()
		{
			Writer.WriteEndElement();

			Writer.Flush();

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
			case "DebuggerOnly":
				WriterSettings.Indent = bool.Parse(value);
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
		/// <remarks>For this output provider, the value is always 'xml'</remarks>
		protected override string GetExtension()
		{
			return "xml";
		}
		
		//****************************************
		
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets the indentation to apply to the Xml output
		/// </summary>
		public bool Indent
		{
			get { return WriterSettings.Indent; }
			set { WriterSettings.Indent = value; }
		}
		
		//****************************************
		
		private class ThreadLocalData
		{	//****************************************
			public XmlWriter SectionWriter;
			public Stream DataStream;
			public int SectionLevel;
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
