/****************************************\
 Extensions.cs
 Created: 2011-08-02
\****************************************/
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Contains extension methods for Threading support
	/// </summary>
	public static class Extensions
	{	//****************************************
		private static ConditionalWeakTable<WaitHandle, Task<bool>> _AsyncWaits = new ConditionalWeakTable<WaitHandle, Task<bool>>();
		//****************************************
		
		/// <summary>
		/// Open a Read Lock for the duration of the Using Block
		/// </summary>
		/// <param name="readLock">The <see cref="ReaderWriterLockSlim" /> to lock</param>
		/// <returns>An IDisposable structure to pass to a Using block</returns>
		public static ReadLock UsingReader(this ReaderWriterLockSlim readLock)
		{
			return ReadLock.From(readLock);
		}
		
		/// <summary>
		/// Open a Write Lock for the duration of the Using Block
		/// </summary>
		/// <param name="writeLock">The <see cref="ReaderWriterLockSlim" /> to lock</param>
		/// <returns>An IDisposable structure to pass to a Using block</returns>
		public static WriteLock UsingWriter(this ReaderWriterLockSlim writeLock)
		{
			return WriteLock.From(writeLock);
		}
		
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
						var MyRegisteredWait = ThreadPool.RegisterWaitForSingleObject(waitHandle, (state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult(!timedOut), MyTaskSource, timeout, true);
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
