/****************************************\
 WaitHandleExtensions.cs
 Created: 2014-12-05
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides extension methods for <see cref="WaitHandle" />
	/// </summary>
	public static class WaitHandleExtensions
	{	//****************************************
		private static ConditionalWeakTable<WaitHandle, Task<bool>> _AsyncWaits = new ConditionalWeakTable<WaitHandle, Task<bool>>();
		//****************************************
		
		/// <summary>
		/// Gets a <see cref="Task" /> used to await this <see cref="WaitHandle" />, with a timeout
		/// </summary>
		/// <param name="waitHandle">The <see cref="WaitHandle" /> to wait on</param>
		/// <param name="timeout">The time to wait. Use <see cref="Timeout.Infinite" /> for no timeout (wait forever)</param>
		/// <returns>A <see cref="Task" /> returning true if the <see cref="WaitHandle" /> received a signal, or false if it timed out</returns>
		/// <remarks>This method ensures multiple waits on a single WaitHandle are valid</remarks>
		public static Task<bool> WaitAsync(this WaitHandle waitHandle, int timeout)
		{
			if (waitHandle == null)
				throw new ArgumentNullException("waitHandle");
			
			lock (waitHandle)
			{
				return _AsyncWaits.GetValue(
					waitHandle,
					(innerHandle) =>
					{
						var MyTaskSource = new TaskCompletionSource<bool>();
						var MyRegisteredWait = ThreadPool.RegisterWaitForSingleObject(innerHandle, (state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult(!timedOut), MyTaskSource, timeout, true);
						var MyTask = MyTaskSource.Task;
						
						MyTask.ContinueWith((ancestor, state) => ((RegisteredWaitHandle)state).Unregister(null), MyRegisteredWait);
						
						return MyTask;
					}
				);
			}
		}
		
		/// <summary>
		/// Gets a <see cref="Task" /> used to await this <see cref="WaitHandle" />
		/// </summary>
		/// <param name="waitHandle">The <see cref="WaitHandle" /> to wait on</param>
		/// <returns>A <see cref="Task" /> that completes when the <see cref="WaitHandle" /> is signalled</returns>
		public static Task WaitAsync(this WaitHandle waitHandle)
		{
			return waitHandle.WaitAsync(Timeout.Infinite);
		}
		
	}
}
#endif