/****************************************\
 SafeTimer.cs
 Created: 2012-05-11
\****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Represents a one-off timer to execute a method or set of methods on the ThreadPool
	/// </summary>
	public sealed class SafeTimer
	{
		/// <summary>
		/// Executes a method in the future on the ThreadPool
		/// </summary>
		/// <param name="callback">A callback to execute</param>
		/// <param name="dueTime">The number of milliseconds before the method will execute</param>
		public static void DelayedCallback(Action callback, int dueTime)
		{
			new SafeTimer(callback, dueTime);
		}
		
		/// <summary>
		/// Executes a method in the future on the ThreadPool
		/// </summary>
		/// <param name="callback">A callback to execute</param>
		/// <param name="state">An object to pass to the callback</param>
		/// <param name="dueTime">The number of milliseconds before the method will execute</param>
		public static void DelayedCallback<TState>(Action<TState> callback, TState state, int dueTime)
		{
			new SafeTimerSingle<TState>(callback, state, dueTime);
		}
		
		/// <summary>
		/// Executes a set of methods in the future on the ThreadPool
		/// </summary>
		/// <param name="actions">An enumerable object returning the actions to execute</param>
		/// <param name="dueTime">The number of milliseconds before the methods will begin executing</param>
		public static void DelayedInvoke(IEnumerable<Action> actions, int dueTime)
		{
			new SafeTimerMulti<Action>(actions, RaiseAction, dueTime, 0);
		}
		
		/// <summary>
		/// Executes a set of methods in the future on the ThreadPool
		/// </summary>
		/// <param name="actions">An enumerable object returning the actions to execute</param>
		/// <param name="dueTime">The number of milliseconds before the methods will begin executing</param>
		/// <param name="maxParallelism">The maximum number of ThreadPool threads to execute at any one time</param>
		public static void DelayedInvoke(IEnumerable<Action> actions, int dueTime, int maxParallelism)
		{
			new SafeTimerMulti<Action>(actions, RaiseAction, dueTime, maxParallelism);
		}
		
		/// <summary>
		/// Executes an action for each object
		/// </summary>
		/// <param name="source">An enumerable object containing the objects to work on</param>
		/// <param name="action">An action to execute for each object in the enumerable</param>
		/// <param name="dueTime">The number of milliseconds before the actions will begin executing</param>
		public static void DelayedForEach<TSource>(IEnumerable<TSource> source, Action<TSource> action, int dueTime)
		{
			new SafeTimerMulti<TSource>(source, action, dueTime, 0);
		}
		
		/// <summary>
		/// Executes an action for each object
		/// </summary>
		/// <param name="source">An enumerable object containing the objects to work on</param>
		/// <param name="action">An action to execute for each object in the enumerable</param>
		/// <param name="dueTime">The number of milliseconds before the actions will begin executing</param>
		/// <param name="maxParallelism">The maximum number of ThreadPool threads to execute at any one time</param>
		public static void DelayedForEach<TSource>(IEnumerable<TSource> source, Action<TSource> action, int dueTime, int maxParallelism)
		{
			new SafeTimerMulti<TSource>(source, action, dueTime, maxParallelism);
		}
		
		//****************************************
		
		private static void RaiseAction(Action action)
		{
			action();
		}
		
		//****************************************
		private Action _Callback;
		//****************************************
		
		private SafeTimer(Action callback, int dueTime)
		{
			_Callback = callback;
			
			var MyTimer = new Timer(OnSingleTimer);
			MyTimer.Change(dueTime, Timeout.Infinite);
		}
		
		//****************************************
		
		private void OnSingleTimer(object state)
		{
			((Timer)state).Dispose();
			
			_Callback();
		}
	}
	
	internal class SafeTimerSingle<TSource>
	{	//****************************************
		private Action<TSource> _Callback;
		private TSource _Source;
		//****************************************
		
		internal SafeTimerSingle(Action<TSource> callback, TSource state, int dueTime)
		{
			_Callback = callback;
			_Source = state;
			
			var MyTimer = new Timer(OnSingleTimer);
			
			MyTimer.Change(dueTime, Timeout.Infinite);
		}
		
		//****************************************
		
		private void OnSingleTimer(object state)
		{
			((Timer)state).Dispose();
			
			_Callback(_Source);
		}
	}
	
	internal class SafeTimerMulti<TSource>
	{	//****************************************
		private Action<TSource> _Callback;
		private IEnumerable<TSource> _Source;
		private int _MaxParallelism;
		//****************************************
		
		internal SafeTimerMulti(IEnumerable<TSource> source, Action<TSource> callback, int dueTime, int maxParallelism)
		{
			_Callback = callback;
			_Source = source;
			_MaxParallelism = maxParallelism;
			
			var MyTimer = new Timer(OnMultipleTimer);
			
			MyTimer.Change(dueTime, Timeout.Infinite);
		}
		
		//****************************************
		
		private void OnMultipleTimer(object state)
		{
			((Timer)state).Dispose();
			
			Parallel.ForEach<TSource>((IEnumerable<TSource>)state, _Callback);
		}
	}
}
