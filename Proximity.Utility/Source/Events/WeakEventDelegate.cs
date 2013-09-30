/****************************************\
 WeakEventDelegate.cs
 Created: 2012-10-30
\****************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
//****************************************

namespace Proximity.Utility.Events
{
	/// <summary>
	/// Provides Weak Event Delegate services
	/// </summary>
	public static class WeakEventDelegate
	{
		/// <summary>
		/// Creates a new Weak Event Delegate
		/// </summary>
		/// <param name="eventHandler">The event handler to weakly bind to</param>
		/// <param name="unregisterCallback">A callback to raise if the event handler becomes invalid, receiving the Weak Event Delegate to unregister</param>
		/// <returns>A weak delegate that will call EventHandler as long as the object still exists, or <paramref name="eventHandler" /> if the target is a static object</returns>
		public static EventHandler<TEventArgs> Create<TEventArgs>(EventHandler<TEventArgs> eventHandler, UnregisterCallback<TEventArgs> unregisterCallback) where TEventArgs : EventArgs
		{		//****************************************
			Type TargetType;
			IEventDelegate<TEventArgs> MyHandler;
			//****************************************
			
			if (eventHandler.Target == null) // Ignore weak references for static events
				return eventHandler;
			
			TargetType = typeof(EventDelegate<,>).MakeGenericType(eventHandler.Target.GetType(), typeof(TEventArgs));
			
			MyHandler = (IEventDelegate<TEventArgs>)Activator.CreateInstance(TargetType, eventHandler.Target, eventHandler.Method, unregisterCallback);
			
			return MyHandler.Handler;
		}
		
		/// <summary>
		/// Locates the appropriate Weak Event Delegate that was added to an event delegate
		/// </summary>
		/// <param name="source">The multicast event delegate the weak delegate provided by <see cref="Create" /> was added to</param>
		/// <param name="target">The method targeted by the weak delegate</param>
		/// <returns></returns>
		/// <remarks>To support the += and -= pattern, we must translate the strong delegate given into a weak delegate that can match the value passed to +=, in order to remove via -=</remarks>
		public static EventHandler<TEventArgs> FindHandler<TEventArgs>(EventHandler<TEventArgs> source, EventHandler<TEventArgs> target) where TEventArgs : EventArgs
		{	//****************************************
			IEventDelegate<TEventArgs> MyTarget;
			object MyTargetObject;
			//****************************************
			
			if (target.Target == null)
				return target;
			
			//****************************************
			
			foreach(Delegate MyDelegate in source.GetInvocationList())
			{
				// Find a weak delegate that invokes the target delegate
				MyTarget = MyDelegate.Target as IEventDelegate<TEventArgs>;
				
				if (MyTarget == null)
					continue;
				
				if (MyTarget.Method != target.Method)
					continue;
				
				MyTargetObject = MyTarget.Target;
				
				if (MyTargetObject == null)
				{
					// Don't bother unregistering at this point, do it only when we invoke
					// MyTarget._UnregisterHandler.Invoke(MyTarget._EventHandler);
					
					continue;
				}
				
				if (MyTargetObject == target.Target)
					return (EventHandler<TEventArgs>)MyDelegate;
			}
			
			return null;
		}
		
		//****************************************
		
		private interface IEventDelegate<TEventArgs> where TEventArgs : EventArgs
		{
			void Handler(object sender, TEventArgs e);
			
			MethodInfo Method { get; }
			object Target { get; }
		}
		
		//****************************************
		
		private class EventDelegate<TTarget, TEventArgs> : IEventDelegate<TEventArgs> where TTarget : class where TEventArgs : EventArgs
		{	//****************************************
			private delegate void WeakEventHandler(TTarget target, object sender, TEventArgs e);
			
			private GCHandle _Target;
			private WeakEventHandler _Handler;
			
			private UnregisterCallback<TEventArgs> _UnregisterCallback;
			//****************************************
			
			public EventDelegate(TTarget target, MethodInfo method, UnregisterCallback<TEventArgs> unregisterCallback)
			{
				_Target = GCHandle.Alloc(target, GCHandleType.Weak);
				_Handler = (WeakEventHandler)Delegate.CreateDelegate(typeof(WeakEventHandler), method);
				
				_UnregisterCallback = unregisterCallback;
			}
			
			~EventDelegate()
			{
				if (_Target.IsAllocated)
					_Target.Free();
			}
			
			//****************************************
			
			public void Handler(object sender, TEventArgs e)
			{	//****************************************
				var MyTarget = (TTarget)_Target.Target;
				//****************************************
				
				if (MyTarget == null)
				{
					_Target.Free();
					_UnregisterCallback(Handler);
					
					return;
				}
				
				_Handler(MyTarget, sender, e);
			}
			
			//****************************************
			
			public object Target
			{
				get { return _Target.Target; }
			}
			
			public MethodInfo Method
			{
				get { return _Handler.Method; }
			}
		}
	}
}
