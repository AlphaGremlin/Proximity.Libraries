/****************************************\
 TaskStreamTests.cs
 Created: 2013-07-26
\****************************************/
using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Description of TaskStreamTests.
	/// </summary>
	[TestFixture()]
	public class TaskStreamTests
	{	//****************************************
		private int _WaitTime;
		private int _Step1, _Step2, _Step3, _Step4;
		private int _Step1Value, _Step2Value, _Step3Value, _Step4Value;
		
		private CancellationTokenSource _CancelSource;
		//****************************************
		
		public TaskStreamTests()
		{
		}
		
		//****************************************
		
		[SetUp()]
		public void SetupTest()
		{
			_Step1 = 0;
			_Step1Value = 0;
			
			_Step2 = 0;
			_Step2Value = 0;
			
			_Step3 = 0;
			_Step3Value = 0;
			
			_Step4 = 0;
			_Step4Value = 0;
			
			_WaitTime = 100;
		}
		
		[TearDown()]
		public void CleanupTest()
		{
			if (_CancelSource != null)
			{
				_CancelSource.Dispose();
				
				_CancelSource = null;
			}
		}
		
		//****************************************
		
		[Test()]
		public void TestQueue()
		{
			var MyQueue = new TaskStream();
			
			var MyTask = MyQueue.Queue(OnStep1);
			
			Assert.IsTrue(MyTask.Wait(1000), "Timed out");
			
			Assert.AreEqual(1, _Step1, "Step 1 was executed {0} times", _Step1);
		}
		
		[Test()]
		public void TestQueueLonger()
		{
			var MyQueue = new TaskStream();
			Task LatestTask = null;
			
			_WaitTime = 10;
			
			for (int Index = 0; Index < 100; Index++)
				LatestTask = MyQueue.Queue(OnStep1);
			
			Assert.IsTrue(LatestTask.Wait(2000), "Timed out");
			
			Assert.AreEqual(100, _Step1, "Step 1 was executed {0} times", _Step1);
		}
		
		[Test()]
		public void TestQueueCancel()
		{
			var MyQueue = new TaskStream();
			
			_WaitTime = 1000;
			_CancelSource = new CancellationTokenSource();
			
			var MyTask = MyQueue.Queue(OnStep3, _CancelSource.Token);
			
			Thread.Sleep(100);
			
			_CancelSource.Cancel();
			
			while (!MyTask.IsCompleted)
			{
				Thread.Sleep(100);
			}
			
			Assert.IsFalse(MyTask.IsFaulted, "Task faulted: {0}", MyTask.Exception);
			Assert.IsTrue(MyTask.IsCanceled, "Task did not cancel");
			
			Assert.AreEqual(0, _Step3, "Step 3 was executed {0} times", _Step3);
		}
		
		[Test()]
		public void TestQueueLongerCancel()
		{
			var MyQueue = new TaskStream();
			
			_WaitTime = 500;
			_CancelSource = new CancellationTokenSource();
			
			var MyFirstTask = MyQueue.Queue(OnStep1);
			var MySecondTask = MyQueue.Queue(OnStep3, _CancelSource.Token);
			
			Thread.Sleep(100);
			
			_CancelSource.Cancel();
			
			while (!MyFirstTask.IsCompleted || !MySecondTask.IsCompleted)
			{
				Thread.Sleep(100);
			}
			
			Assert.IsFalse(MySecondTask.IsFaulted, "Task faulted: {0}", MySecondTask.Exception);
			Assert.IsTrue(MySecondTask.IsCanceled, "Task did not cancel");
			
			Assert.AreEqual(1, _Step1, "Step 1 was executed {0} times", _Step1);
			Assert.AreEqual(0, _Step3, "Step 3 was executed {0} times", _Step3);
		}
		
		[Test()]
		public void TestValue()
		{
			var MyQueue = new TaskStream();
			
			var MyTask = MyQueue.Queue(OnStep1Param, 42);
			
			Assert.IsTrue(MyTask.Wait(1000), "Timed out");
			
			Assert.AreEqual(1, _Step1, "Step 1 was executed {0} times", _Step1);
			Assert.AreEqual(42, _Step1Value, "Step 1 value is {0}", _Step1Value);
		}
		
		[Test()]
		public void TestValueLonger()
		{
			var MyQueue = new TaskStream();
			
			_WaitTime = 10;
			
			for (int Index = 0; Index < 100; Index++)
				MyQueue.Queue(OnStep1Param, 30);
			
			var MyTask = MyQueue.Queue(OnStep3Param, 42);
			
			Assert.IsTrue(MyTask.Wait(2000), "Timed out");
			
			Assert.AreEqual(100, _Step1, "Step 1 was executed {0} times", _Step1);
			Assert.AreEqual(30, _Step1Value, "Step 1 value is {0}", _Step1Value);
			Assert.AreEqual(1, _Step3, "Step 2 was executed {0} times", _Step2);
			Assert.AreEqual(42, _Step3Value, "Step 2 value is {0}", _Step2Value);
		}
		
		[Test()]
		public void TestValueCancel()
		{
			var MyQueue = new TaskStream();
			
			_WaitTime = 1000;
			_CancelSource = new CancellationTokenSource();
			
			var MyTask = MyQueue.Queue(OnStep3Param, 42, _CancelSource.Token);
			
			Thread.Sleep(100);
			
			_CancelSource.Cancel();
			
			while (!MyTask.IsCompleted)
			{
				Thread.Sleep(100);
			}
			
			Assert.IsFalse(MyTask.IsFaulted, "Task faulted: {0}", MyTask.Exception);
			Assert.IsTrue(MyTask.IsCanceled, "Task did not cancel");
			
			Assert.AreEqual(0, _Step3, "Step 3 was executed {0} times", _Step3);
			Assert.AreEqual(0, _Step3Value, "Step 3 value is {0}", _Step3Value);
		}
		
		[Test()]
		public void TestValueLongerCancel()
		{
			var MyQueue = new TaskStream();
			
			_WaitTime = 500;
			_CancelSource = new CancellationTokenSource();
			
			var MyFirstTask = MyQueue.Queue(OnStep1Param, 42);
			var MySecondTask = MyQueue.Queue(OnStep3Param, 42, _CancelSource.Token);
			
			Thread.Sleep(100);
			
			_CancelSource.Cancel();
			
			while (!MyFirstTask.IsCompleted || !MySecondTask.IsCompleted)
			{
				Thread.Sleep(100);
			}
			
			Assert.IsFalse(MySecondTask.IsFaulted, "Task faulted: {0}", MySecondTask.Exception);
			Assert.IsTrue(MySecondTask.IsCanceled, "Task did not cancel");
			
			Assert.AreEqual(1, _Step1, "Step 1 was executed {0} times", _Step1);
			Assert.AreEqual(42, _Step1Value, "Step 1 value is {0}", _Step1Value);
			Assert.AreEqual(0, _Step3, "Step 3 was executed {0} times", _Step3);
			Assert.AreEqual(0, _Step3Value, "Step 3 value is {0}", _Step3Value);
		}
		
		[Test()]
		public void TestResult()
		{
			var MyQueue = new TaskStream();
			
			var MyTask = MyQueue.Queue<int>(OnStep4);
			
			Assert.IsTrue(MyTask.Wait(1000), "Timed out");
			
			Assert.AreEqual(1, _Step4, "Step 4 was executed {0} times", _Step4);
			Assert.AreEqual(42, MyTask.Result, "Step 4 result is {0}", MyTask.Result);
		}
		
		[Test()]
		public void TestResultLonger()
		{
			var MyQueue = new TaskStream();
			Task<int> LatestTask = null;
			
			_WaitTime = 10;
			
			for (int Index = 0; Index < 100; Index++)
				LatestTask = MyQueue.Queue<int>(OnStep4);
			
			Assert.IsTrue(LatestTask.Wait(2000), "Timed out");
			
			Assert.AreEqual(100, _Step4, "Step 4 was executed {0} times", _Step4);
			Assert.AreEqual(42, LatestTask.Result, "Step 4 result is {0}", LatestTask.Result);
		}
		
		[Test()]
		public void TestTask()
		{
			var MyQueue = new TaskStream();
			
			var MyTask = MyQueue.QueueTask(OnStep2);
			
			Assert.IsTrue(MyTask.Wait(1000), "Timed out");
			
			Assert.AreEqual(1, _Step2, "Step 2 was executed {0} times", _Step2);
		}
		
		[Test()]
		public void TestTaskValue()
		{
			var MyQueue = new TaskStream();
			
			var MyTask = MyQueue.QueueTask(OnStep2Param, 42);
			
			Assert.IsTrue(MyTask.Wait(1000), "Timed out");
			
			Assert.AreEqual(1, _Step2, "Step 2 was executed {0} times", _Step2);
			Assert.AreEqual(42, _Step2Value, "Step 2 value is {0}", _Step2Value);
		}
		
		[Test()]
		public void TestComplete()
		{
			var MyQueue = new TaskStream();
			
			var MyTask = MyQueue.QueueTask(OnStep2);
			
			Assert.IsTrue(MyQueue.Complete().Wait(1000), "Timed out");
			Assert.IsTrue(MyTask.IsCompleted, "Task did not complete");
			
			Assert.AreEqual(1, _Step2, "Step 2 was executed {0} times", _Step2);
		}
		
		[Test()]
		public void TestCompleteLonger()
		{
			var MyQueue = new TaskStream();
			Task LatestTask = null;
			
			_WaitTime = 10;
			
			for (int Index = 0; Index < 100; Index++)
				LatestTask = MyQueue.QueueTask(OnStep2);
			
			Assert.IsTrue(MyQueue.Complete().Wait(2000), "Timed out");
			Assert.IsTrue(LatestTask.IsCompleted, "Task did not complete");
			
			Assert.AreEqual(100, _Step2, "Step 2 was executed {0} times", _Step2);
		}
		
		//****************************************
		
		private void OnStep1()
		{
			Thread.Sleep(_WaitTime);
			
			Interlocked.Increment(ref _Step1);
		}
		
		private void OnStep1Param(int value)
		{
			Thread.Sleep(_WaitTime);
			
			Interlocked.Increment(ref _Step1);
			_Step1Value = value;
		}
		
		private async Task OnStep2()
		{
			await Task.Yield();
			
			await Task.Delay(_WaitTime);
			
			Interlocked.Increment(ref _Step2);
		}
		
		private async Task OnStep2Param(int value)
		{
			await Task.Yield();
			
			await Task.Delay(_WaitTime);
			
			Interlocked.Increment(ref _Step2);
			_Step2Value = value;
		}
		
		private void OnStep3()
		{
			int TimeWaited = 0;
			
			while (TimeWaited < _WaitTime)
			{
				Thread.Sleep(10);
				
				if (_CancelSource != null)
					_CancelSource.Token.ThrowIfCancellationRequested();
				
				TimeWaited += 10;
			}
			
			Interlocked.Increment(ref _Step3);
		}
		
		private void OnStep3Param(int value)
		{
			int TimeWaited = 0;
			
			while (TimeWaited < _WaitTime)
			{
				Thread.Sleep(10);
				
				if (_CancelSource != null)
					_CancelSource.Token.ThrowIfCancellationRequested();
				
				TimeWaited += 10;
			}
			
			Interlocked.Increment(ref _Step3);
			_Step3Value = value;
		}
		
		private int OnStep4()
		{
			Thread.Sleep(_WaitTime);
			
			Interlocked.Increment(ref _Step4);
			
			return 42;
		}
		
		private int OnStep4Param(int value)
		{
			Thread.Sleep(_WaitTime);
			
			Interlocked.Increment(ref _Step4);
			_Step4Value = value;
			
			return 42;
		}
	}
}
