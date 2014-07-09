/****************************************\
 AsyncCounterTests.cs
 Created: 2014-07-09
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
	/// Tests the functionality of the asynchronous counter
	/// </summary>
	[TestFixture]
	public class AsyncCounterTests
	{
		[Test]
		public void Increment()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			MyCounter.Increment();
			
			//****************************************
			
			Assert.AreEqual(1, MyCounter.CurrentCount, "Counter not incremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test]
		public void Decrement()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			MyCounter.Increment();
			
			var MyTask = MyCounter.Decrement();
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to decrement");
			
			Assert.AreEqual(0, MyCounter.CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test]
		public void DecrementInitial()
		{	//****************************************
			var MyCounter = new AsyncCounter(1);
			//****************************************
			
			var MyTask = MyCounter.Decrement();
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to decrement");
			
			Assert.AreEqual(0, MyCounter.CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test]
		public void DecrementIncrement()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			var MyTask = MyCounter.Decrement();
			
			Assert.IsFalse(MyTask.IsCompleted, "Decremented too early");
			
			MyCounter.Increment();
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to decrement");
			
			Assert.AreEqual(0, MyCounter.CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test]
		public void DecrementCancel()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				var MyTask = MyCounter.Decrement(MySource.Token);
			
				Assert.IsFalse(MyTask.IsCompleted, "Decremented too early");
				
				MySource.Cancel();
				
				Assert.IsTrue(MyTask.IsCanceled, "Wait not cancelled");
			}
			
			MyCounter.Increment();
			
			//****************************************
			
			Assert.AreEqual(1, MyCounter.CurrentCount, "Counter still decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test]
		public void DecrementMultiCancel()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				var MyTask1 = MyCounter.Decrement(MySource.Token);
				var MyTask2 = MyCounter.Decrement();
			
				Assert.IsFalse(MyTask1.IsCompleted, "Decremented too early");
				Assert.IsFalse(MyTask2.IsCompleted, "Decremented too early");
				
				MySource.Cancel();
				
				Assert.IsTrue(MyTask1.IsCanceled, "Wait not cancelled");
				Assert.IsFalse(MyTask2.IsCanceled, "Wait cancelled");
				
				MyCounter.Increment();
				
				Assert.IsTrue(MyTask2.IsCompleted, "Still waiting to decrement");
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyCounter.CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public void DecrementTimeout()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			var MyTask = MyCounter.Decrement(TimeSpan.FromMilliseconds(50));
			
			Thread.Sleep(100);
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCanceled, "Wait not cancelled");
		}
		
		[Test, Timeout(1000)]
		public void DecrementTimeoutNone()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			MyCounter.Increment();
			
			var MyTask = MyCounter.Decrement(TimeSpan.FromMilliseconds(50));
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to decrement");
		}
		
		[Test, Timeout(1000)]
		public void DecrementTimeoutDelayed()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			var MyTask = MyCounter.Decrement(TimeSpan.FromMilliseconds(50));
			
			MyCounter.Increment();
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to decrement");
		}
		
		[Test]
		public void Dispose()
		{	//****************************************
			Task MyTask;
			//****************************************
			
			using (var MyCounter = new AsyncCounter())
			{
				MyTask = MyCounter.Decrement();
			}
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCanceled, "Wait not cancelled");
		}
	}
}
