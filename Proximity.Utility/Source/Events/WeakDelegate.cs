/****************************************\
 WeakDelegate.cs
 Created: 10-09-2008
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
	/// Represents a callback for when a delegate is unregistered due to being missing
	/// </summary>
	public delegate void UnregisterCallback<TEventArgs>(EventHandler<TEventArgs> eventHandler) where TEventArgs : EventArgs;
	
	/// <summary>
	/// Represents a fake delegate to a method on a weakly bound object
	/// </summary>
	public class WeakDelegate<TEventArgs> where TEventArgs : EventArgs
	{	//****************************************
		private delegate void WeakEventHandler(object target, object sender, TEventArgs e);
		//****************************************
		private GCHandle _Target;
		private MethodInfo _Method;
		private WeakEventHandler _MethodInvoker;
		
		private EventHandler<TEventArgs> _EventHandler;
		private UnregisterCallback<TEventArgs> _UnregisterHandler;
		//****************************************
		private static Dictionary<MethodInfo, Delegate> _MethodInvokers = new Dictionary<MethodInfo, Delegate>();
		//****************************************
		
		/// <summary>
		/// Creates a new Weak Delegate
		/// </summary>
		/// <param name="eventHandler">The event handler to weakly bind to</param>
		/// <param name="unregisterCallback">A callback to raise if the event handler becomes invalid</param>
		public WeakDelegate(Delegate eventHandler, UnregisterCallback<TEventArgs> unregisterCallback)
		{
			if (eventHandler.Target == null) // Ignore weak references for static events
			{
				_EventHandler = (EventHandler<TEventArgs>)eventHandler;
				
				return;
			}

			_Target = GCHandle.Alloc(eventHandler.Target, GCHandleType.Weak);
			_Method = eventHandler.Method;
			
			_UnregisterHandler = unregisterCallback;
			_EventHandler = Invoke;
			
			BuildMethod(eventHandler.Method);
		}
		
		/// <summary>
		/// Destroys the Weak Delegate
		/// </summary>
		~WeakDelegate()
		{
			if (_Target.IsAllocated)
				_Target.Free();
		}
		
		//****************************************
		
		private void BuildMethod(MethodInfo eventHandler)
		{	//****************************************
			EmitHelper Emitter;
			Delegate MyDelegate;
			//****************************************
			
			lock(_MethodInvokers)
			{
				// Check if we've already made a method that invokes this delegate
				if (_MethodInvokers.TryGetValue(eventHandler, out MyDelegate))
				{
					_MethodInvoker = (WeakEventHandler)MyDelegate;
					
					return;
				}
				
				//****************************************
				// Create the Dynamic Method
				
				// TODO: Alter this so we don't need the Invoke method, which cleans up the call-stack
				
				Emitter = EmitHelper.FromAction("WeakInvoke" + eventHandler.Name, eventHandler.DeclaringType, typeof(object), typeof(object), typeof(TEventArgs));
	
				Emitter
					.LdArg(0) // Target (object)
					.CastClass(eventHandler.DeclaringType) // Target (event owner)
					.LdArg(1) // Target, Sender
					.LdArg(2) // Target, Sender, EventArgs
					.TailCall.Call(eventHandler, null) // ..
					.Ret
					.End()
					;
				
				_MethodInvoker = Emitter.ToDelegate<WeakEventHandler>();
				
				//****************************************
				
				// Cache our new Delegate
				_MethodInvokers.Add(eventHandler, _MethodInvoker);
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Retrieves a delegate that can weakly invoke the target event handler
		/// </summary>
		/// <returns>The requested delegate</returns>
		public EventHandler<TEventArgs> GetDelegate()
		{
			return _EventHandler;
		}
		
		//****************************************

		/// <summary>
		/// Calls the weak delegate, or the unregister handler if it is no longer valid
		/// </summary>
		/// <param name="sender">The sending object</param>
		/// <param name="e">Any event arguments</param>
		public void Invoke(object sender, TEventArgs e)
		{	//****************************************
			object Target = _Target.Target;
			//****************************************
			
			// Check if the WeakDelegate target has been garbage collected
			if (Target == null)
			{
				// Target no longer exists, unregister it
				_UnregisterHandler.Invoke(_EventHandler);
				
				return;
			}
			
			// Call the invocation method for this target
			_MethodInvoker.Invoke(Target, sender, e);
		}
		
		//****************************************
		
		/// <summary>
		/// Converts this weak delegate into a delegate that can be called to perform a weak invocation
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public static implicit operator EventHandler<TEventArgs>(WeakDelegate<TEventArgs> source)
		{
			return source._EventHandler;
		}
		
		//****************************************
		
		/// <summary>
		/// Locates the appropriate weak delegate invoker that was added to an event delegate
		/// </summary>
		/// <param name="source">The event delegate the weak delegate provided by <see cref="GetDelegate" /> was added to</param>
		/// <param name="target">The method targeted by the weak delegate</param>
		/// <returns></returns>
		/// <remarks>To support the += and -= pattern, we must translate the strong delegate given into a weak delegate that can match the value passed to +=, in order to remove via -=</remarks>
		public static EventHandler<TEventArgs> FindHandler(EventHandler<TEventArgs> source, EventHandler<TEventArgs> target)
		{	//****************************************
			WeakDelegate<TEventArgs> MyTarget;
			object MyTargetObject;
			//****************************************
			
			if (target.Target == null)
				return target;
			
			//****************************************
			
			foreach(Delegate MyDelegate in source.GetInvocationList())
			{
				MyTarget = MyDelegate.Target as WeakDelegate<TEventArgs>;
				
				if (MyTarget == null)
					continue;
				
				if (MyTarget._Method != target.Method)
					continue;
				
				MyTargetObject = MyTarget._Target.Target;
				
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
		
		/// <summary>
		/// Clears the invocation method cache
		/// </summary>
		/// <remarks>Used for testing the dynamic emit functionality</remarks>
		public static void ClearInvokerCache()
		{
			lock(_MethodInvokers)
			{
				_MethodInvokers.Clear();
			}
		}
	}
}
