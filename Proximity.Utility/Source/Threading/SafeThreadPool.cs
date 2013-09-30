/****************************************\
 SafeThreadPool.cs
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
	/// Provides methods for executing actions safely on the threadpool
	/// </summary>
	public static class SafeThreadPool
	{
		/// <summary>
		/// Executes a set of actions across the threadpool and the current thread
		/// </summary>
		/// <param name="actions">An enumerable object containing the actions to execute</param>
		/// <remarks>
		/// <para>If the threadpool never gets a chance to execute, all actions will still complete</para>
		/// <para>Intended to partially mimic Parallel.Invoke from .Net 4.0</para>
		/// </remarks>
		public static void Invoke(IEnumerable<Action> actions)
		{
			ForEach(actions, delegate(Action action) { action(); }, 0);
		}
		
		/// <summary>
		/// Executes a set of actions across the threadpool and the current thread
		/// </summary>
		/// <param name="actions">An enumerable object containing the actions to execute</param>
		/// <param name="maxParallelism">The maximum number of threads to spawn</param>
		/// <remarks>
		/// <para>If the threadpool never gets a chance to execute, all actions will still complete</para>
		/// <para>Intended to partially mimic Parallel.Invoke from .Net 4.0</para>
		/// </remarks>
		public static void Invoke(IEnumerable<Action> actions, int maxParallelism)
		{
			ForEach(actions, delegate(Action action) { action(); }, maxParallelism);
		}
		
		/// <summary>
		/// Executes the action for each item across the threadpool and the current thread
		/// </summary>
		/// <param name="source">An enumerable object containing the objects to execute actions for</param>
		/// <param name="action">The action to execute for each source object</param>
		/// <remarks>
		/// <para>If the threadpool never gets a chance to execute, all actions will still complete</para>
		/// <para>Intended to partially mimic Parallel.ForEach from .Net 4.0</para>
		/// </remarks>
		public static void ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> action)
		{
			ForEach(source, action, 0);
		}
		
		/// <summary>
		/// Executes the action for each item across the threadpool and the current thread
		/// </summary>
		/// <param name="source">An enumerable object containing the objects to execute actions for</param>
		/// <param name="action">The action to execute for each source object</param>
		/// <param name="maxParallelism">The maximum number of threads to spawn</param>
		/// <remarks>
		/// <para>If the threadpool never gets a chance to execute, all actions will still complete</para>
		/// <para>Intended to partially mimic Parallel.ForEach from .Net 4.0</para>
		/// </remarks>
		public static void ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> action, int maxParallelism)
		{	//****************************************
			var MyEnumerator = source.GetEnumerator();
			var MyExceptions = new List<Exception>();
			var MyWaitObject = new object();
			int ActiveThreads = 0;
			bool IsFinished = false;
			WaitCallback ActionExecutor = null;
			//****************************************
			
			ActionExecutor = delegate(object state)
			{
				TSource MyValue;
				
				if (IsFinished) // Have we already executed every action?
					return;
				
				Interlocked.Increment(ref ActiveThreads); // Still actions to go, let the root thread know
				
				try
				{
					// Try and queue up another processing thread
					if (maxParallelism == 0 || ActiveThreads < maxParallelism)
						ThreadPool.QueueUserWorkItem(ActionExecutor, null);
					
					for (;;)
					{
						lock (MyEnumerator)
						{
							// See if we have any more actions to run?
							if (IsFinished || !MyEnumerator.MoveNext())
							{
								IsFinished = true;
								
								break;
							}
							
							MyValue = MyEnumerator.Current;
						}
						
						try
						{
							action(MyValue); // Apply the action
						}
						catch (Exception e)
						{
							lock (MyExceptions)
								MyExceptions.Add(e);
						}
					}
				}
				finally
				{
					lock (MyWaitObject)
					{
						// No more actions to proces, decrease the active count
						if (Interlocked.Decrement(ref ActiveThreads) == 0)
							Monitor.Pulse(MyWaitObject); // No more threads, poke the root thread
					}
				}
			};
			
			// Start processing
			ActionExecutor(null);
			
			// No more actions, wait for the other threads to complete
			lock (MyWaitObject)
			{
				if (ActiveThreads != 0)
					Monitor.Wait(MyWaitObject, Timeout.Infinite, false);
			}
			
			// If we had any exceptions, throw them all at once
			if (MyExceptions.Count > 0)
				throw new AggregateException(MyExceptions);
		}
	}
}
