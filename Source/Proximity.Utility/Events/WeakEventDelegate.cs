﻿/****************************************\
 WeakEventDelegate.cs
 Created: 2012-10-30
\****************************************/
#if !MOBILE && !PORTABLE
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
//****************************************

namespace Proximity.Utility.Events
{
	/// <summary>
	/// Provides Weak Delegate services optimised for the EventHandler model
	/// </summary>
	public static class WeakEventDelegate
	{
		/// <summary>
		/// Creates a new Weak Event Delegate
		/// </summary>
		/// <param name="eventHandler">The event handler to weakly bind to</param>
		/// <returns>A weak delegate that will call EventHandler as long as the object still exists, or <paramref name="eventHandler" /> if the target is a static object</returns>
		public static EventHandler<TEventArgs> Create<TEventArgs>(EventHandler<TEventArgs> eventHandler) where TEventArgs : EventArgs
		{		//****************************************
			Type TargetType;
			IEventDelegate<TEventArgs> MyHandler;
			//****************************************
			
			if (eventHandler.Target == null) // Ignore weak references for static events
				return eventHandler;
			
			TargetType = typeof(EventDelegate<,>).MakeGenericType(eventHandler.Target.GetType(), typeof(TEventArgs));
			
			MyHandler = (IEventDelegate<TEventArgs>)Activator.CreateInstance(TargetType, eventHandler.Target, eventHandler.Method);
			
			return MyHandler.Handler;
		}
		
		/// <summary>
		/// Cleans out any expired delegates from the Event
		/// </summary>
		/// <param name="source">The event handler delegate</param>
		/// <param name="unregister">An action to call to remove handlers from the delegate</param>
		/// <remarks>To ensure thread safety, the unregister delegate should take the form of <code>(action) =&gt; MyEvent -= action;</code></remarks>
		public static void Cleanup<TEventArgs>(EventHandler<TEventArgs> source, Action<EventHandler<TEventArgs>> unregister) where TEventArgs : EventArgs
		{	//****************************************
			IEventDelegate<TEventArgs> MyTarget;
			object MyTargetObject;
			//****************************************
			
			if (source == null)
				return;
			
			foreach(Delegate MyDelegate in source.GetInvocationList())
			{
				MyTarget = MyDelegate.Target as IEventDelegate<TEventArgs>;
				
				if (MyTarget == null) // Not a Weak Delegate (ie: static)
					continue;
				
				MyTargetObject = MyTarget.Target;
				
				if (MyTargetObject != null) // Target object still exists
					continue;
				
				// No target object, unregister the delegate
				unregister((EventHandler<TEventArgs>)MyDelegate);
			}
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
					continue;
				
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
			//****************************************
			
			public EventDelegate(TTarget target, MethodInfo method)
			{
				_Target = GCHandle.Alloc(target, GCHandleType.Weak);
				_Handler = (WeakEventHandler)Delegate.CreateDelegate(typeof(WeakEventHandler), method);
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
					return;
				
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
#endif