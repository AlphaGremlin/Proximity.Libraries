/****************************************\
 WeakEventTests.cs
 Created: 26-08-2010
\****************************************/
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Proximity.Utility.Events;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Tests for the WeakEvent/WeakHandler system
	/// </summary>
	[TestFixture()]
	public class WeakEventTests
	{	//****************************************
		
		[Test()]
		public void RaiseStaticEvent()
		{	//****************************************
			var MyEvent = new WeakEvent<WeakEventArgs>();
			WeakEventArgs MyArgs = new WeakEventArgs();
			//****************************************
			
			MyEvent.Add(OnStaticEventRaise);
						
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			MyEvent.Invoke(this, MyArgs);
			
			Assert.AreEqual(1, MyArgs.InvokeCount, "Static Event Handler was not raised");
		}
		
		[Test()]
		public void RaiseInstanceEvent()
		{	//****************************************
			var MyEvent = new WeakEvent<WeakEventArgs>();
			WeakEventArgs MyArgs = new WeakEventArgs();
			WeakEventInstance MyInstance = new WeakEventInstance();
			//****************************************
			
			MyEvent.Add(MyInstance.OnEventRaise);
			
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			MyEvent.Invoke(this, MyArgs);
			
			GC.KeepAlive(MyInstance); // Required so we don't prematurely garbage collect our Instance
			
			Assert.AreEqual(1, MyArgs.InvokeCount, "Instance Event Handler was not raised");
		}
		
		[Test()]
		public void RaiseLostInstanceEvent()
		{	//****************************************
			var MyEvent = new WeakEvent<WeakEventArgs>();
			WeakEventArgs MyArgs = new WeakEventArgs();
			//****************************************
			
			MyEvent.Add(new WeakEventInstance().OnEventRaise);
			
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			MyEvent.Invoke(this, MyArgs);
			
			Assert.AreEqual(0, MyArgs.InvokeCount, "Instance Event Handler was raised");
		}
		
		[Test()]
		public void CleanupStaticHandler()
		{	//****************************************
			var MyEvent = new WeakEvent<WeakEventArgs>();
			WeakEventArgs MyArgs = new WeakEventArgs();
			//****************************************
			
			MyEvent.Add(OnStaticEventRaise);
			
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			MyEvent.Add(OnStaticEventRaise);
			
			MyEvent.Invoke(this, MyArgs);
			
			//****************************************
			
			Assert.AreEqual(2, MyArgs.InvokeCount, "Static Event Handler was not raised the expected number of times");
		}
		
		[Test()]
		public void CleanupInstanceHandler()
		{	//****************************************
			var MyEvent = new WeakEvent<WeakEventArgs>();
			WeakEventArgs MyArgs = new WeakEventArgs();
			WeakEventInstance MyInstance = new WeakEventInstance();
			//****************************************
			
			MyEvent.Add(MyInstance.OnEventRaise);
			
			MyInstance = new WeakEventInstance();
			
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			MyEvent.Add(MyInstance.OnEventRaise);
			
			MyEvent.Invoke(this, MyArgs);
			
			GC.KeepAlive(MyInstance); // Required so we don't prematurely garbage collect our second Instance
			
			//****************************************
			
			Assert.AreEqual(1, MyArgs.InvokeCount, "Instance Event Handler was raised too many times");
		}
		
		[Test()]
		public void RemoveStaticEvent()
		{	//****************************************
			WeakEvent<WeakEventArgs> MyEvent = new WeakEvent<WeakEventArgs>();
			WeakEventArgs MyArgs = new WeakEventArgs();
			//****************************************
			
			MyEvent.Add(OnStaticEventRaise);
			
			MyEvent.Remove(OnStaticEventRaise);
			
			MyEvent.Invoke(this, MyArgs);
			
			//****************************************
			
			Assert.AreEqual(0, MyArgs.InvokeCount, "Static Event Handler was raised");
		}
		
		[Test()]
		public void RemoveInstanceEvent()
		{	//****************************************
			WeakEvent<WeakEventArgs> MyEvent = new WeakEvent<WeakEventArgs>();
			WeakEventArgs MyArgs = new WeakEventArgs();
			WeakEventInstance MyInstance = new WeakEventInstance();
			//****************************************
			
			MyEvent.Add(MyInstance.OnEventRaise);
			
			MyEvent.Remove(MyInstance.OnEventRaise);
			
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			MyEvent.Invoke(this, MyArgs);
			
			GC.KeepAlive(MyInstance); // Required so we don't prematurely garbage collect our Instance
			
			//****************************************
			
			Assert.AreEqual(0, MyArgs.InvokeCount, "Instance Event Handler was raised");
		}
		
		//****************************************
		
		private static void OnStaticEventRaise(object sender, WeakEventArgs e)
		{
			e.Increment();
		}
		
		private static EventHandler<WeakEventArgs> CreateWeak()
		{
			return WeakEventDelegate.Create((EventHandler<WeakEventArgs>)new WeakEventInstance().OnEventRaise);
		}
		
		//****************************************
				
		private class WeakEventInstance
		{
			public void OnEventRaise(object sender, WeakEventArgs e)
			{
				e.Increment();
			}
		}
	}
}