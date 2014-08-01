/****************************************\
 XmlFileOutputElement.cs
 Created: 2014-07-30
\****************************************/
using System;
using System.Configuration;
using Proximity.Utility.Configuration;
//****************************************

namespace Proximity.Utility.Logging.Outputs
{
	/// <summary>
	/// Describes the configuration for the XML File Output Logger
	/// </summary>
	public class XmlFileOutputElement : FileOutputElement
	{
		public XmlFileOutputElement()
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets whether to indent the XML output
		/// </summary>
		[ConfigurationProperty("Indent", IsRequired=false, DefaultValue=true)]
		public bool Indent
		{
			get { return (int)base["Indent"]; }
			set { base["Indent"] = value; }
		}
		
		/// <summary>
		/// Gets/Sets the text encoding to use
		/// </summary>
		[ConfigurationProperty("Encoding", IsRequired=false, DefaultValue="UTF-8")]
		public string Encoding
		{
			get { return (string)base["Encoding"]; }
			set { base["Encoding"] = value; }
		}
	}
}
