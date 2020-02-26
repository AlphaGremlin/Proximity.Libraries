using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter
	{
		internal ValueTaskWaiter(ValueTask task) => Tasks = ImmutableStack<ValueTask>.Empty.Push(task);

		internal ValueTaskWaiter(ValueTask task1, ValueTask task2) => Tasks = ImmutableStack<ValueTask>.Empty.Push(task1).Push(task2);

		internal ValueTaskWaiter(ImmutableStack<ValueTask> tasks) => Tasks = tasks;

		//****************************************

		/// <summary>
		/// Adds another <see cref="ValueTask"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter ThenWaitOn(ValueTask task) => new ValueTaskWaiter((Tasks ?? ImmutableStack<ValueTask>.Empty).Push(task));

		/// <summary>
		/// Adds a <see cref="ValueTask{T}"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter<T> ThenWaitOn<T>(ValueTask<T> task) => new ValueTaskWaiter<T>(this, task);

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter GetAwaiter() => ValueTaskExtensions.WhenAll(Tasks ?? ImmutableStack<ValueTask>.Empty).GetAwaiter();

		//****************************************

		internal ImmutableStack<ValueTask> Tasks { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter<T1>
	{ //****************************************
		private readonly ValueTaskWaiter _Waiter;
		//****************************************

		internal ValueTaskWaiter(ValueTask<T1> task)
		{
			_Waiter = default;
			Tasks1 = ImmutableStack<ValueTask<T1>>.Empty.Push(task);
			Count1 = 1;
		}

		internal ValueTaskWaiter(ValueTask<T1> task1, ValueTask<T1> task2)
		{
			_Waiter = default;
			Tasks1 = ImmutableStack<ValueTask<T1>>.Empty.Push(task1).Push(task2);
			Count1 = 2;
		}

		internal ValueTaskWaiter(ValueTask task1, ValueTask<T1> task2)
		{
			_Waiter = new ValueTaskWaiter(task1);
			Tasks1 = ImmutableStack<ValueTask<T1>>.Empty.Push(task2);
			Count1 = 1;
		}

		internal ValueTaskWaiter(ValueTaskWaiter waiter, ValueTask<T1> task)
		{
			_Waiter = waiter;
			Tasks1 = ImmutableStack<ValueTask<T1>>.Empty.Push(task);
			Count1 = 1;
		}

		internal ValueTaskWaiter(ValueTaskWaiter waiter, ImmutableStack<ValueTask<T1>> tasks, int count)
		{
			_Waiter = waiter;
			Tasks1 = tasks;
			Count1 = count;
		}

		//****************************************

		/// <summary>
		/// Adds another <see cref="ValueTask"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter<T1> ThenWaitOn(ValueTask task) => new ValueTaskWaiter<T1>(_Waiter.ThenWaitOn(task), Tasks1, Count1);

		/// <summary>
		/// Adds another <see cref="ValueTask{T1}"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter<T1> ThenWaitOn(ValueTask<T1> task) => new ValueTaskWaiter<T1>(_Waiter, (Tasks1 ?? ImmutableStack<ValueTask<T1>>.Empty).Push(task), Count1 + 1);

		/// <summary>
		/// Adds a <see cref="ValueTask{T2}"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter<T1, T2> ThenWaitOn<T2>(ValueTask<T2> task) => new ValueTaskWaiter<T1, T2>(this, task);

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter<T1[]> GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask<T1[]> WhenAll()
		{
			List<Exception> Exceptions = null;
			var Results = new T1[Count1];

			foreach (var Task in _Waiter.Tasks ?? ImmutableStack<ValueTask>.Empty)
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

			var Index = 0;

			foreach (var Task in Tasks1 ?? ImmutableStack<ValueTask<T1>>.Empty)
			{
				try
				{
					Results[Index] = await Task;
				}
				catch (Exception e)
				{
					if (Exceptions == null)
						Exceptions = new List<Exception>();

					Exceptions.Add(e);
				}

				Index++;
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return Results;
		}

		//****************************************

		internal ImmutableStack<ValueTask> Tasks => _Waiter.Tasks;

		internal ImmutableStack<ValueTask<T1>> Tasks1 { get; }

		internal int Count1 { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter<T1, T2>
	{ //****************************************
		private readonly ValueTaskWaiter<T1> _Waiter;
		//****************************************

		internal ValueTaskWaiter(ValueTask<T1> task1, ValueTask<T2> task2)
		{
			_Waiter = new ValueTaskWaiter<T1>(task1);
			Tasks2 = ImmutableStack<ValueTask<T2>>.Empty.Push(task2);
			Count2 = 1;
		}

		internal ValueTaskWaiter(ValueTaskWaiter<T1> waiter, ValueTask<T2> task)
		{
			_Waiter = waiter;
			Tasks2 = ImmutableStack<ValueTask<T2>>.Empty.Push(task);
			Count2 = 1;
		}

		internal ValueTaskWaiter(ValueTaskWaiter<T1> waiter, ImmutableStack<ValueTask<T2>> tasks, int count)
		{
			_Waiter = waiter;
			Tasks2 = tasks;
			Count2 = count;
		}

		//****************************************

		/// <summary>
		/// Adds another <see cref="ValueTask"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter<T1,T2> ThenWaitOn(ValueTask task) => new ValueTaskWaiter<T1, T2>(_Waiter.ThenWaitOn(task), Tasks2, Count2);

		/// <summary>
		/// Adds another <see cref="ValueTask{T1}"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter<T1,T2> ThenWaitOn(ValueTask<T1> task) => new ValueTaskWaiter<T1, T2>(_Waiter.ThenWaitOn(task), Tasks2, Count2);

		/// <summary>
		/// Adds another <see cref="ValueTask{T2}"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter<T1, T2> ThenWaitOn(ValueTask<T2> task) => new ValueTaskWaiter<T1, T2>(_Waiter, (Tasks2 ?? ImmutableStack<ValueTask<T2>>.Empty).Push(task), Count2 + 1);

		/// <summary>
		/// Adds a <see cref="ValueTask{T3}"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter<T1, T2, T3> ThenWaitOn<T3>(ValueTask<T3> task) => new ValueTaskWaiter<T1, T2, T3>(this, task);

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter<(T1[], T2[])> GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask<(T1[], T2[])> WhenAll()
		{
			List<Exception> Exceptions = null;
			var Results1 = new T1[_Waiter.Count1];
			var Results2 = new T2[Count2];

			foreach (var Task in _Waiter.Tasks ?? ImmutableStack<ValueTask>.Empty)
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

			var Index = 0;

			foreach (var Task in _Waiter.Tasks1 ?? ImmutableStack<ValueTask<T1>>.Empty)
			{
				try
				{
					Results1[Index] = await Task;
				}
				catch (Exception e)
				{
					if (Exceptions == null)
						Exceptions = new List<Exception>();

					Exceptions.Add(e);
				}

				Index++;
			}

			Index = 0;

			foreach (var Task in Tasks2 ?? ImmutableStack<ValueTask<T2>>.Empty)
			{
				try
				{
					Results2[Index] = await Task;
				}
				catch (Exception e)
				{
					if (Exceptions == null)
						Exceptions = new List<Exception>();

					Exceptions.Add(e);
				}

				Index++;
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return (Results1, Results2);
		}

		//****************************************

		internal ImmutableStack<ValueTask> Tasks => _Waiter.Tasks;

		internal ImmutableStack<ValueTask<T1>> Tasks1 => _Waiter.Tasks1;

		internal ImmutableStack<ValueTask<T2>> Tasks2 { get; }

		internal int Count1 => _Waiter.Count1;

		internal int Count2 { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter<T1, T2, T3>
	{ //****************************************
		private readonly ValueTaskWaiter<T1, T2> _Waiter;
		//****************************************

		internal ValueTaskWaiter(ValueTaskWaiter<T1, T2> waiter, ValueTask<T3> task)
		{
			_Waiter = waiter;
			Tasks3 = ImmutableStack<ValueTask<T3>>.Empty.Push(task);
			Count3 = 1;
		}

		internal ValueTaskWaiter(ValueTaskWaiter<T1, T2> waiter, ImmutableStack<ValueTask<T3>> tasks, int count)
		{
			_Waiter = waiter;
			Tasks3 = tasks;
			Count3 = count;
		}

		//****************************************

		/// <summary>
		/// Adds another <see cref="ValueTask"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter<T1, T2, T3> ThenWaitOn(ValueTask task) => new ValueTaskWaiter<T1, T2, T3>(_Waiter.ThenWaitOn(task), Tasks3, Count3);

		/// <summary>
		/// Adds another <see cref="ValueTask{T1}"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter<T1, T2, T3> ThenWaitOn(ValueTask<T1> task) => new ValueTaskWaiter<T1, T2, T3>(_Waiter.ThenWaitOn(task), Tasks3, Count3);

		/// <summary>
		/// Adds another <see cref="ValueTask{T2}"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter<T1, T2, T3> ThenWaitOn(ValueTask<T2> task) => new ValueTaskWaiter<T1, T2, T3>(_Waiter.ThenWaitOn(task), Tasks3, Count3);

		/// <summary>
		/// Adds another <see cref="ValueTask{T3}"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter<T1, T2, T3> ThenWaitOn(ValueTask<T3> task) => new ValueTaskWaiter<T1, T2, T3>(_Waiter, (Tasks3 ?? ImmutableStack<ValueTask<T3>>.Empty).Push(task), Count3 + 1);

		//public ValueTaskWaiter<T1, T2, T3> ThenWaitOn<T3>(ValueTask<T2> task) => new ValueTaskWaiter<T1, T2>(this, task);

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter<(T1[], T2[], T3[])> GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask<(T1[], T2[], T3[])> WhenAll()
		{
			List<Exception> Exceptions = null;
			var Results1 = new T1[_Waiter.Count1];
			var Results2 = new T2[_Waiter.Count2];
			var Results3 = new T3[Count3];

			foreach (var Task in _Waiter.Tasks ?? ImmutableStack<ValueTask>.Empty)
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

			var Index = 0;

			foreach (var Task in _Waiter.Tasks1 ?? ImmutableStack<ValueTask<T1>>.Empty)
			{
				try
				{
					Results1[Index] = await Task;
				}
				catch (Exception e)
				{
					if (Exceptions == null)
						Exceptions = new List<Exception>();

					Exceptions.Add(e);
				}

				Index++;
			}

			Index = 0;

			foreach (var Task in _Waiter.Tasks2 ?? ImmutableStack<ValueTask<T2>>.Empty)
			{
				try
				{
					Results2[Index] = await Task;
				}
				catch (Exception e)
				{
					if (Exceptions == null)
						Exceptions = new List<Exception>();

					Exceptions.Add(e);
				}

				Index++;
			}

			Index = 0;

			foreach (var Task in Tasks3 ?? ImmutableStack<ValueTask<T3>>.Empty)
			{
				try
				{
					Results3[Index] = await Task;
				}
				catch (Exception e)
				{
					if (Exceptions == null)
						Exceptions = new List<Exception>();

					Exceptions.Add(e);
				}

				Index++;
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return (Results1, Results2, Results3);
		}

		//****************************************

		internal ImmutableStack<ValueTask<T3>> Tasks3 { get; }

		internal int Count3 { get; }
	}
}
