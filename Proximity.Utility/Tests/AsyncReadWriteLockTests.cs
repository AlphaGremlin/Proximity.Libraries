/****************************************\
 AsyncReadWriteLockTests.cs
 Created: 2014-02-21
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
		{	//****************************************
			var MyLock = new AsyncReadWriteLock();
			bool Resource = false;
			//****************************************
			
			using (await MyLock.LockRead())
			{
				Resource = true;
			}
			
			//****************************************
			
			Assert.IsTrue(Resource, "Block not entered");
			
			Assert.IsFalse(MyLock.IsReading, "Reader still registered");
			Assert.IsFalse(MyLock.IsWriting, "Writer still registered");
			Assert.AreEqual(0, MyLock.WaitingReaders, "Readers still waiting");
			Assert.AreEqual(0, MyLock.WaitingWriters, "Writers still waiting");
		}
		
		[Test, Timeout(1000)]
		public async Task SingleWrite()
		{	//****************************************
			var MyLock = new AsyncReadWriteLock();
			bool Resource = false;
			//****************************************
			
			using (await MyLock.LockWrite())
			{
				Resource = true;
			}
			
			//****************************************
			
			Assert.IsTrue(Resource, "Block not entered");
			
			Assert.IsFalse(MyLock.IsReading, "Reader still registered");
			Assert.IsFalse(MyLock.IsWriting, "Writer still registered");
			Assert.AreEqual(0, MyLock.WaitingReaders, "Readers still waiting");
			Assert.AreEqual(0, MyLock.WaitingWriters, "Writers still waiting");
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task ConcurrentRead()
		{	//****************************************
			var MyLock = new AsyncReadWriteLock();
			int Resource = 0;
			//****************************************
			
			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise
						
						using (await MyLock.LockRead())
						{
							Interlocked.Increment(ref Resource);
						}
						
						return;
					})
			);
			
			//****************************************
			
			Assert.AreEqual(100, Resource, "Block not entered");
			
			Assert.IsFalse(MyLock.IsReading, "Reader still registered");
			Assert.IsFalse(MyLock.IsWriting, "Writer still registered");
			Assert.AreEqual(0, MyLock.WaitingReaders, "Readers still waiting");
			Assert.AreEqual(0, MyLock.WaitingWriters, "Writers still waiting");
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task ConcurrentReadWrite()
		{	//****************************************
			var MyLock = new AsyncReadWriteLock();
			int Resource = 0;
			//****************************************
			
			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise
						
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
			
			//****************************************
			
			Assert.AreEqual(100, Resource, "Block not entered");
			
			Assert.IsFalse(MyLock.IsReading, "Reader still registered");
			Assert.IsFalse(MyLock.IsWriting, "Writer still registered");
			Assert.AreEqual(0, MyLock.WaitingReaders, "Readers still waiting");
			Assert.AreEqual(0, MyLock.WaitingWriters, "Writers still waiting");
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task ShortReadLongWrite()
		{	//****************************************
			var MyLock = new AsyncReadWriteLock();
			int Resource = 0;
			bool InWrite = false;
			//****************************************
			
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
			
			//****************************************
			
			Assert.AreEqual(8, Resource, "Block not entered");
			
			Assert.IsFalse(MyLock.IsReading, "Reader still registered");
			Assert.IsFalse(MyLock.IsWriting, "Writer still registered");
			Assert.AreEqual(0, MyLock.WaitingReaders, "Readers still waiting");
			Assert.AreEqual(0, MyLock.WaitingWriters, "Writers still waiting");
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task ShortWriteLongRead()
		{	//****************************************
			var MyLock = new AsyncReadWriteLock();
			int Resource = 0;
			bool InWrite = false;
			//****************************************
			
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
			
			//****************************************
			
			Assert.AreEqual(8, Resource, "Block not entered");
			
			Assert.IsFalse(MyLock.IsReading, "Reader still registered");
			Assert.IsFalse(MyLock.IsWriting, "Writer still registered");
			Assert.AreEqual(0, MyLock.WaitingReaders, "Readers still waiting");
			Assert.AreEqual(0, MyLock.WaitingWriters, "Writers still waiting");
		}
		
		[Test, Timeout(1000)]
		public async Task WriteCancelRead()
		{	//****************************************
			var MyLock = new AsyncReadWriteLock();
			Task<IDisposable> MyReader;
			//****************************************
			
			var MyWriter = await MyLock.LockWrite();
			
			using (var MySource = new CancellationTokenSource())
			{
				MyReader = MyLock.LockRead(MySource.Token);
				
				MySource.Cancel();
				
				MyWriter.Dispose();
			}
			
			//****************************************
			
			Assert.IsTrue(MyReader.IsCanceled, "Did not cancel");
			
			Assert.IsFalse(MyLock.IsReading, "Reader still registered");
			Assert.IsFalse(MyLock.IsWriting, "Writer still registered");
			Assert.AreEqual(0, MyLock.WaitingReaders, "Readers still waiting");
			Assert.AreEqual(0, MyLock.WaitingWriters, "Writers still waiting");
		}
		
		[Test, Timeout(1000)]
		public async Task WriteCancelReadMulti()
		{	//****************************************
			var MyLock = new AsyncReadWriteLock();
			Task<IDisposable> MyReader1, MyReader2;
			//****************************************
			
			var MyWriter = await MyLock.LockWrite();
			
			using (var MySource = new CancellationTokenSource())
			{
				MyReader1 = MyLock.LockRead(MySource.Token);
				MyReader2 = MyLock.LockRead();
				
				MySource.Cancel();
				
				MyWriter.Dispose();
			}
			
			//****************************************
			
			Assert.IsTrue(MyReader1.IsCanceled, "Did not cancel");
			Assert.IsFalse(MyReader2.IsCanceled, "Cancelled");
			
			MyReader2.Result.Dispose();
			
			Assert.IsFalse(MyLock.IsReading, "Reader still registered");
			Assert.IsFalse(MyLock.IsWriting, "Writer still registered");
			Assert.AreEqual(0, MyLock.WaitingReaders, "Readers still waiting");
			Assert.AreEqual(0, MyLock.WaitingWriters, "Writers still waiting");
		}
		
		[Test, Timeout(1000)]
		public async Task ReadCancelWrite()
		{	//****************************************
			var MyLock = new AsyncReadWriteLock();
			Task<IDisposable> MyWriter;
			//****************************************
			
			var MyReader = await MyLock.LockRead();
			
			using (var MySource = new CancellationTokenSource())
			{
				MyWriter = MyLock.LockWrite(MySource.Token);
				
				MySource.Cancel();
				
				MyReader.Dispose();
			}
			
			//****************************************
			
			Thread.Sleep(100); // Release happens on another thread and there's nothing to wait on
			
			Assert.IsTrue(MyWriter.IsCanceled, "Did not cancel");
			
			Assert.IsFalse(MyLock.IsReading, "Reader still registered");
			Assert.IsFalse(MyLock.IsWriting, "Writer still registered");
			Assert.AreEqual(0, MyLock.WaitingReaders, "Readers still waiting");
			Assert.AreEqual(0, MyLock.WaitingWriters, "Writers still waiting");
		}
		
		[Test, Timeout(1000)]
		public async Task ReadCancelWriteMulti()
		{	//****************************************
			var MyLock = new AsyncReadWriteLock();
			Task<IDisposable> MyWriter1, MyWriter2;
			//****************************************
			
			var MyReader = await MyLock.LockRead();
			
			using (var MySource = new CancellationTokenSource())
			{
				MyWriter1 = MyLock.LockWrite(MySource.Token);
				MyWriter2 = MyLock.LockWrite();
				
				MySource.Cancel();
				
				MyReader.Dispose();
			}
			
			//****************************************
			
			Assert.IsTrue(MyWriter1.IsCanceled, "Did not cancel");
			Assert.IsFalse(MyWriter2.IsCanceled, "Cancelled");
			
			MyWriter2.Result.Dispose();
			
			Assert.IsFalse(MyLock.IsReading, "Reader still registered");
			Assert.IsFalse(MyLock.IsWriting, "Writer still registered");
			Assert.AreEqual(0, MyLock.WaitingReaders, "Readers still waiting");
			Assert.AreEqual(0, MyLock.WaitingWriters, "Writers still waiting");
		}
		
		[Test, Timeout(1000)]
		public async Task WriteTimeoutRead()
		{	//****************************************
			var MyLock = new AsyncReadWriteLock();
			Task<IDisposable> MyReader;
			//****************************************
			
			using (await MyLock.LockWrite())
			{
				MyReader = MyLock.LockRead(TimeSpan.FromMilliseconds(50));
				
				Thread.Sleep(100);
			}
			
			//****************************************
			
			Assert.IsTrue(MyReader.IsCanceled, "Did not cancel");
			
			Assert.IsFalse(MyLock.IsReading, "Reader still registered");
			Assert.IsFalse(MyLock.IsWriting, "Writer still registered");
			Assert.AreEqual(0, MyLock.WaitingReaders, "Readers still waiting");
			Assert.AreEqual(0, MyLock.WaitingWriters, "Writers still waiting");
		}
		
		[Test, Timeout(1000)]
		public async Task ReadTimeoutWrite()
		{	//****************************************
			var MyLock = new AsyncReadWriteLock();
			Task<IDisposable> MyWriter;
			//****************************************
			
			using (await MyLock.LockRead())
			{
				MyWriter = MyLock.LockWrite(TimeSpan.FromMilliseconds(50));
				
				Thread.Sleep(100);
			}
			
			//****************************************
			
			Thread.Sleep(100); // Release happens on another thread and there's nothing to wait on
			
			Assert.IsTrue(MyWriter.IsCanceled, "Did not cancel");
			
			Assert.IsFalse(MyLock.IsReading, "Reader still registered");
			Assert.IsFalse(MyLock.IsWriting, "Writer still registered");
			Assert.AreEqual(0, MyLock.WaitingReaders, "Readers still waiting");
			Assert.AreEqual(0, MyLock.WaitingWriters, "Writers still waiting");
		}
		
		[Test, Timeout(1000)]
		public void NoTimeoutRead()
		{	//****************************************
			var MyLock = new AsyncReadWriteLock();
			Task<IDisposable> MyReader;
			//****************************************
			
			MyReader = MyLock.LockRead(TimeSpan.FromMilliseconds(50));
			
			//****************************************
			
			Assert.IsTrue(MyReader.IsCompleted, "Is not completed");
			
			MyReader.Result.Dispose();
			
			Assert.IsFalse(MyLock.IsReading, "Reader still registered");
			Assert.IsFalse(MyLock.IsWriting, "Writer still registered");
			Assert.AreEqual(0, MyLock.WaitingReaders, "Readers still waiting");
			Assert.AreEqual(0, MyLock.WaitingWriters, "Writers still waiting");
		}
		
		[Test, Timeout(1000)]
		public void NoTimeoutWrite()
		{	//****************************************
			var MyLock = new AsyncReadWriteLock();
			Task<IDisposable> MyWriter;
			//****************************************
			
			MyWriter = MyLock.LockWrite(TimeSpan.FromMilliseconds(50));
			
			//****************************************
			
			Assert.IsTrue(MyWriter.IsCompleted, "Is not completed");
			
			MyWriter.Result.Dispose();
			
			Assert.IsFalse(MyLock.IsReading, "Reader still registered");
			Assert.IsFalse(MyLock.IsWriting, "Writer still registered");
			Assert.AreEqual(0, MyLock.WaitingReaders, "Readers still waiting");
			Assert.AreEqual(0, MyLock.WaitingWriters, "Writers still waiting");
		}
	}
}
