using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Proximity.Logging;
using Proximity.Logging.Outputs;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Description of LoggingTests.
	/// </summary>
	[TestFixture]
	public class LoggingTests
	{
		[Test, Repeat(10)]
		public void StartFinish()
		{
			var LogFileName = Path.GetTempFileName();
			
			try
			{
				var MyConfig = new LoggingConfig();
				MyConfig.Outputs.Add(new TextFileOutputElement() { RolloverOn = RolloverType.Fixed, Prefix = LogFileName });
				
				LogManager.Start(MyConfig);
				
				Log.Info("Test Entry");
				
				ThreadPool.QueueUserWorkItem((state) => Log.Info("Test Again"));
				
				LogManager.Finish();
			}
			finally
			{
				File.Delete(LogFileName);
			}
			
			GC.Collect();
		}
	}
}
