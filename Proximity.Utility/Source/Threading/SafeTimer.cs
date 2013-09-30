/****************************************\
 SafeTimer.cs
 Created: 2012-05-11
\****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Proximity.Utility;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Represents a one-off timer to execute a method or set of methods on the ThreadPool
	/// </summary>
	public class SafeTimer
	{
		/// <summary>
		/// Executes a method in the future on the ThreadPool
		/// </summary>
		/// <param name="callback">A callback to execute</param>
		/// <param name="state">An object to pass to the callback</param>
		/// <param name="dueTime">The number of milliseconds before the method will execute</param>
		public static void DelayedCallback<TState>(Action<TState> callback, TState state, int dueTime)
		{
			new SafeTimer<TState>(callback, state, dueTime);
		}
		
		/// <summary>
		/// Executes a set of methods in the future on the ThreadPool
		/// </summary>
		/// <param name="actions">An enumerable object returning the actions to execute</param>
		/// <param name="dueTime">The number of milliseconds before the methods will begin executing</param>
		public static void DelayedInvoke(IEnumerable<Action> actions, int dueTime)
		{
			new SafeTimer<Action>(actions, delegate(Action action) { action(); }, dueTime, 0);
		}
		
		/// <summary>
		/// Executes a set of methods in the future on the ThreadPool
		/// </summary>
		/// <param name="actions">An enumerable object returning the actions to execute</param>
		/// <param name="dueTime">The number of milliseconds before the methods will begin executing</param>
		/// <param name="maxParallelism">The maximum number of ThreadPool threads to execute at any one time</param>
		public static void DelayedInvoke(IEnumerable<Action> actions, int dueTime, int maxParallelism)
		{
			new SafeTimer<Action>(actions, delegate(Action action) { action(); }, dueTime, maxParallelism);
		}
		
		/// <summary>
		/// Executes an action for each object
		/// </summary>
		/// <param name="source">An enumerable object containing the objects to work on</param>
		/// <param name="action">An action to execute for each object in the enumerable</param>
		/// <param name="dueTime">The number of milliseconds before the actions will begin executing</param>
		public static void DelayedForEach<TSource>(IEnumerable<TSource> source, Action<TSource> action, int dueTime)
		{
			new SafeTimer<TSource>(source, action, dueTime, 0);
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
			new SafeTimer<TSource>(source, action, dueTime, maxParallelism);
		}
	}
	
	internal class SafeTimer<TSource>
	{	//****************************************
		private Timer _Timer;
		private Action<TSource> _Callback;
		private int _MaxParallelism;
		//****************************************
		
		internal SafeTimer(Action<TSource> callback, TSource state, int dueTime)
		{
			_Callback = callback;
			
			_Timer = new Timer(OnSingleTimer, state, dueTime, Timeout.Infinite);
		}
		
		internal SafeTimer(IEnumerable<TSource> source, Action<TSource> callback, int dueTime, int maxParallelism)
		{
			_Callback = callback;
			_MaxParallelism = maxParallelism;
			
			_Timer = new Timer(OnMultipleTimer, source, dueTime, Timeout.Infinite);
		}
		
		//****************************************
		
		private void OnSingleTimer(object state)
		{
			_Timer.Dispose();
			
			_Callback((TSource)state);
		}
		
		private void OnMultipleTimer(object state)
		{
			_Timer.Dispose();
			
			SafeThreadPool.ForEach<TSource>((IEnumerable<TSource>)state, _Callback);
		}
	}
}
