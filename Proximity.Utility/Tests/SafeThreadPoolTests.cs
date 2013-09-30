/****************************************\
 SafeThreadPoolTests.cs
 Created: 2012-07-17
\****************************************/
using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Tests for the SafeThreadPool
	/// </summary>
	[TestFixture()]
	public class SafeThreadPoolTests : IExpectException
	{
		[TestFixtureSetUp()]
		public void Setup()
		{
			int WorkerThreads, IOThreads;
			
			ThreadPool.GetMinThreads(out WorkerThreads, out IOThreads);
			ThreadPool.SetMinThreads(Math.Max(Environment.ProcessorCount, WorkerThreads), IOThreads);
		}
		
		[Test()]
		public void EnumerateArray()
		{	//****************************************
			var MyCollection = new int[10000];
			var Counter = 0;
			//****************************************
			
			Populate(MyCollection);
			
			SafeThreadPool.ForEach(
				MyCollection,
				delegate(int value)
				{
					Interlocked.Increment(ref Counter);
				}
			);
			
			Assert.AreEqual(MyCollection.Length, Counter, "Thread Pool ForEach did not enumerate correctly");
		}
		
		[Test()]
		public void EnumerateArrayWithWaiting()
		{	//****************************************
			var MyCollection = new int[1000];
			var Counter = 0;
			//****************************************
			
			Populate(MyCollection);
			
			SafeThreadPool.ForEach(
				MyCollection,
				delegate(int value)
				{
					if ((value % 3) == 0)
						SpinWait(50);
					else if ((value % 2) == 0)
						Thread.Sleep(50);
					
					Interlocked.Increment(ref Counter);
				}
			);
			
			Assert.AreEqual(MyCollection.Length, Counter, "Thread Pool ForEach did not enumerate correctly");
		}
		
		[Test()]
		public void EnumerateList()
		{	//****************************************
			var MyCollection = new List<int>(10000);
			var Counter = 0;
			//****************************************
			
			Populate(MyCollection);
			
			SafeThreadPool.ForEach(
				MyCollection,
				delegate(int value)
				{
					Interlocked.Increment(ref Counter);
				}
			);
			
			Assert.AreEqual(MyCollection.Count, Counter, "Thread Pool ForEach did not enumerate correctly");
		}
		
		[Test()]
		public void EnumerateNested()
		{	//****************************************
			var MyCollection = new int[100];
			int InnerCounter = 0, OuterCounter = 0;
			//****************************************
			
			Populate(MyCollection);
			
			SafeThreadPool.ForEach(
				MyCollection,
				delegate(int value)
				{
					Interlocked.Increment(ref OuterCounter);
					
					var MyInnerCollection = new int[100];
					
					SafeThreadPool.ForEach(
						MyInnerCollection,
						delegate(int innerValue)
						{
							Interlocked.Increment(ref InnerCounter);
						}
					);
				}
			);
			
			Assert.AreEqual(MyCollection.Length, OuterCounter, "Thread Pool ForEach did not enumerate outer correctly");
			
			Assert.AreEqual(100 * 100, InnerCounter, "Thread Pool ForEach did not enumerate inner correctly");
		}
		
		[Test()]
		public void EnumerateNestedWithWaiting()
		{	//****************************************
			var MyCollection = new int[32];
			int InnerCounter = 0, OuterCounter = 0;
			//****************************************
			
			Populate(MyCollection);
			
			SafeThreadPool.ForEach(
				MyCollection,
				delegate(int value)
				{
					Interlocked.Increment(ref OuterCounter);
					
					var MyInnerCollection = new int[32];
					
					SafeThreadPool.ForEach(
						MyInnerCollection,
						delegate(int innerValue)
						{
							if ((value % 3) == 0)
								SpinWait(50);
							else if ((value % 2) == 0)
								Thread.Sleep(50);
							
							Interlocked.Increment(ref InnerCounter);
						}
					);
				}
			);
			
			Assert.AreEqual(MyCollection.Length, OuterCounter, "Thread Pool ForEach did not enumerate outer correctly");
			
			Assert.AreEqual(32 * 32, InnerCounter, "Thread Pool ForEach did not enumerate inner correctly");
		}
		
		[Test(), ExpectedException(typeof(AggregateException))]
		public void EnumerateExceptionThrown()
		{	//****************************************
			var MyCollection = new int[10000];
			var Counter = 0;
			//****************************************
			
			Populate(MyCollection);
			
			SafeThreadPool.ForEach(
				MyCollection,
				delegate(int value)
				{
					if (Interlocked.Increment(ref Counter) % 10 == 0)
						throw new ApplicationException();
				}
			);
		}
		
		[Test()]
		public void EnumerateExceptionCaught()
		{	//****************************************
			var MyCollection = new int[10000];
			var Counter = 0;
			//****************************************
			
			Populate(MyCollection);
			
			try
			{
				SafeThreadPool.ForEach(
					MyCollection,
					delegate(int value)
					{
						if (Interlocked.Increment(ref Counter) % 10 == 0)
							throw new ApplicationException();
					}
				);
			}
			catch (AggregateException e)
			{
				Assert.AreEqual(MyCollection.Length / 10, e.InnerExceptions.Count, "Thread Pool ForEach did not catch exceptions correctly");
			}
			
			Assert.AreEqual(MyCollection.Length, Counter, "Thread Pool ForEach did not enumerate correctly");
		}
		
		[Test()]
		public void EnumerateMinimum()
		{	//****************************************
			var MyCollection = new List<int>(500);
			var Counter = 0;
			var Concurrent = 0;
			bool MaxThreadsExceeded = false;
			//****************************************
			
			Populate(MyCollection);
			
			SafeThreadPool.ForEach(
				MyCollection,
				delegate(int value)
				{
					Interlocked.Increment(ref Counter);
					
					if (Interlocked.Increment(ref Concurrent) > 2)
						MaxThreadsExceeded = true;
					
					Thread.Sleep(50);
					
					Interlocked.Decrement(ref Concurrent);
				},
				2 // Maximum of 2 threads
			);
			
			Assert.AreEqual(MyCollection.Count, Counter, "Thread Pool ForEach did not enumerate correctly");
			Assert.IsFalse(MaxThreadsExceeded, "Thread Pool ForEach exceeded thread limit");
		}
		
		//****************************************
		
		void IExpectException.HandleException(Exception e)
		{
			
		}
		
		//****************************************
		
		private void Populate(int[] array)
		{	//****************************************
			var MyRandom = new Random();
			//****************************************
			
			for (int Index = 0; Index < array.Length; Index++)
			{
				array[Index] = MyRandom.Next();
			}
		}
		
		private void Populate(List<int> list)
		{	//****************************************
			var MyRandom = new Random();
			//****************************************
			
			for (int Index = 0; Index < list.Capacity; Index++)
			{
				list.Add(MyRandom.Next());
			}
		}
		
		private void SpinWait(int milliseconds)
		{	//****************************************
			DateTime EndTime = DateTime.Now.AddMilliseconds(milliseconds);
			//****************************************
			
			while (DateTime.Now < EndTime)
				Thread.SpinWait(10);
		}
	}
}
