using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Remoting
{
	/// <summary>
	/// Provides utility methods for Remote Tasks
	/// </summary>
	public static class RemoteTaskExtensions
	{
		/// <summary>
		/// Allows awaiting of a RemoteTask from another AppDomain
		/// </summary>
		/// <param name="remoteTask">The remote task to await</param>
		/// <returns>A task awaiter that can be used to await the remote task</returns>
		/// <remarks>Beware of unloading the target AppDomain in a continuation of this Task, as it's likely running with the AppDomain in the stack, causing a strange ThreadAbortException</remarks>
		public static TaskAwaiter GetAwaiter(this RemoteTask remoteTask) =>
			// This is an extension method so the code runs in the calling AppDomain
			RemoteTask.CreateTask(remoteTask).GetAwaiter();

		/// <summary>
		/// Allows awaiting of a RemoteTask from another AppDomain
		/// </summary>
		/// <param name="remoteTask">The remote task to await</param>
		/// <returns>A task awaiter that can be used to await the remote task</returns>
		/// <remarks>Beware of unloading the target AppDomain in a continuation of this Task, as it's likely running with the AppDomain in the stack, causing a strange ThreadAbortException</remarks>
		public static TaskAwaiter<TResult> GetAwaiter<TResult>(this RemoteTask<TResult> remoteTask) =>
			// This is an extension method so the code runs in the calling AppDomain
			RemoteTask<TResult>.CreateTask(remoteTask).GetAwaiter();

		/// <summary>
		/// Wraps a RemoteTask with an awaitable Task that exists within this AppDomain
		/// </summary>
		/// <param name="remoteTask">The remote task to wrap</param>
		/// <returns>A task that will reflect the status of the RemoteTask</returns>
		/// <remarks>Beware of unloading the target AppDomain in a continuation of this Task, as it's likely running with the AppDomain in the stack, causing a strange ThreadAbortException</remarks>
		public static Task ToTask(this RemoteTask remoteTask) => RemoteTask.CreateTask(remoteTask);

		/// <summary>
		/// Wraps a RemoteTask with an awaitable Task that exists within this AppDomain
		/// </summary>
		/// <param name="remoteTask">The remote task to wrap</param>
		/// <returns>A task that will reflect the status of the RemoteTask</returns>
		/// <remarks>Beware of unloading the target AppDomain in a continuation of this Task, as it's likely running with the AppDomain in the stack, causing a strange ThreadAbortException</remarks>
		public static Task ToTask<TResult>(this RemoteTask<TResult> remoteTask) => RemoteTask<TResult>.CreateTask(remoteTask);

		/// <summary>
		/// Transforms a Task into a RemoteTask suitable for passing between AppDomains
		/// </summary>
		/// <param name="task">The task to transform</param>
		/// <returns>A RemoteTask that can be awaited from another AppDomain</returns>
		/// <remarks>To create a RemoteTask that listens to a RemoteCancellationToken, use one of the <see cref="O:RemoteTask.Start" /> overrides</remarks>
		public static RemoteTask ToRemote(this Task task) => new RemoteTask(task);

		/// <summary>
		/// Transforms a Task into a RemoteTask suitable for passing between AppDomains
		/// </summary>
		/// <param name="task">The task to transform</param>
		/// <returns>A RemoteTask that can be awaited from another AppDomain</returns>
		/// <remarks>To create a RemoteTask that listens to a RemoteCancellationToken, use one of the <see cref="O:RemoteTask&lt;TResult&gt;.Start" /> overrides</remarks>
		public static RemoteTask<TResult> ToRemote<TResult>(this Task<TResult> task) => new RemoteTask<TResult>(task);

		/// <summary>
		/// Transforms a Cancellation Token into a RemoteCancellationToken suitable for passing between AppDomains
		/// </summary>
		/// <param name="token">The Cancellation Token to transfork</param>
		/// <returns>A RemoteCancellationToken that can pass cancellation requests to another AppDomain</returns>
		/// <remarks>When calling remote objects supporting cancellation, the RemoteCancellationToken must persist for the lifetime of the call. Wrap the call with a Using statement and await the RemoteTask inside.</remarks>
		public static RemoteCancellationToken ToRemote(this CancellationToken token) => new RemoteCancellationToken(token);

		/// <summary>
		/// Transforms a Cancellation Token Source into a RemoteCancellationToken suitable for passing between AppDomains
		/// </summary>
		/// <param name="tokenSource">The Cancellation Token Source to transfork</param>
		/// <returns>A RemoteCancellationToken that can pass cancellation requests to another AppDomain</returns>
		/// <remarks>When calling remote objects supporting cancellation, the RemoteCancellationToken must persist for the lifetime of the call. Wrap the call with a Using statement and await the RemoteTask inside.</remarks>
		public static RemoteCancellationToken ToRemote(this CancellationTokenSource tokenSource) => new RemoteCancellationToken(tokenSource.Token);
	}
}