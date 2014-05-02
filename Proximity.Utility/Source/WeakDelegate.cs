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

namespace Proximity.Utility
{
	/// <summary>
	/// Represents a delegate to a method on a weakly bound object
	/// </summary>
	public sealed class WeakDelegate<TDelegate> where TDelegate : class
	{	//****************************************
		private GCHandle _Target;
		private MethodInfo _Method;
		
		private TDelegate _CallbackHandler;
		private Action<TDelegate> _UnregisterHandler;
		//****************************************
		private static Dictionary<MethodInfo, EmitHelper> _MethodInvokers = new Dictionary<MethodInfo, EmitHelper>();
		//****************************************
		
		/// <summary>
		/// Creates a new Weak Delegate
		/// </summary>
		/// <param name="callback">The delegate to weakly bind to</param>
		/// <param name="unregisterCallback">A callback to raise if the delegate target becomes invalid</param>
		public WeakDelegate(TDelegate callback, Action<TDelegate> unregisterCallback)
		{	//****************************************
			var MyCallback = callback as Delegate;
			//****************************************
			
			if (MyCallback == null)
				throw new ArgumentException("Callback must be a delegate object");
			
			//****************************************
			
			if (MyCallback.Target == null) // Ignore weak references for static delegates
			{
				_CallbackHandler = (TDelegate)callback;
				
				return;
			}

			_Target = GCHandle.Alloc(MyCallback.Target, GCHandleType.Weak);
			_Method = MyCallback.Method;
			
			_UnregisterHandler = unregisterCallback;
			
			BuildMethod();
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
		
		private void BuildMethod()
		{	//****************************************
			EmitHelper Emitter;
			//****************************************
			
			lock(_MethodInvokers)
			{
				// Check if we've already made a method that invokes this delegate signature
				if (!_MethodInvokers.TryGetValue(_Method, out Emitter))
				{
					// Calculate the parameters of the new method
					var MyParams = _Method.GetParameters();
					var MyInParams = new Type[MyParams.Length];
					
					MyInParams[0] = typeof(WeakDelegate<TDelegate>);
					
					for (int Index = 1; Index < MyParams.Length; Index++)
					{
						MyInParams[Index] = MyParams[Index].ParameterType;
					}
					
					// Create the Dynamic Method
					Emitter = EmitHelper.FromAction("WeakInvoke" + _Method.Name, typeof(WeakDelegate<TDelegate>), MyInParams);
					
					// Declare the various locals and labels
					Emitter.DeclareLocal("Target", typeof(object));
					Emitter
						.DeclareLabel("NotNull")
						.DeclareLabel("End");
					
					// Retrieve the object from our weak reference
					Emitter
						.LdArg(0) // WeakDelegate
						.LdFldA(typeof(WeakDelegate<TDelegate>), "_Target") // GCHandle
						.Call(typeof(GCHandle), "get_Target") // Target Object
						.StLoc("Target"); // -
					
					// If it's null, run the unregister callback
					Emitter
						.LdLoc("Target") // Target Object
						.BrTrue("NotNull")
						.LdArg(0) // WeakDelegate
						.LdFld(typeof(WeakDelegate<TDelegate>), "_UnregisterHandler") // Unregister Callback
						.LdArg(0) // Callback, WeakDelegate
						.LdFld(typeof(WeakDelegate<TDelegate>), "_EventHandler") // Callback, Handler
						.CallVirt(typeof(Action<TDelegate>), "Invoke", typeof(TDelegate)) // -
						.Br("End"); // -
						
					// Otherwise, call the delegate on the target object
					// First make the target object into the right type
					Emitter
						.LdLoc("Target") // Target (object)
						.CastClass(_Method.DeclaringType); // Target (event owner)
					
					// Then add the arguments
					for (int Index = 1; Index < MyParams.Length; Index++)
					{
						Emitter.LdArg(Index); // Target, Args(n)
					}
					
					// Now call it
					Emitter
						.TailCall.CallVirt(_Method) // -
						.MarkLabel("End")
						.Ret // -
						.End()
						;
				
					// Cache our new Delegate
					_MethodInvokers.Add(_Method, Emitter);
				}
			}
			
			//****************************************
			
			// Create an invoker that's attached to this instance
			_CallbackHandler = Emitter.ToDelegate<TDelegate>(this);
		}
		
		//****************************************
		
		/// <summary>
		/// Gets a delegate that can attempt to invoke the method targeted by this WeakDelegate
		/// </summary>
		public TDelegate CallbackHandler
		{
			get { return _CallbackHandler; }
		}
		
		//****************************************
		
		/// <summary>
		/// Converts this weak delegate into a delegate that can be called to perform a weak invocation
		/// </summary>
		/// <param name="source">The weak delegate to convert</param>
		/// <returns>A delegate that invokes this weak delegate</returns>
		public static implicit operator TDelegate(WeakDelegate<TDelegate> source)
		{
			return source._CallbackHandler;
		}
		
		//****************************************
		
		/// <summary>
		/// Locates the appropriate weak delegate invoker that was added to an event delegate
		/// </summary>
		/// <param name="source">The event delegate the weak delegate provided by <see cref="CallbackHandler" /> was added to</param>
		/// <param name="target">The method targeted by the weak delegate</param>
		/// <returns></returns>
		/// <remarks>To support the += and -= pattern, we must translate the strong delegate given into a weak delegate that can match the value passed to +=, in order to remove via -=</remarks>
		public static TDelegate FindHandler(Delegate source, Delegate target)
		{	//****************************************
			WeakDelegate<TDelegate> MyTarget;
			object MyTargetObject;
			//****************************************
			
			if (target.Target == null)
				return target as TDelegate;
			
			//****************************************
			
			foreach(Delegate MyDelegate in source.GetInvocationList())
			{
				MyTarget = MyDelegate.Target as WeakDelegate<TDelegate>;
				
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
					return MyDelegate as TDelegate;
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
