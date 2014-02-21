/****************************************\
 AsyncReadWriteLockTests.cs
 Created: 2013-07-26
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
	/// Tests the functionality of the Asynchronous Reader/Writer Lock
	/// </summary>
	[TestFixture]
	public sealed class AsyncReadWriteLockTests
	{
		[Test, Timeout(1000)]
		public async Task SingleRead()
		{
			var MyLock = new AsyncReadWriteLock();
			bool Resource = false;
			
			using (await MyLock.LockRead())
			{
				Resource = true;
			}
			
			Assert.IsTrue(Resource, "Block not entered");
		}
		
		[Test, Timeout(1000)]
		public async Task SingleWrite()
		{
			var MyLock = new AsyncReadWriteLock();
			bool Resource = false;
			
			using (await MyLock.LockWrite())
			{
				Resource = true;
			}
			
			Assert.IsTrue(Resource, "Block not entered");
		}
		
		[Test, Timeout(1000)]
		public async Task ConcurrentRead()
		{
			var MyLock = new AsyncReadWriteLock();
			int Resource = 0;
			
			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						await Task.Yield();
						
						using (await MyLock.LockRead())
						{
							Interlocked.Increment(ref Resource);
						}
						
						return;
					})
			);
			
			Assert.AreEqual(100, Resource, "Block not entered");
		}
		
		[Test] //, Timeout(1000)]
		public async Task ConcurrentReadWrite()
		{
			var MyLock = new AsyncReadWriteLock();
			int Resource = 0;
			
			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						await Task.Yield();
						
						if (count % 2 == 0)
						{
							using (var MyWait = await MyLock.LockWrite())
							{
								Interlocked.Increment(ref Resource);
							}
						}
						else
						{
							using (var MyWait = await MyLock.LockRead())
							{
								Interlocked.Increment(ref Resource);
							}
						}
						
						return;
					})
			);
			
			Assert.AreEqual(100, Resource, "Block not entered");
		}
		
		[Test, Timeout(1000)]
		public async Task ShortReadLongWrite()
		{
			var MyLock = new AsyncReadWriteLock();
			int Resource = 0;
			bool InWrite = false;
			
			await Task.WhenAll(
				Enumerable.Range(1, 8).Select(
					async count =>
					{
						await Task.Yield();
						
						if (count % 2 == 0)
						{
							using (await MyLock.LockWrite())
							{
								InWrite = true;
								
								Thread.Sleep(100);
								
								Interlocked.Increment(ref Resource);
								
								InWrite = false;
							}
						}
						else
						{
							using (await MyLock.LockRead())
							{
								if (InWrite)
									Assert.Fail("Read during write");
								
								Interlocked.Increment(ref Resource);
							}
						}
						
						return;
					})
			);
			
			Assert.AreEqual(8, Resource, "Block not entered");
		}
		
		[Test, Timeout(1000)]
		public async Task ShortWriteLongRead()
		{
			var MyLock = new AsyncReadWriteLock();
			int Resource = 0;
			bool InWrite = false;
			
			await Task.WhenAll(
				Enumerable.Range(1, 8).Select(
					async count =>
					{
						await Task.Yield();
						
						if (count % 2 == 0)
						{
							using (await MyLock.LockWrite())
							{
								InWrite = true;
								
								Thread.Sleep(50);
								
								Interlocked.Increment(ref Resource);
								
								InWrite = false;
							}
						}
						else
						{
							using (await MyLock.LockRead())
							{
								if (InWrite)
									Assert.Fail("Read during write");
								
								Thread.Sleep(100);
								
								Interlocked.Increment(ref Resource);
							}
						}
						
						return;
					})
			);
			
			Assert.AreEqual(8, Resource, "Block not entered");
		}
		
	}
}
