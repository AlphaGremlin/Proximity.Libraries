/****************************************\
 EmitterTests.cs
 Created: 2012-07-17
\****************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Description of EmitterTests.
	/// </summary>
	[TestFixture()]
	public class EmitterTests
	{
		[Test()]
		public void TestEmitDecimalZero()
		{
			var MyEmitter = EmitHelper.FromFunction("TestEmitDecimalZero", null, typeof(decimal));
			
			MyEmitter
				.Ldc(decimal.Zero)
				.Ret
				.End();
			
			var MyMethod = MyEmitter.ToDelegate<Func<decimal>>();
			
			var Result = MyMethod();
			
			//Trace.WriteLine(string.Join("", MyEmitter.Method.GetMethodBody().GetILAsByteArray().Select((inByte) => inByte.ToString("X2"))));
			
			Assert.IsTrue(Result == decimal.Zero, "Result was {0}", Result);
		}
		
		[Test()]
		public void TestEmitDecimalNumber()
		{
			var MyEmitter = EmitHelper.FromFunction("TestEmitDecimalNumber", null, typeof(decimal));
			
			MyEmitter
				.Ldc(105.001m)
				.Ret
				.End();
			
			var MyMethod = MyEmitter.ToDelegate<Func<decimal>>();
			
			var Result = MyMethod();
			
			//Trace.WriteLine(string.Join("", MyEmitter.Method.GetMethodBody().GetILAsByteArray().Select((inByte) => inByte.ToString("X2"))));
			
			Assert.IsTrue(Result == 105.001m, "Result was {0}", Result);
		}
		
		[Test()]
		public void TestEmitDecimalInteger()
		{
			var MyEmitter = EmitHelper.FromFunction("TestEmitDecimalNumber", null, typeof(decimal));
			
			MyEmitter
				.Ldc(100m)
				.Ret
				.End();
			
			var MyMethod = MyEmitter.ToDelegate<Func<decimal>>();
			
			var Result = MyMethod();
			
			//Trace.WriteLine(string.Join("", MyEmitter.Method.GetMethodBody().GetILAsByteArray().Select((inByte) => inByte.ToString("X2"))));
			
			Assert.IsTrue(Result == 100m, "Result was {0}", Result);
		}
	}
}
