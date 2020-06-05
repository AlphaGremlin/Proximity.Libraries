using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> instances
	/// </summary>
	public readonly struct ValueTaskMultiWaiter
	{
		internal ValueTaskMultiWaiter(ImmutableStack<ValueTask> tasks) => Tasks = tasks;

		//****************************************

		/// <summary>
		/// Adds another <see cref="ValueTask"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskMultiWaiter Then(ValueTask task) => new ValueTaskMultiWaiter((Tasks ?? ImmutableStack<ValueTask>.Empty).Push(task));

		/// <summary>
		/// Adds more <see cref="ValueTask"/> instances to the waiter
		/// </summary>
		/// <param name="tasks">The tasks to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskMultiWaiter Then(IEnumerable<ValueTask> tasks) => new ValueTaskMultiWaiter((Tasks ?? ImmutableStack<ValueTask>.Empty).Push(tasks, out _));

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter GetAwaiter() => ValueTaskExtensions.WhenAll(Tasks ?? ImmutableStack<ValueTask>.Empty).GetAwaiter();

		//****************************************

		internal ImmutableStack<ValueTask> Tasks { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask{T}"/> instances
	/// </summary>
	public readonly struct ValueTaskMultiWaiter<T>
	{
		internal ValueTaskMultiWaiter(ImmutableStack<ValueTask<T>> tasks, int count) => (Tasks, Count) = (tasks, count);

		//****************************************

		/// <summary>
		/// Adds another <see cref="ValueTask{T}"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskMultiWaiter<T> Then(ValueTask<T> task) => new ValueTaskMultiWaiter<T>((Tasks ?? ImmutableStack<ValueTask<T>>.Empty).Push(task), Count + 1);

		/// <summary>
		/// Adds more <see cref="ValueTask{T}"/> instances to the waiter
		/// </summary>
		/// <param name="tasks">The tasks to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskMultiWaiter<T> Then(IEnumerable<ValueTask<T>> tasks) => new ValueTaskMultiWaiter<T>((Tasks ?? ImmutableStack<ValueTask<T>>.Empty).Push(tasks, out var Added), Count + Added);

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter<T[]> GetAwaiter() => ValueTaskExtensions.WhenAll(Tasks ?? ImmutableStack<ValueTask<T>>.Empty).GetAwaiter();

		//****************************************

		internal ImmutableStack<ValueTask<T>> Tasks { get; }

		internal int Count { get; }
	}
}
