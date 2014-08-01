/****************************************\
 LoggingConfig.cs
 Created: 2-06-2009
\****************************************/
using System;
using System.Xml;
using System.Configuration;
using Proximity.Utility.Logging.Config;
//****************************************

namespace Proximity.Utility.Logging
{
	/// <summary>
	/// Configuration for the Logging Infrastructure
	/// </summary>
	public class LoggingConfig : ConfigurationSection
	{	//****************************************
		private static LoggingConfig ActiveConfig;
		//****************************************
		
		/// <summary>
		/// Opens an instance of the logging configuration
		/// </summary>
		/// <returns>A single instance of the logging configuration</returns>
		public static LoggingConfig OpenConfig()
		{
			if (ActiveConfig == null)
			{
				ActiveConfig = ConfigurationManager.GetSection("Proximity.Utility.Logging") as LoggingConfig;
				
				if (ActiveConfig == null)
					ActiveConfig = new LoggingConfig();
			}
			
			return ActiveConfig;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the collection of logging outputs
		/// </summary>
		[ConfigurationProperty("Outputs", IsDefaultCollection = false)]
		public OutputCollection Outputs
		{
			get { return (OutputCollection)this["Outputs"] ?? new OutputCollection(); }
		}
		
		/// <summary>
		/// Gets whether to maintain an internal history of logged data
		/// </summary>
		[ConfigurationProperty("MaintainHistory", IsRequired = false, DefaultValue = false)]
		public bool MaintainHistory
		{
			get { return (bool)this["MaintainHistory"]; }
		}
	}
}
