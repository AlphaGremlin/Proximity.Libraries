using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Contains extension methods for <see cref="ValueTask" /> and <see cref="ValueTask{TResult}"/>
	/// </summary>
	public static class ValueTaskExtensions
	{
		/// <summary>
		/// Wraps the given ValueTask, waiting for it to complete or the given token to cancel
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task that completes with the original task, and cancelling if either the original task or the given token cancel</returns>
		public static ValueTask When(this ValueTask task, CancellationToken token)
		{
			if (!token.CanBeCanceled || task.IsCompleted)
				return task;

			var Source = ValueTaskWhenToken.Retrieve(task, token);

			return new ValueTask(Source, Source.Token);
		}

		/// <summary>
		/// Wraps the given ValueTask, waiting for it to complete or the given token to cancel
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task that completes with the original task, and cancelling if either the original task or the given token cancel</returns>
		public static ValueTask<TResult> When<TResult>(this ValueTask<TResult> task, CancellationToken token)
		{
			if (!token.CanBeCanceled || task.IsCompleted)
				return task;

			var Source = ValueTaskWhenToken<TResult>.Retrieve(task, token);

			return new ValueTask<TResult>(Source, Source.Token);
		}

		/// <summary>
		/// Waits until all given ValueTasks have completed
		/// </summary>
		/// <param name="tasks">The Tasks to wait on</param>
		/// <returns>A ValueTask that completes when all given Tasks complete</returns>
		public static async ValueTask WhenAll(this IEnumerable<ValueTask> tasks)
		{
			if (tasks is null)
				throw new ArgumentNullException(nameof(tasks));

			List<Exception> Exceptions = null;

			foreach (var Task in tasks)
			{
				try
				{
					await Task;
				}
				catch (Exception e)
				{
					if (Exceptions == null)
						Exceptions = new List<Exception>();

					Exceptions.Add(e);
				}
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);
		}

		/// <summary>
		/// Waits until all given ValueTasks have completed
		/// </summary>
		/// <typeparam name="T">The type of the result from each Task</typeparam>
		/// <param name="tasks">The Tasks to wait on</param>
		/// <returns>A ValueTask that completes when all given Tasks complete</returns>
		public static ValueTask<T[]> WhenAll<T>(this IEnumerable<ValueTask<T>> tasks)
		{
			if (tasks is null)
				throw new ArgumentNullException(nameof(tasks));

			if (tasks is ValueTask<T>[] ArrayTasks)
				return InternalWhenAll(ArrayTasks);

			if (tasks is IReadOnlyCollection<ValueTask<T>> ReadOnlyCollection)
			{
				if (ReadOnlyCollection.Count == 0)
					return new ValueTask<T[]>(Array.Empty<T>());

				var Index = 0;

				ArrayTasks = new ValueTask<T>[ReadOnlyCollection.Count];

				foreach (var CurrentTask in tasks)
					ArrayTasks[Index++] = CurrentTask;

				return InternalWhenAll(ArrayTasks);
			}

			if (tasks is ICollection<ValueTask<T>> Collection)
			{
				if (Collection.Count == 0)
					return new ValueTask<T[]>(Array.Empty<T>());

				var Index = 0;

				ArrayTasks = new ValueTask<T>[Collection.Count];

				foreach (var CurrentTask in tasks)
					ArrayTasks[Index++] = CurrentTask;

				return InternalWhenAll(ArrayTasks);
			}

			var ListTasks = new List<ValueTask<T>>(tasks);

			if (ListTasks.Count == 0)
				return new ValueTask<T[]>(Array.Empty<T>());

			return InternalWhenAll(ListTasks);
		}

		/// <summary>
		/// Waits on two or more <see cref="ValueTask"/> instances
		/// </summary>
		/// <param name="task1">The first task to wait on</param>
		/// <param name="task2">The second task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public static ValueTaskWaiter ThenWaitOn(this ValueTask task1, ValueTask task2) => new ValueTaskWaiter(task1, task2);

		/// <summary>
		/// Waits on two or more <see cref="ValueTask"/> instances
		/// </summary>
		/// <param name="task1">The first task to wait on</param>
		/// <param name="task2">The second task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public static ValueTaskWaiter<T> ThenWaitOn<T>(this ValueTask<T> task1, ValueTask task2) => new ValueTaskWaiter<T>(task2, task1);

		/// <summary>
		/// Waits on two or more <see cref="ValueTask"/> instances
		/// </summary>
		/// <param name="task1">The first task to wait on</param>
		/// <param name="task2">The second task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public static ValueTaskWaiter<T> ThenWaitOn<T>(this ValueTask task1, ValueTask<T> task2) => new ValueTaskWaiter<T>(task1, task2);

		/// <summary>
		/// Waits on two or more <see cref="ValueTask"/> instances
		/// </summary>
		/// <param name="task1">The first task to wait on</param>
		/// <param name="task2">The second task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public static ValueTaskWaiter<T> ThenWaitOn<T>(this ValueTask<T> task1, ValueTask<T> task2) => new ValueTaskWaiter<T>(task1, task2);

		/// <summary>
		/// Waits on two or more <see cref="ValueTask"/> instances
		/// </summary>
		/// <param name="task1">The first task to wait on</param>
		/// <param name="task2">The second task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public static ValueTaskWaiter<T1,T2> ThenWaitOn<T1,T2>(this ValueTask<T1> task1, ValueTask<T2> task2) => new ValueTaskWaiter<T1, T2>(task1, task2);

		//****************************************

		internal static ValueTask AsValueTask(this Task task) => new ValueTask(task);

		internal static ValueTask<TResult> AsValueTask<TResult>(this Task<TResult> task) => new ValueTask<TResult>(task);

		//****************************************

		private static async ValueTask<T[]> InternalWhenAll<T>(IReadOnlyCollection<ValueTask<T>> tasks)
		{
			var Results = new T[tasks.Count];
			List<Exception> Exceptions = null;
			var Index = 0;

			foreach (var Task in tasks)
			{
				try
				{
					Results[Index++] = await Task;
				}
				catch (Exception e)
				{
					if (Exceptions == null)
						Exceptions = new List<Exception>();

					Exceptions.Add(e);
				}
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return Results;
		}

		//****************************************

		private sealed class ValueTaskWhenToken : IValueTaskSource
		{ //****************************************
			private static readonly ConcurrentBag<ValueTaskWhenToken> _Cache = new ConcurrentBag<ValueTaskWhenToken>();
			//****************************************
			private readonly Action _ContinueWrappedTask;

#if NETSTANDARD2_0
			private readonly ManualResetValueTaskSourceCore<VoidStruct> _TaskSource = new ManualResetValueTaskSourceCore<VoidStruct>();
#else
			private ManualResetValueTaskSourceCore<VoidStruct> _TaskSource = new ManualResetValueTaskSourceCore<VoidStruct>();
#endif

			private ValueTask _InnerTask;
			private CancellationToken _Token;
			private CancellationTokenSource _TokenSource;
			private CancellationTokenRegistration _Registration;
			private volatile int _CanCancel;
			//****************************************

			public ValueTaskWhenToken() => _ContinueWrappedTask = ContinueWrappedTask;

			//****************************************

			public void Initialise(ValueTask task, CancellationTokenSource tokenSource, CancellationToken token)
			{
				_InnerTask = task;
				_TokenSource = tokenSource;
				_Token = token;
				_CanCancel = 1;

				_Registration = token.Register(CancelTask, this);

				try
				{
					var Awaiter = task.GetAwaiter();

					if (Awaiter.IsCompleted)
						ContinueWrappedTask();
					else
						Awaiter.OnCompleted(_ContinueWrappedTask);
				}
				catch (Exception e)
				{
					if (_CanCancel == 0 || Interlocked.CompareExchange(ref _CanCancel, 2, 1) != 1)
						return; // Task has already completed, can no longer cancel

					_TaskSource.SetException(e);
				}
			}

			//****************************************

			private void ContinueWrappedTask()
			{
				var Awaiter = _InnerTask.GetAwaiter();

				try
				{
					Awaiter.GetResult();

					if (_CanCancel == 0 || Interlocked.CompareExchange(ref _CanCancel, 2, 1) != 1)
						return; // Task has already completed, can no longer cancel

					_TaskSource.SetResult(default);
				}
				catch (Exception e)
				{
					if (_CanCancel == 0 || Interlocked.CompareExchange(ref _CanCancel, 2, 1) != 1)
						return; // Task has already completed, can no longer cancel

					_TaskSource.SetException(e);
				}
			}

			void IValueTaskSource.GetResult(short token)
			{
				try
				{
					_TaskSource.GetResult(token);
				}
				finally
				{
					_TaskSource.Reset();
					_Token = default;
					_Registration.Dispose();
					_InnerTask = default;

					_TokenSource?.Dispose();
					_TokenSource = null;

					_Cache.Add(this);
				}
			}

			ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _TaskSource.GetStatus(token);

			void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			//****************************************

			public short Token => _TaskSource.Version;

			//****************************************

			private static void CancelTask(object state)
			{
				var Task = (ValueTaskWhenToken)state;

				if (Task._CanCancel == 0 || Interlocked.CompareExchange(ref Task._CanCancel, 2, 1) != 1)
					return; // Task has already completed, can no longer cancel

				Task._TaskSource.SetException(new OperationCanceledException(Task._Token));
			}

			//****************************************

			internal static ValueTaskWhenToken Retrieve(ValueTask task, CancellationToken token)
			{
				if (!_Cache.TryTake(out var TaskWhen))
					TaskWhen = new ValueTaskWhenToken();

				TaskWhen.Initialise(task, null, token);

				return TaskWhen;
			}
		}

		private sealed class ValueTaskWhenToken<TResult> : IValueTaskSource<TResult>
		{ //****************************************
			private static readonly ConcurrentBag<ValueTaskWhenToken<TResult>> _Cache = new ConcurrentBag<ValueTaskWhenToken<TResult>>();
			//****************************************
			private readonly Action _ContinueWrappedTask;

#if NETSTANDARD2_0
			private readonly ManualResetValueTaskSourceCore<TResult> _TaskSource = new ManualResetValueTaskSourceCore<TResult>();
#else
			private ManualResetValueTaskSourceCore<TResult> _TaskSource = new ManualResetValueTaskSourceCore<TResult>();
#endif

			private ValueTask<TResult> _InnerTask;
			private CancellationToken _Token;
			private CancellationTokenSource _TokenSource;
			private CancellationTokenRegistration _Registration;
			private volatile int _CanCancel;
			//****************************************

			public ValueTaskWhenToken() => _ContinueWrappedTask = ContinueWrappedTask;

			//****************************************

			public void Initialise(ValueTask<TResult> task, CancellationTokenSource tokenSource, CancellationToken token)
			{
				_InnerTask = task;
				_Token = token;
				_TokenSource = tokenSource;
				_CanCancel = 1;

				_Registration = token.Register(CancelTask, this);

				try
				{
					var Awaiter = task.GetAwaiter();

					if (Awaiter.IsCompleted)
						ContinueWrappedTask();
					else
						Awaiter.OnCompleted(_ContinueWrappedTask);
				}
				catch (Exception e)
				{
					if (_CanCancel == 0 || Interlocked.CompareExchange(ref _CanCancel, 2, 1) != 1)
						return; // Task has already completed, can no longer cancel

					_TaskSource.SetException(e);
				}
			}

			//****************************************

			private void ContinueWrappedTask()
			{
				var Awaiter = _InnerTask.GetAwaiter();

				try
				{
					Awaiter.GetResult();

					if (_CanCancel == 0 || Interlocked.CompareExchange(ref _CanCancel, 2, 1) != 1)
						return; // Task has already completed, can no longer cancel

					_TaskSource.SetResult(default);
				}
				catch (Exception e)
				{
					if (_CanCancel == 0 || Interlocked.CompareExchange(ref _CanCancel, 2, 1) != 1)
						return; // Task has already completed, can no longer cancel

					_TaskSource.SetException(e);
				}
			}

			TResult IValueTaskSource<TResult>.GetResult(short token)
			{
				try
				{
					return _TaskSource.GetResult(token);
				}
				finally
				{
					_TaskSource.Reset();
					_Token = default;
					_Registration.Dispose();
					_InnerTask = default;

					_TokenSource?.Dispose();
					_TokenSource = null;

					_Cache.Add(this);
				}
			}

			ValueTaskSourceStatus IValueTaskSource<TResult>.GetStatus(short token) => _TaskSource.GetStatus(token);

			void IValueTaskSource<TResult>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			//****************************************

			public short Token => _TaskSource.Version;

			//****************************************

			private static void CancelTask(object state)
			{
				var Task = (ValueTaskWhenToken<TResult>)state;

				if (Task._CanCancel == 0 || Interlocked.CompareExchange(ref Task._CanCancel, 2, 1) != 1)
					return; // Task has already completed, can no longer cancel

				Task._TaskSource.SetException(new OperationCanceledException(Task._Token));
			}

			//****************************************

			internal static ValueTaskWhenToken<TResult> Retrieve(ValueTask<TResult> task, CancellationToken token)
			{
				if (!_Cache.TryTake(out var TaskWhen))
					TaskWhen = new ValueTaskWhenToken<TResult>();

				TaskWhen.Initialise(task, null, token);

				return TaskWhen;
			}
		}
	}
}
