/****************************************\
 WeakDelegateSlim.cs
 Created: 2014-04-29
\****************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Provides methods for managing a delegate to a method on a weakly bound object
	/// </summary>
	[SecurityCritical]
	public static class WeakDelegateSlim
	{
		// IOS is statically compiled and doesn't do JIT, so we can't make generic types from reflection
#if !IOS
		/// <summary>
		/// Creates a new Weak Event Delegate
		/// </summary>
		/// <param name="callback">The delegate to weakly bind to</param>
		/// <param name="unsubscribe">An optional callback to unsubscribe from the event handler</param>
		/// <returns>A weak delegate that will call the target method as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static TDelegate CreateDynamic<TDelegate>(TDelegate callback, Action<TDelegate> unsubscribe = null) where TDelegate : class
		{   //****************************************
			Type TargetType;
			//****************************************

			if (!(callback is Delegate MyCallback))
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

			case 0:

		
			default:
				throw new ArgumentException("Method Parameters not supported");
			}
			
			//****************************************

			
			var TypeParams = new Type[MyParams.Length + 2];

			TypeParams[0] = typeof(TDelegate);
			TypeParams[1] = MyCallback.Target.GetType(); // First type parameter is the target object type
			
			// The remaining parameters are the types of any method parameters
			for (int Index = 0; Index < MyParams.Length; Index++)
			{
				TypeParams[Index + 2] = MyParams[Index].ParameterType;
			}
			
			//****************************************

			return (TDelegate)((IDelegateBase)Activator.CreateInstance(TargetType.MakeGenericType(TypeParams), callback, unsubscribe)).GetHandler();
		}

		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <param name="unsubscribe">An optional callback to unsubscribe from the event handler</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action Create(Action callback, Action<Action> unsubscribe = null)
		{
			if (callback.Target == null)
				return callback;
			
			//****************************************

			return DelegateBase<Action>.Create(typeof(DelegateHandler<,>), callback, unsubscribe).GetHandler();
		}

		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <param name="unsubscribe">An optional callback to unsubscribe from the event handler</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0> Create<T0>(Action<T0> callback, Action<Action<T0>> unsubscribe = null)
		{
			if (callback.Target == null)
				return callback;

			//****************************************

			return DelegateBase<Action<T0>>.Create(typeof(DelegateHandler<,,>), callback, unsubscribe, typeof(T0)).GetHandler();
		}
		
		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <param name="unsubscribe">An optional callback to unsubscribe from the event handler</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0, T1> Create<T0, T1>(Action<T0, T1> callback, Action<Action<T0, T1>> unsubscribe = null)
		{
			if (callback.Target == null)
				return callback;

			//****************************************

			return DelegateBase<Action<T0, T1>>.Create(typeof(DelegateHandler<,,,>), callback, unsubscribe, typeof(T0), typeof(T1)).GetHandler();
		}
		
		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <param name="unsubscribe">An optional callback to unsubscribe from the event handler</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0, T1, T2> Create<T0, T1, T2>(Action<T0, T1, T2> callback, Action<Action<T0, T1, T2>> unsubscribe = null)
		{
			if (callback.Target == null)
				return callback;

			//****************************************

			return DelegateBase<Action<T0, T1, T2>>.Create(typeof(DelegateHandler<,,,,>), callback, unsubscribe, typeof(T0), typeof(T1), typeof(T2)).GetHandler();
		}
		
		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <param name="unsubscribe">An optional callback to unsubscribe from the event handler</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0, T1, T2, T3> Create<T0, T1, T2, T3>(Action<T0, T1, T2, T3> callback, Action<Action<T0, T1, T2, T3>> unsubscribe = null)
		{
			if (callback.Target == null)
				return callback;

			//****************************************

			return DelegateBase<Action<T0, T1, T2, T3>>.Create(typeof(DelegateHandler<,,,,,>), callback, unsubscribe, typeof(T0), typeof(T1), typeof(T2), typeof(T3)).GetHandler();
		}
		
		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <param name="unsubscribe">An optional callback to unsubscribe from the event handler</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0, T1, T2, T3, T4> Create<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> callback, Action<Action<T0, T1, T2, T3, T4>> unsubscribe = null)
		{
			if (callback.Target == null)
				return callback;

			//****************************************

			return DelegateBase<Action<T0, T1, T2, T3, T4>>.Create(typeof(DelegateHandler<,,,,,,>), callback, unsubscribe, typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4)).GetHandler();
		}
#endif

		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <param name="unsubscribe">An optional callback to unsubscribe from the event handler</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action CreateFor<TTarget>(Action callback, Action<Action> unsubscribe = null) where TTarget : class
		{
			if (callback.Target == null)
				return callback;

			//****************************************

			return new DelegateHandler<Action, TTarget>(callback, unsubscribe).GetHandler();
		}

		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <param name="unsubscribe">An optional callback to unsubscribe from the event handler</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0> CreateFor<TTarget, T0>(Action<T0> callback, Action<Action<T0>> unsubscribe = null) where TTarget : class
		{
			if (callback.Target == null)
				return callback;

			//****************************************

			return new DelegateHandler<Action<T0>, TTarget, T0>(callback, unsubscribe).GetHandler();
		}

		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <param name="unsubscribe">An optional callback to unsubscribe from the event handler</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0, T1> CreateFor<TTarget, T0, T1>(Action<T0, T1> callback, Action<Action<T0, T1>> unsubscribe = null) where TTarget : class
		{
			if (callback.Target == null)
				return callback;

			//****************************************

			return new DelegateHandler<Action<T0, T1>, TTarget, T0, T1>(callback, unsubscribe).GetHandler();
		}

		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <param name="unsubscribe">An optional callback to unsubscribe from the event handler</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0, T1, T2> CreateFor<TTarget, T0, T1, T2>(Action<T0, T1, T2> callback, Action<Action<T0, T1, T2>> unsubscribe = null) where TTarget : class
		{
			if (callback.Target == null)
				return callback;

			//****************************************

			return new DelegateHandler<Action<T0, T1, T2>, TTarget, T0, T1, T2>(callback, unsubscribe).GetHandler();
		}

		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <param name="unsubscribe">An optional callback to unsubscribe from the event handler</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0, T1, T2, T3> Create<TTarget, T0, T1, T2, T3>(Action<T0, T1, T2, T3> callback, Action<Action<T0, T1, T2, T3>> unsubscribe = null) where TTarget : class
		{
			if (callback.Target == null)
				return callback;

			//****************************************

			return new DelegateHandler<Action<T0, T1, T2, T3>, TTarget, T0, T1, T2, T3>(callback, unsubscribe).GetHandler();
		}

		/// <summary>
		/// Creates a new Weak Event Delegate targeting an Action
		/// </summary>
		/// <param name="callback">The action to weakly bind to</param>
		/// <param name="unsubscribe">An optional callback to unsubscribe from the event handler</param>
		/// <returns>An action that will call the target Action as long as the reference still exists, or <paramref name="callback" /> if the target is a static object</returns>
		public static Action<T0, T1, T2, T3, T4> Create<TTarget, T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> callback, Action<Action<T0, T1, T2, T3, T4>> unsubscribe = null) where TTarget : class
		{
			if (callback.Target == null)
				return callback;

			//****************************************

			return new DelegateHandler<Action<T0, T1, T2, T3, T4>, TTarget, T0, T1, T2, T3, T4>(callback, unsubscribe).GetHandler();
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

			foreach (var MyDelegate in source.GetInvocationList())
			{
				// Find a weak delegate that invokes the target delegate
				if (MyDelegate.Target is DelegateBase<TDelegate> MyHandler)
				{
					if (MyHandler.Method != target.Method)
						continue;

					var MyTargetObject = MyHandler.Target;

					if (MyTargetObject == null)
						continue;

					if (MyTargetObject == target.Target)
						return MyDelegate as TDelegate;
				}
			}

			return null;
		}
		
		//****************************************

		[SecurityCritical]
		private interface IDelegateBase
		{
			object GetHandler();
		}

		private abstract class DelegateBase<TDelegate> : IDelegateBase where TDelegate : class
		{	//****************************************
			private GCHandle _Target;

			private readonly Action<TDelegate> _Unsubscribe;
			//****************************************

			protected DelegateBase(object target, Action<TDelegate> unsubscribe)
			{
				_Target = GCHandle.Alloc(target, GCHandleType.Weak);
				_Unsubscribe = unsubscribe;
			}

			[SecuritySafeCritical]
			~DelegateBase()
			{
				if (_Target.IsAllocated)
					_Target.Free();
			}

			//****************************************

			[SecurityCritical]
			object IDelegateBase.GetHandler() => GetHandler();

			[SecurityCritical]
			public TDelegate GetHandler()
			{
				// Create the delegate that is exposed to the outside world
				return Delegate.CreateDelegate(typeof(TDelegate), this, "OnRaise") as TDelegate;
			}

			internal void Release()
			{
				_Unsubscribe?.Invoke(GetHandler());

				_Target.Free();
				GC.SuppressFinalize(this);
			}

			//****************************************

			internal object Target => _Target.Target;

			internal abstract MethodInfo Method { get; }

			//****************************************

			public static DelegateBase<TDelegate> Create(Type source, Delegate callback, Action<TDelegate> unsubscribe, params Type[] args)
			{
				var MyTypes = new Type[args.Length + 2];

				MyTypes[0] = typeof(TDelegate);
				MyTypes[1] = callback.Target.GetType();

				Array.Copy(args, 0, MyTypes, 2, args.Length);

				return (DelegateBase<TDelegate>)Activator.CreateInstance(source.MakeGenericType(MyTypes), callback, unsubscribe);
			}
		}

		private abstract class DelegateBase<TDelegate, TOpenDelegate> : DelegateBase<TDelegate> where TDelegate : class where TOpenDelegate : class
		{
			protected DelegateBase(Delegate callback, Action<TDelegate> unsubscribe) : base(callback.Target, unsubscribe)
			{
				OpenDelegate = Delegate.CreateDelegate(typeof(TOpenDelegate), callback.Method) as TOpenDelegate;
			}

			//****************************************

			internal TOpenDelegate OpenDelegate { get; }

			internal override MethodInfo Method => (OpenDelegate as Delegate).Method;
		}

		private sealed class DelegateHandler<TDelegate, TTarget> : DelegateBase<TDelegate, Action<TTarget>> where TDelegate : class where TTarget : class
		{
			public DelegateHandler(Delegate callback, Action<TDelegate> unsubscribe) : base(callback, unsubscribe)
			{
			}
			
			//****************************************
			
			public void OnRaise()
			{
				if (Target is TTarget MyTarget)
					OpenDelegate(MyTarget);
				else
					Release();
			}
		}

		private sealed class DelegateHandler<TDelegate, TTarget, T1> : DelegateBase<TDelegate, Action<TTarget, T1>> where TDelegate : class where TTarget : class
		{
			public DelegateHandler(Delegate callback, Action<TDelegate> unsubscribe) : base(callback, unsubscribe)
			{
			}
			
			//****************************************
			
			public void OnRaise(T1 arg1)
			{
				if (Target is TTarget MyTarget)
					OpenDelegate(MyTarget, arg1);
				else
					Release();
			}
		}
		
		private sealed class DelegateHandler<TDelegate, TTarget, T1, T2> : DelegateBase<TDelegate, Action<TTarget, T1, T2>> where TDelegate : class where TTarget : class
		{
			public DelegateHandler(Delegate callback, Action<TDelegate> unsubscribe) : base(callback, unsubscribe)
			{
			}
			
			//****************************************
			
			public void OnRaise(T1 arg1, T2 arg2)
			{
				if (Target is TTarget MyTarget)
					OpenDelegate(MyTarget, arg1, arg2);
				else
					Release();
			}
		}
		
		private sealed class DelegateHandler<TDelegate, TTarget, T1, T2, T3> : DelegateBase<TDelegate, Action<TTarget, T1, T2, T3>> where TDelegate : class where TTarget : class
		{
			public DelegateHandler(Delegate callback, Action<TDelegate> unsubscribe) : base(callback, unsubscribe)
			{
			}
			
			//****************************************
			
			public void OnRaise(T1 arg1, T2 arg2, T3 arg3)
			{
				if (Target is TTarget MyTarget)
					OpenDelegate(MyTarget, arg1, arg2, arg3);
				else
					Release();
			}
		}
		
		private sealed class DelegateHandler<TDelegate, TTarget, T1, T2, T3, T4> : DelegateBase<TDelegate, Action<TTarget, T1, T2, T3, T4>> where TDelegate : class where TTarget : class
		{
			public DelegateHandler(Delegate callback, Action<TDelegate> unsubscribe) : base(callback, unsubscribe)
			{
			}
			
			//****************************************
			
			public void OnRaise(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
			{
				if (Target is TTarget MyTarget)
					OpenDelegate(MyTarget, arg1, arg2, arg3, arg4);
				else
					Release();
			}
		}
		
		private sealed class DelegateHandler<TDelegate, TTarget, T1, T2, T3, T4, T5> : DelegateBase<TDelegate, Action<TTarget, T1, T2, T3, T4, T5>> where TDelegate : class where TTarget : class
		{
			public DelegateHandler(Delegate callback, Action<TDelegate> unsubscribe) : base(callback, unsubscribe)
			{
			}
			
			//****************************************
			
			public void OnRaise(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
			{
				if (Target is TTarget MyTarget)
					OpenDelegate(MyTarget, arg1, arg2, arg3, arg4, arg5);
				else
					Release();
			}
		}
	}
}
