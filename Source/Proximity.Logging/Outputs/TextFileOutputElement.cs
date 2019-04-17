using System;
using System.Configuration;
using System.Security;
using Proximity.Utility.Configuration;
//****************************************

namespace Proximity.Logging.Outputs
{
	/// <summary>
	/// Describes the configuration for the Text File Output Logger
	/// </summary>
	[TypedElement(typeof(TextFileOutput))]
	public class TextFileOutputElement : FileOutputElement
	{
		/// <summary>
		/// Creates a new Text File Output configuration element
		/// </summary>
		public TextFileOutputElement()
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets whether to indent with tabs rather than spaces
		/// </summary>
		[ConfigurationProperty("IndentTabs", IsRequired=false, DefaultValue=true)]
		public bool IndentTabs
		{
			get { return (bool)base["IndentTabs"]; }
			set { base["IndentTabs"] = value; }
		}
		
		/// <summary>
		/// Gets/Sets the size of each indent level when indenting with spaces
		/// </summary>
		[ConfigurationProperty("IndentSize", IsRequired=false, DefaultValue=2)]
		public int IndentSize
		{
			get { return (int)base["IndentSize"]; }
			set { base["IndentSize"] = value; }
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