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
		
		//****************************************
		
		[SetUp()]
		public void SetupTest()
		{
			//WeakDelegate<WeakEventArgs>.ClearInvokerCache();
		}
		
		//****************************************
		
		[Test()]
		public void RaiseStaticEvent()
		{	//****************************************
			WeakEvent<WeakEventArgs> MyEvent = new WeakEvent<WeakEventArgs>();
			WeakEventArgs MyArgs = new WeakEventArgs();
			//****************************************
			
			MyEvent.Add(OnStaticEventRaise);
						
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			MyEvent.Invoke(this, MyArgs);
			
			Assert.IsTrue(MyArgs.WasInvoked, "Static Event Handler was not raised");
		}
		
		[Test()]
		public void RaiseInstanceEvent()
		{	//****************************************
			WeakEvent<WeakEventArgs> MyEvent = new WeakEvent<WeakEventArgs>();
			WeakEventArgs MyArgs = new WeakEventArgs();
			WeakEventInstance MyInstance = new WeakEventInstance();
			//****************************************
			
			MyEvent.Add(MyInstance.OnEventRaise);
						
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			MyEvent.Invoke(this, MyArgs);
			
			GC.KeepAlive(MyInstance); // Required so we don't prematurely garbage collect our Instance
			
			Assert.IsTrue(MyArgs.WasInvoked, "Instance Event Handler was not raised");
		}
		
		[Test()]
		public void RaiseLostInstanceEvent()
		{	//****************************************
			WeakEvent<WeakEventArgs> MyEvent = new WeakEvent<WeakEventArgs>();
			WeakEventArgs MyArgs = new WeakEventArgs();
			//****************************************
			
			MyEvent.Add(new WeakEventInstance().OnEventRaise);
			
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			MyEvent.Invoke(this, MyArgs);
			
			Assert.IsFalse(MyArgs.WasInvoked, "Instance Event Handler was raised");
		}
		/*
		[Test()]
		public void RaiseStaticHandler()
		{	//****************************************
			EventHandler<WeakEventArgs> MyWeakEventHandler;
			UnregisterCallback<WeakEventArgs> MyUnregisterCallback;
			WeakEventArgs MyArgs = new WeakEventArgs();
			bool WasUnregistered = false;
			//****************************************
			
			MyUnregisterCallback = delegate(EventHandler<WeakEventArgs> eventHandler)
			{
				WasUnregistered = true;
			};
			
			MyWeakEventHandler = new WeakDelegate<WeakEventArgs>((EventHandler<WeakEventArgs>)OnStaticEventRaise, MyUnregisterCallback);
			
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			if (MyWeakEventHandler != null)
				MyWeakEventHandler(this, MyArgs);
			
			Assert.IsFalse(WasUnregistered, "Static Event Handler was unregistered");
			Assert.IsTrue(MyArgs.WasInvoked, "Static Event Handler was not raised");
		}
		
		[Test()]
		public void RaiseInstanceHandler()
		{	//****************************************
			EventHandler<WeakEventArgs> MyWeakEventHandler;
			UnregisterCallback<WeakEventArgs> MyUnregisterCallback;
			WeakEventInstance MyInstance = new WeakEventInstance();
			WeakEventArgs MyArgs = new WeakEventArgs();
			bool WasUnregistered = false;
			//****************************************
			
			MyUnregisterCallback = delegate(EventHandler<WeakEventArgs> eventHandler)
			{
				WasUnregistered = true;
			};
			
			//MyWeakEventHandler = new WeakDelegate<WeakEventArgs>((EventHandler<WeakEventArgs>)MyInstance.OnEventRaise, MyUnregisterCallback);
			MyWeakEventHandler = WeakEventDelegate.Create((EventHandler<WeakEventArgs>)MyInstance.OnEventRaise, MyUnregisterCallback);
			
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			if (MyWeakEventHandler != null)
				MyWeakEventHandler(this, MyArgs);
			
			GC.KeepAlive(MyInstance); // Required so we don't prematurely garbage collect our Instance
			
			Assert.IsFalse(WasUnregistered, "Instance Event Handler was unregistered");
			Assert.IsTrue(MyArgs.WasInvoked, "Instance Event Handler was not raised");
		}
		
		[Test()]
		public void RaiseLostInstanceHandler()
		{	//****************************************
			EventHandler<WeakEventArgs> MyWeakEventHandler;
			UnregisterCallback<WeakEventArgs> MyUnregisterCallback;
			WeakEventArgs MyArgs = new WeakEventArgs();
			bool WasUnregistered = false;
			//****************************************
			
			MyUnregisterCallback = delegate(EventHandler<WeakEventArgs> eventHandler)
			{
				WasUnregistered = true;
			};
			
			// Call in a separate method so any temporary objects will be out of scope
			MyWeakEventHandler = CreateWeak(MyUnregisterCallback);
			//MyWeakEventHandler = new WeakDelegate<WeakEventArgs>((EventHandler<WeakEventArgs>)new WeakEventInstance().OnEventRaise, MyUnregisterCallback);
			
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			if (MyWeakEventHandler != null)
				MyWeakEventHandler(this, MyArgs);
			
			Assert.IsTrue(WasUnregistered, "Instance Event Handler was not unregistered");
			Assert.IsFalse(MyArgs.WasInvoked, "Instance Event Handler was raised");
		}
		*/
		[Test()]
		public void RemoveStaticEvent()
		{	//****************************************
			WeakEvent<WeakEventArgs> MyEvent = new WeakEvent<WeakEventArgs>();
			WeakEventArgs MyArgs = new WeakEventArgs();
			//****************************************
			
			MyEvent.Add(OnStaticEventRaise);
			
			MyEvent.Remove(OnStaticEventRaise);
			
			MyEvent.Invoke(this, MyArgs);
			
			Assert.IsFalse(MyArgs.WasInvoked, "Static Event Handler was raised");
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
			
			Assert.IsFalse(MyArgs.WasInvoked, "Instance Event Handler was raised");
		}
		
		//****************************************
		
		private static void OnStaticEventRaise(object sender, WeakEventArgs e)
		{
			e.WasInvoked = true;
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
				e.WasInvoked = true;
			}
		}
	}
}