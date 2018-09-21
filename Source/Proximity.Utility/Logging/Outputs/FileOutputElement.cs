using System;
using System.Configuration;
using System.Security;
using Proximity.Utility.Configuration;
using Proximity.Utility.Logging.Config;
//****************************************

namespace Proximity.Utility.Logging.Outputs
{
	/// <summary>
	/// Describes the configuration for the File Output Logger
	/// </summary>
	public class FileOutputElement : OutputElement
	{
		/// <summary>
		/// Creates a new File Output configuration element
		/// </summary>
		public FileOutputElement()
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets the prefix of the log file name
		/// </summary>
		[ConfigurationProperty("Prefix", IsRequired=true)]
		public string Prefix
		{
			get { return (string)base["Prefix"]; }
			set { base["Prefix"] = value; }
		}
		
		/// <summary>
		/// Gets/Sets the conditions under which the log file rolls over
		/// </summary>
		[ConfigurationProperty("RolloverOn", IsRequired=false, DefaultValue="Startup")]
		public RolloverType RolloverOn
		{
			get { return (RolloverType)base["RolloverOn"]; }
			set { base["RolloverOn"] = value; }
		}
		
		/// <summary>
		/// Gets/Sets the maximum size before the log file rolls over
		/// </summary>
		[ConfigurationProperty("MaximumSize", IsRequired=false, DefaultValue=-1L)]
		public long MaximumSize
		{
			get { return (long)base["MaximumSize"]; }
			set { base["MaximumSize"] = value; }
		}
		
		/// <summary>
		/// Gets/Sets the number of historical log files to keep
		/// </summary>
		/// <remarks>Set to -1 to never delete logs</remarks>
		[ConfigurationProperty("KeepHistory", IsRequired=false, DefaultValue=-1)]
		public int KeepHistory
		{
			get { return (int)base["KeepHistory"]; }
			set { base["KeepHistory"] = value; }
		}
	}
}