/****************************************\
 XmlFileOutput.cs
 Created: 2-06-2009
\****************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Xml;
using Proximity.Utility.Collections;
using Proximity.Utility.Logging.Config;
//****************************************

namespace Proximity.Utility.Logging.Outputs
{
	/// <summary>
	/// Writes log output to an XML file
	/// </summary>
	public class XmlFileOutput : FileOutput
	{	//****************************************
		private static int _NextID = 0;
		//****************************************
		private readonly string _UniqueID;
		
		private XmlWriter _Writer;
		private readonly XmlWriterSettings WriterSettings;

#if NET462
		private static AsyncLocal<ImmutableCountedStack<ContextData>> _Context;
#endif
		//****************************************

		/// <summary>
		/// Creates a new Xml Output writer
		/// </summary>
		public XmlFileOutput() : base()
		{
			_UniqueID = string.Format("Logging.XmlOutput#{0}", Interlocked.Increment(ref _NextID));
			
			WriterSettings = new XmlWriterSettings();
			WriterSettings.Indent = true;
		}
		
		//****************************************
		
		protected internal override void Configure(OutputElement config)
		{	//****************************************
			var MyConfig = (XmlFileOutputElement)config;
			//****************************************
			
			base.Configure(config);
			
			//****************************************
			
			WriterSettings.Indent = MyConfig.Indent;
			
			WriterSettings.Encoding = Encoding.GetEncoding(MyConfig.Encoding);
		}
		
		//****************************************
		
		/// <inheritdoc />
		protected override void OnStreamChanging(Stream newStream)
		{
			if (_Writer != null)
			{
				_Writer.WriteEndElement();

				_Writer.Flush();
				_Writer.Close();

				_Writer = null;
			}
			
			if (newStream != null)
			{
				_Writer = XmlWriter.Create(newStream, WriterSettings);
				
				//_Writer.WriteProcessingInstruction("xml-stylesheet", "href=\"Style.css\" type=\"text/css\"");
				
				_Writer.WriteStartElement("Log");
				_Writer.WriteAttributeString("StartTime", LogManager.StartTime.ToString("O", CultureInfo.InvariantCulture));
			}
		}
		
		protected override void OnWrite(LogEntry entry, ImmutableCountedStack<LogSection> context)
		{
			
		}
		
		/// <inheritdoc />
		protected internal override void StartSection(LogSection newSection)
		{	//****************************************
			var MyContext = new ContextData(newSection);
			//****************************************
			
			Context = Context.Push(MyContext);
		}
		
		/// <inheritdoc />
		protected internal override void Write(LogEntry newEntry)
		{	//****************************************
			var MyContext = Context;
			var MySection = MyContext.IsEmpty ? null : MyContext.Peek();
			var MyWriter = MySection != null ? MySection.Writer : _Writer;
			//****************************************
			
			lock (MyWriter)
			{
				MyWriter.WriteStartElement("Entry");
				
				MyWriter.WriteAttributeString("Time", newEntry.RelativeTime.ToString());
				MyWriter.WriteAttributeString("Severity", newEntry.Severity.ToString());
				
				MyWriter.WriteString(newEntry.Text.SanitiseForDisplay());
	
				MyWriter.WriteEndElement();
			}
		}
		
		/// <inheritdoc />
		protected internal override void FinishSection(LogSection section)
		{	//****************************************
			var MyContext = Context;
			ContextData MySection, MyParent = null;
			//****************************************
			
			// Might be empty if we were added in the middle of a section
			if (MyContext.IsEmpty)
				return;
			
			// Remove this section's context
			Context = MyContext = MyContext.Pop(out MySection);
			
			while (!MyContext.IsEmpty)
			{
				MyParent = MyContext.Peek();
				
				lock (MyParent.Writer)
				{
					// If the parent isn't finished, we can write out to it
					if (!MyParent.IsFinished)
					{
						MySection.Finish(MyParent.Writer);
						
						return;
					}
				}
				
				// Parent is finished already, move up a level
				// This can happen if we start a void async operation inside a section and don't call ClearContext
				Context = MyContext = MyContext.Pop();
			}
			
			// No parent context, we write directly to the output
			lock (_Writer)
			{
				MySection.Finish(_Writer);
			}
		}
		
		//****************************************
		
		/// <inheritdoc />
		protected override string GetExtension()
		{
			return "xml";
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets the indentation to apply to the Xml output
		/// </summary>
		public bool Indent
		{
			get { return WriterSettings.Indent; }
			set { WriterSettings.Indent = value; }
		}
		
		/// <summary>
		/// Gets the current section stack for this logical context
		/// </summary>
		private ImmutableCountedStack<ContextData> Context
		{
#if NET462
			get { return _Context.Value ?? ImmutableCountedStack<ContextData>.Empty; }
			private set { _Context.Value = value; }
#else
			get { return (CallContext.LogicalGetData(_UniqueID) as ImmutableCountedStack<ContextData>) ?? ImmutableCountedStack<ContextData>.Empty; }
			set { CallContext.LogicalSetData(_UniqueID, value); }
#endif
		}
		
		//****************************************
		
		private class ContextData
		{	//****************************************
			private readonly XmlWriter _SectionWriter;
			private readonly Stream _DataStream;
			private bool _IsFinished;
			//****************************************
			
			public ContextData(LogSection section)
			{
				_DataStream = new MemoryStream();
				
				_SectionWriter = XmlWriter.Create(_DataStream);
				
				//****************************************
				
				_SectionWriter.WriteStartElement("Section");
				_SectionWriter.WriteAttributeString("Time", section.Entry.RelativeTime.ToString());
				_SectionWriter.WriteAttributeString("Severity", section.Entry.Severity.ToString());
				_SectionWriter.WriteElementString("Title", section.Text.SanitiseForDisplay());
			}
			
			//****************************************
			
			public void Finish(XmlWriter parent)
			{
				_IsFinished = true;
				
				_SectionWriter.WriteEndElement();
				
				_SectionWriter.Close();
					
				_DataStream.Position = 0;
				
				using (var MyReader = XmlReader.Create(_DataStream))
				{
					MyReader.ReadToFollowing("Section");
					
					parent.WriteNode(MyReader, false);
				}
				
				parent.Flush();
			}
			
			//****************************************
			
			public XmlWriter Writer
			{
				get { return _SectionWriter; }
			}
			
			public bool IsFinished
			{
				get { return _IsFinished; }
			}
		}
	}
}
