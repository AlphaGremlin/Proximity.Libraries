/****************************************\
 WeakDelegateSlim.cs
 Created: 2014-04-29
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
	/// Provides methods for managing a delegate to a method on a weakly bound object
	/// </summary>
	public static class WeakDelegateSlim
	{
		/// <summary>
		/// Creates a new Weak Event Delegate
		/// </summary>
		/// <param name="callback">The delegate to weakly bind to</param>
		/// <returns>A weak delegate that will call the target method as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static TDelegate Create<TDelegate>(TDelegate callback) where TDelegate : class
		{		//****************************************
			Delegate MyCallback = callback as Delegate;
			Type TargetType;
			DelegateBase<TDelegate> MyHandler;
			//****************************************
			
			if (MyCallback == null)
				throw new ArgumentException("Callback must be a delegate type");
			
			if (MyCallback.Target == null) // Ignore weak references for static events
				return callback;
			
			//****************************************
			
			var MyParams = MyCallback.Method.GetParameters();
			
			switch (MyParams.Length)
			{
			case 1:
				TargetType = typeof(DelegateHandler<,>);
				break;
				
			case 2:
				TargetType = typeof(DelegateHandler<,,>);
				break;
				
			case 3:
				TargetType = typeof(DelegateHandler<,,,>);
				break;
				
			case 4:
				TargetType = typeof(DelegateHandler<,,,,>);
				break;
				
			case 5:
				TargetType = typeof(DelegateHandler<,,,,,>);
				break;
				
			default:
			case 0:
				throw new ArgumentException("Method Parameters not supported");
				break;
			}
			
			//****************************************
			
			var TypeParams = new Type[MyParams.Length + 1];
			
			TypeParams[0] = MyCallback.Target.GetType(); // First type parameter is the target object type
			
			// The remaining parameters are the types of any method parameters
			for (int Index = 0; Index < MyParams.Length; Index++)
			{
				TypeParams[Index + 1] = MyParams[Index].ParameterType;
			}
			
			//****************************************
			
			TargetType = TargetType.MakeGenericType(TypeParams);
			
			MyHandler = (DelegateBase<TDelegate>)Activator.CreateInstance(TargetType, MyCallback.Target, MyCallback.Method);
			
			//****************************************
			
			return MyHandler.GetHandler();
		}
		
		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action Create(Action callback)
		{
			if (callback.Target == null)
				return callback;
			
			//****************************************
			
			var TargetType = typeof(DelegateHandler<,>).MakeGenericType(typeof(Action), callback.Target.GetType());
			
			var MyHandler = (DelegateBase<Action>)Activator.CreateInstance(TargetType, callback.Target, callback.Method);
			
			//****************************************
			
			return MyHandler.GetHandler();
		}
		
		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0> Create<T0>(Action<T0> callback)
		{
			if (callback.Target == null)
				return callback;
			
			//****************************************
			
			var TargetType = typeof(DelegateHandler<,,>).MakeGenericType(typeof(Action<T0>), callback.Target.GetType(), typeof(T0));
			
			var MyHandler = (DelegateBase<Action<T0>>)Activator.CreateInstance(TargetType, callback.Target, callback.Method);
			
			//****************************************
			
			return MyHandler.GetHandler();
		}
		
		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0, T1> Create<T0, T1>(Action<T0, T1> callback)
		{
			if (callback.Target == null)
				return callback;
			
			//****************************************
			
			var TargetType = typeof(DelegateHandler<,,,>).MakeGenericType(typeof(Action<T0, T1>), callback.Target.GetType(), typeof(T0), typeof(T1));
			
			var MyHandler = (DelegateBase<Action<T0, T1>>)Activator.CreateInstance(TargetType, callback.Target, callback.Method);
			
			//****************************************
			
			return MyHandler.GetHandler();
		}
		
		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0, T1, T2> Create<T0, T1, T2>(Action<T0, T1, T2> callback)
		{
			if (callback.Target == null)
				return callback;
			
			//****************************************
			
			var TargetType = typeof(DelegateHandler<,,,,>).MakeGenericType(typeof(Action<T0, T1, T2>), callback.Target.GetType(), typeof(T0), typeof(T1), typeof(T2));
			
			var MyHandler = (DelegateBase<Action<T0, T1, T2>>)Activator.CreateInstance(TargetType, callback.Target, callback.Method);
			
			//****************************************
			
			return MyHandler.GetHandler();
		}
		
		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0, T1, T2, T3> Create<T0, T1, T2, T3>(Action<T0, T1, T2, T3> callback)
		{
			if (callback.Target == null)
				return callback;
			
			//****************************************
			
			var TargetType = typeof(DelegateHandler<,,,,,>).MakeGenericType(typeof(Action<T0, T1, T2, T3>), callback.Target.GetType(), typeof(T0), typeof(T1), typeof(T2), typeof(T3));
			
			var MyHandler = (DelegateBase<Action<T0, T1, T2, T3>>)Activator.CreateInstance(TargetType, callback.Target, callback.Method);
			
			//****************************************
			
			return MyHandler.GetHandler();
		}
		
		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0, T1, T2, T3, T4> Create<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> callback)
		{
			if (callback.Target == null)
				return callback;
			
			//****************************************
			
			var TargetType = typeof(DelegateHandler<,,,,,,>).MakeGenericType(typeof(Action<T0, T1, T2, T3, T4>), callback.Target.GetType(), typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4));
			
			var MyHandler = (DelegateBase<Action<T0, T1, T2, T3, T4>>)Activator.CreateInstance(TargetType, callback.Target, callback.Method);
			
			//****************************************
			
			return MyHandler.GetHandler();
		}
		
		/// <summary>
		/// Cleans out any expired delegates from the source
		/// </summary>
		/// <param name="source">The multicast delegate with delegates provided by <see cref="O:Create" /></param>
		/// <param name="unregister">An action to call to remove handlers from the delegate</param>
		/// <remarks>To ensure thread safety, the unregister delegate should take the form of <code>(action) =&gt; MyDelegate -= action;</code></remarks>
		public static void Cleanup<TDelegate>(Delegate source, Action<TDelegate> unregister) where TDelegate : class
		{
			if (source == null)
				return;
			
			foreach(var MyDelegate in source.GetInvocationList())
			{
				var MyHandler = MyDelegate.Target as DelegateBase<TDelegate>;
				
				if (MyHandler == null) // Not a Weak Delegate (ie: static)
					continue;
				
				var MyTargetObject = MyHandler.Target;
				
				if (MyTargetObject != null) // Target object still exists
					continue;
				
				// No target object, unregister the delegate
				unregister(MyDelegate as TDelegate);
			}
		}
		
		/// <summary>
		/// Locates the appropriate Weak Event Delegate that was added to an event delegate
		/// </summary>
		/// <param name="source">The multicast delegate the weak delegate provided by <see cref="O:Create" /> was added to</param>
		/// <param name="target">The method targeted by the weak delegate</param>
		/// <returns></returns>
		/// <remarks>To support the += and -= pattern, we must translate the strong delegate given into a weak delegate that can match the value passed to +=, in order to remove via -=</remarks>
		public static TDelegate FindHandler<TDelegate>(Delegate source, Delegate target) where TDelegate : class
		{
			if (target.Target == null)
				return target as TDelegate;
			
			//****************************************
			
			foreach(var MyDelegate in source.GetInvocationList())
			{
				// Find a weak delegate that invokes the target delegate
				var MyHandler = MyDelegate.Target as DelegateBase<TDelegate>;
				
				if (MyHandler == null)
					continue;
				
				if (MyHandler.Method != target.Method)
					continue;
				
				var MyTargetObject = MyHandler.Target;
				
				if (MyTargetObject == null)
					continue;
				
				if (MyTargetObject == target.Target)
					return MyDelegate as TDelegate;
			}
			
			return null;
		}
		
		//****************************************
		
		private abstract class DelegateBase<TDelegate> where TDelegate : class
		{	//****************************************
			private GCHandle _Target;
			//****************************************
			
			protected DelegateBase(object target)
			{
				_Target = GCHandle.Alloc(target, GCHandleType.Weak);
			}
			
			~DelegateBase()
			{
				if (_Target.IsAllocated)
					_Target.Free();
			}
			
			//****************************************
			
			public TDelegate GetHandler()
			{
				return Delegate.CreateDelegate(typeof(TDelegate), this, "OnRaise") as TDelegate;
			}
			
			//****************************************
			
			public object Target
			{
				get { return _Target.Target; }
			}
			
			public abstract MethodInfo Method { get; }
		}
		
		private sealed class DelegateHandler<TDelegate, TTarget> : DelegateBase<TDelegate> where TDelegate : class where TTarget : class
		{	//****************************************
			private Action<TTarget> _Handler;
			//****************************************
			
			public DelegateHandler(TTarget target, MethodInfo method) : base(target)
			{
				_Handler = (Action<TTarget>)Delegate.CreateDelegate(typeof(Action<TTarget>), method);
			}
			
			//****************************************
			
			public void OnRaise()
			{	//****************************************
				var MyTarget = (TTarget)Target;
				//****************************************
				
				if (MyTarget == null)
					return;
				
				_Handler(MyTarget);
			}
			
			//****************************************
			
			public override MethodInfo Method
			{
				get { return _Handler.Method; }
			}
		}
		
		private sealed class DelegateHandler<TDelegate, TTarget, T1> : DelegateBase<TDelegate> where TDelegate : class where TTarget : class
		{	//****************************************
			private Action<TTarget, T1> _Handler;
			//****************************************
			
			public DelegateHandler(TTarget target, MethodInfo method) : base(target)
			{
				_Handler = (Action<TTarget, T1>)Delegate.CreateDelegate(typeof(Action<TTarget, T1>), method);
			}
			
			//****************************************
			
			public void OnRaise(T1 arg1)
			{	//****************************************
				var MyTarget = (TTarget)Target;
				//****************************************
				
				if (MyTarget == null)
					return;
				
				_Handler(MyTarget, arg1);
			}
			
			//****************************************
			
			public override MethodInfo Method
			{
				get { return _Handler.Method; }
			}
		}
		
		private sealed class DelegateHandler<TDelegate, TTarget, T1, T2> : DelegateBase<TDelegate> where TDelegate : class where TTarget : class
		{	//****************************************
			private Action<TTarget, T1, T2> _Handler;
			//****************************************
			
			public DelegateHandler(TTarget target, MethodInfo method) : base(target)
			{
				_Handler = (Action<TTarget, T1, T2>)Delegate.CreateDelegate(typeof(Action<TTarget, T1, T2>), method);
			}
			
			//****************************************
			
			public void OnRaise(T1 arg1, T2 arg2)
			{	//****************************************
				var MyTarget = (TTarget)Target;
				//****************************************
				
				if (MyTarget == null)
					return;
				
				_Handler(MyTarget, arg1, arg2);
			}
			
			//****************************************
			
			public override MethodInfo Method
			{
				get { return _Handler.Method; }
			}
		}
		
		private sealed class DelegateHandler<TDelegate, TTarget, T1, T2, T3> : DelegateBase<TDelegate> where TDelegate : class where TTarget : class
		{	//****************************************
			private Action<TTarget, T1, T2, T3> _Handler;
			//****************************************
			
			public DelegateHandler(TTarget target, MethodInfo method) : base(target)
			{
				_Handler = (Action<TTarget, T1, T2, T3>)Delegate.CreateDelegate(typeof(Action<TTarget, T1, T2, T3>), method);
			}
			
			//****************************************
			
			public void OnRaise(T1 arg1, T2 arg2, T3 arg3)
			{	//****************************************
				var MyTarget = (TTarget)Target;
				//****************************************
				
				if (MyTarget == null)
					return;
				
				_Handler(MyTarget, arg1, arg2, arg3);
			}
			
			//****************************************
			
			public override MethodInfo Method
			{
				get { return _Handler.Method; }
			}
		}
		
		private sealed class DelegateHandler<TDelegate, TTarget, T1, T2, T3, T4> : DelegateBase<TDelegate> where TDelegate : class where TTarget : class
		{	//****************************************
			private Action<TTarget, T1, T2, T3, T4> _Handler;
			//****************************************
			
			public DelegateHandler(TTarget target, MethodInfo method) : base(target)
			{
				_Handler = (Action<TTarget, T1, T2, T3, T4>)Delegate.CreateDelegate(typeof(Action<TTarget, T1, T2, T3, T4>), method);
			}
			
			//****************************************
			
			public void OnRaise(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
			{	//****************************************
				var MyTarget = (TTarget)Target;
				//****************************************
				
				if (MyTarget == null)
					return;
				
				_Handler(MyTarget, arg1, arg2, arg3, arg4);
			}
			
			//****************************************
			
			public override MethodInfo Method
			{
				get { return _Handler.Method; }
			}
		}
		
		private sealed class DelegateHandler<TDelegate, TTarget, T1, T2, T3, T4, T5> : DelegateBase<TDelegate> where TDelegate : class where TTarget : class
		{	//****************************************
			private Action<TTarget, T1, T2, T3, T4, T5> _Handler;
			//****************************************
			
			public DelegateHandler(TTarget target, MethodInfo method) : base(target)
			{
				_Handler = (Action<TTarget, T1, T2, T3, T4, T5>)Delegate.CreateDelegate(typeof(Action<TTarget, T1, T2, T3, T4, T5>), method);
			}
			
			//****************************************
			
			public void OnRaise(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
			{	//****************************************
				var MyTarget = (TTarget)Target;
				//****************************************
				
				if (MyTarget == null)
					return;
				
				_Handler(MyTarget, arg1, arg2, arg3, arg4, arg5);
			}
			
			//****************************************
			
			public override MethodInfo Method
			{
				get { return _Handler.Method; }
			}
		}
	}
}
