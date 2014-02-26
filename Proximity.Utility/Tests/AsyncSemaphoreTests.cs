/****************************************\
 AsyncReadWriteLockTests.cs
 Created: 2014-02-26
\****************************************/
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Tests the functionality of the Asynchronous Semaphore
	/// </summary>
	[TestFixture]
	public sealed class AsyncSemaphoreTests
	{
		[Test, Timeout(1000), Repeat(10)]
		public async Task SingleLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			bool Resource = false;
			//****************************************
			
			using (await MyLock.Wait())
			{
				Resource = true;
			}
			
			//****************************************
			
			Assert.IsTrue(Resource, "Block not entered");
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task LockContend()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			bool Resource = false;
			Task<IDisposable> MyWaiter;
			//****************************************
			
			using (await MyLock.Wait())
			{
				MyWaiter = MyLock.Wait();
				
				Assert.IsFalse(MyWaiter.IsCompleted, "Nested lock taken");
				
				Resource = true;
			}
			
			//****************************************
			
			Assert.IsTrue(MyWaiter.IsCompleted, "Nested lock not taken");
			
			Assert.IsTrue(Resource, "Block not entered");
			
			MyWaiter.Result.Dispose();
		}
	}
}
