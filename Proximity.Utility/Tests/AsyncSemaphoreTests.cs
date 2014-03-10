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
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task MultiContend()
		{	//****************************************
			var MyLock = new AsyncSemaphore(2);
			bool Resource = false;
			Task<IDisposable> MyWaiter1, MyWaiter2;
			//****************************************
			
			using (await MyLock.Wait())
			{
				MyWaiter1 = MyLock.Wait();
				
				MyWaiter2 = MyLock.Wait();
				
				Assert.IsTrue(MyWaiter1.IsCompleted, "Nested lock not taken");
				
				Assert.IsFalse(MyWaiter2.IsCompleted, "Nested lock taken");
				
				Resource = true;
			}
			
			//****************************************
			
			Assert.IsTrue(MyWaiter2.IsCompleted, "Nested lock not taken");
			
			Assert.IsTrue(Resource, "Block not entered");
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task MultiContendCancel()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			bool Resource = false;
			Task<IDisposable> MyWaiter1, MyWaiter2;
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				using (await MyLock.Wait())
				{
					MyWaiter1 = MyLock.Wait(MySource.Token);
					
					MyWaiter2 = MyLock.Wait();
					
					MySource.Cancel();
					
					Assert.IsTrue(MyWaiter1.IsCanceled, "NWait cancelled");
					
					Assert.IsFalse(MyWaiter2.IsCompleted, "Nested lock taken");
					
					Resource = true;
				}
			}
			
			//****************************************
			
			Assert.IsTrue(MyWaiter2.IsCompleted, "Nested lock not taken");
			
			Assert.IsTrue(Resource, "Block not entered");
		}
		
		[Test, Timeout(1000)]
		public async Task LockContendTimeout()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			Task<IDisposable> MyWait;
			//****************************************
			
			using (await MyLock.Wait())
			{
				MyWait = MyLock.Wait(TimeSpan.FromMilliseconds(50));
				
				Thread.Sleep(100);
			}
			
			//****************************************
			
			Assert.IsTrue(MyWait.IsCanceled, "Did not cancel");
		}
		
		[Test, Timeout(1000)]
		public void NoTimeout()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			Task<IDisposable> MyWait;
			//****************************************
			
			MyWait = MyLock.Wait(TimeSpan.FromMilliseconds(50));
			
			//****************************************
			
			Assert.IsTrue(MyWait.IsCompleted, "Is not completed");
		}
	}
}
