using System;
using System.Xml;
using System.Configuration;
using Proximity.Logging.Config;
using System.Security;
//****************************************

namespace Proximity.Logging
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
		public OutputCollection Outputs => (OutputCollection)(this["Outputs"] ?? (this["Outputs"] = new OutputCollection()));
	}
}