/****************************************\
 LogOutput.cs
 Created: 2-06-2009
\****************************************/
using System;
using System.Xml;
//****************************************

namespace Proximity.Utility.Logging
{
	/// <summary>
	/// Defines an Output for logging information
	/// </summary>
	public abstract class LogOutput
	{	//****************************************
		
		/// <summary>
		/// Creates a new Log Output
		/// </summary>
		protected LogOutput()
		{
			
		}
		
		/// <summary>
		/// Creates a new Log Output
		/// </summary>
		/// <param name="reader">Configuration Settings</param>
		protected LogOutput(XmlReader reader)
		{
			reader.MoveToElement();
			
			while (reader.MoveToNextAttribute())
			{
				ReadAttribute(reader.LocalName, reader.Value);
			}
			
			reader.MoveToElement();
			
			if (reader.IsEmptyElement)
				return;
			
			while(reader.Read())
			{
				if (reader.MoveToContent() == XmlNodeType.EndElement)
					break;
				
				if (ReadElement(reader))
					continue;
				
				reader.Skip();
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Reads an attribute from the configuration
		/// </summary>
		/// <param name="name">The name of the attribute</param>
		/// <param name="value">The attribute's value</param>
		/// <returns>True if the Attribute is known, otherwise False</returns>
		protected virtual bool ReadAttribute(string name, string value)
		{
			return false;
		}
		
		/// <summary>
		/// Reads an element from the configuration
		/// </summary>
		/// <param name="reader">The XmlReader containing the element</param>
		/// <returns>True if the Element was known, otherwise False</returns>
		protected virtual bool ReadElement(XmlReader reader)
		{
			return false;
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
		/// Ends a logging section for this logical call
		/// </summary>
		/// <param name="oldSection">The details of the old logging section</param>
		protected internal abstract void FinishSection(LogSection oldSection);
		
		/// <summary>
		/// Ends the logging output process
		/// </summary>
		protected internal abstract void Finish();
	}
}
