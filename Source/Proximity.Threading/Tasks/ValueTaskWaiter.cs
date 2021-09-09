using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> instances
	/// </summary>
	public static class ValueTaskWaiter
	{
		/// <summary>
		/// Waits on two or more <see cref="ValueTask"/> instances
		/// </summary>
		/// <param name="task1">The first task to wait on</param>
		/// <param name="task2">The second task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public static ValueTaskWaiter2 ThenWaitOn(this ValueTask task1, ValueTask task2) => new(task1, task2);

		/// <summary>
		/// Waits on two or more <see cref="ValueTask"/> instances
		/// </summary>
		/// <param name="task1">The first task to wait on</param>
		/// <param name="task2">The second task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public static ValueTaskWaiter2<T1> ThenWaitOn<T1>(this ValueTask<T1> task1, ValueTask task2) => new(task1, task2);

		/// <summary>
		/// Waits on two or more <see cref="ValueTask"/> instances
		/// </summary>
		/// <param name="task1">The first task to wait on</param>
		/// <param name="task2">The second task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public static ValueTaskWaiter2B<T2> ThenWaitOn<T2>(this ValueTask task1, ValueTask<T2> task2) => new(task1, task2);

		/// <summary>
		/// Waits on two or more <see cref="ValueTask"/> instances
		/// </summary>
		/// <param name="task1">The first task to wait on</param>
		/// <param name="task2">The second task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public static ValueTaskWaiter2<T1, T2> ThenWaitOn<T1, T2>(this ValueTask<T1> task1, ValueTask<T2> task2) => new(task1, task2);
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter2
	{
		internal ValueTaskWaiter2(ValueTask task1, ValueTask task2) => (Task1, Task2) = (task1, task2);

		//****************************************

		/// <summary>
		/// Adds another <see cref="ValueTask"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter3 Then(ValueTask task) => new(Task1, Task2, task);

		/// <summary>
		/// Adds a <see cref="ValueTask{T}"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter3C<T3> Then<T3>(ValueTask<T3> task) => new(Task1, Task2, task);

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask WhenAll()
		{
			List<Exception>? Exceptions = null;

			try
			{
				await Task1;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				await Task2;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);
		}

		//****************************************

		internal ValueTask Task1 { get; }

		internal ValueTask Task2 { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter2<T1>
	{
		internal ValueTaskWaiter2(ValueTask<T1> task1, ValueTask task2) => (Task1, Task2) = (task1, task2);

		//****************************************

		/// <summary>
		/// Adds another <see cref="ValueTask"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter3<T1> Then(ValueTask task) => new(Task1, Task2, task);

		/// <summary>
		/// Adds a <see cref="ValueTask{T2}"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter3A<T1, T3> Then<T3>(ValueTask<T3> task) => new(Task1, Task2, task);

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter<T1> GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask<T1> WhenAll()
		{
			List<Exception>? Exceptions = null;
			T1 Result = default!;

			try
			{
				Result = await Task1;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				await Task2;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return Result;
		}

		//****************************************

		internal ValueTask<T1> Task1 { get; }

		internal ValueTask Task2 { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter2B<T2>
	{
		internal ValueTaskWaiter2B(ValueTask task1, ValueTask<T2> task2) => (Task1, Task2) = (task1, task2);

		//****************************************

		/// <summary>
		/// Adds another <see cref="ValueTask"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter3B<T2> Then(ValueTask task) => new(Task1, Task2, task);

		/// <summary>
		/// Adds a <see cref="ValueTask{T2}"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter3B<T2, T3> Then<T3>(ValueTask<T3> task) => new(Task1, Task2, task);

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter<T2> GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask<T2> WhenAll()
		{
			List<Exception>? Exceptions = null;
			T2 Result = default!;

			try
			{
				await Task1;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				Result = await Task2;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return Result;
		}

		//****************************************

		internal ValueTask Task1 { get; }

		internal ValueTask<T2> Task2 { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter2<T1, T2>
	{
		internal ValueTaskWaiter2(ValueTask<T1> task1, ValueTask<T2> task2) => (Task1, Task2) = (task1, task2);

		//****************************************

		/// <summary>
		/// Adds another <see cref="ValueTask"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter3<T1,T2> Then(ValueTask task) => new(Task1, Task2, task);

		/// <summary>
		/// Adds a <see cref="ValueTask{T3}"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskWaiter3<T1, T2, T3> Then<T3>(ValueTask<T3> task) => new(Task1, Task2, task);

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter<(T1, T2)> GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask<(T1, T2)> WhenAll()
		{
			List<Exception>? Exceptions = null;
			T1 Result1 = default!;
			T2 Result2 = default!;

			try
			{
				Result1 = await Task1;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				Result2 = await Task2;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return (Result1, Result2);
		}

		//****************************************

		internal ValueTask<T1> Task1 { get; }

		internal ValueTask<T2> Task2 { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter3
	{
		internal ValueTaskWaiter3(ValueTask task1, ValueTask task2, ValueTask task3) => (Task1, Task2, Task3) = (task1, task2, task3);

		//****************************************

		/// <summary>
		/// Adds another <see cref="ValueTask"/> to the waiter
		/// </summary>
		/// <param name="task">The task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public ValueTaskMultiWaiter Then(ValueTask task) => new(ImmutableStack.Create(Task1, Task2, Task3, task));

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask WhenAll()
		{
			List<Exception>? Exceptions = null;

			try
			{
				await Task1;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				await Task2;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				await Task3;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);
		}

		//****************************************

		internal ValueTask Task1 { get; }

		internal ValueTask Task2 { get; }

		internal ValueTask Task3 { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter3<T1>
	{
		internal ValueTaskWaiter3(ValueTask<T1> task1, ValueTask task2, ValueTask task3) => (Task1, Task2, Task3) = (task1, task2, task3);

		//****************************************

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter<T1> GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask<T1> WhenAll()
		{
			List<Exception>? Exceptions = null;
			T1 Result = default!;

			try
			{
				Result = await Task1;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				await Task2;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				await Task3;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return Result;
		}

		//****************************************

		internal ValueTask<T1> Task1 { get; }

		internal ValueTask Task2 { get; }

		internal ValueTask Task3 { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter3A<T1, T3>
	{
		internal ValueTaskWaiter3A(ValueTask<T1> task1, ValueTask task2, ValueTask<T3> task3) => (Task1, Task2, Task3) = (task1, task2, task3);

		//****************************************

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter<(T1, T3)> GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask<(T1, T3)> WhenAll()
		{
			List<Exception>? Exceptions = null;
			T1 Result1 = default!;
			T3 Result3 = default!;

			try
			{
				Result1 = await Task1;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				await Task2;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				Result3 = await Task3;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return (Result1, Result3);
		}

		//****************************************

		internal ValueTask<T1> Task1 { get; }

		internal ValueTask Task2 { get; }

		internal ValueTask<T3> Task3 { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter3B<T2>
	{
		internal ValueTaskWaiter3B(ValueTask task1, ValueTask<T2> task2, ValueTask task3) => (Task1, Task2, Task3) = (task1, task2, task3);

		//****************************************

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter<T2> GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask<T2> WhenAll()
		{
			List<Exception>? Exceptions = null;
			T2 Result = default!;

			try
			{
				await Task1;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				Result = await Task2;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				await Task3;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return Result;
		}

		//****************************************

		internal ValueTask Task1 { get; }

		internal ValueTask<T2> Task2 { get; }

		internal ValueTask Task3 { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter3B<T2, T3>
	{
		internal ValueTaskWaiter3B(ValueTask task1, ValueTask<T2> task2, ValueTask<T3> task3) => (Task1, Task2, Task3) = (task1, task2, task3);

		//****************************************

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter<(T2, T3)> GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask<(T2, T3)> WhenAll()
		{
			List<Exception>? Exceptions = null;
			T2 Result2 = default!;
			T3 Result3 = default!;

			try
			{
				await Task1;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				Result2 = await Task2;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				Result3 = await Task3;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return (Result2, Result3);
		}

		//****************************************

		internal ValueTask Task1 { get; }

		internal ValueTask<T2> Task2 { get; }

		internal ValueTask<T3> Task3 { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter3C<T3>
	{
		internal ValueTaskWaiter3C(ValueTask task1, ValueTask task2, ValueTask<T3> task3) => (Task1, Task2, Task3) = (task1, task2, task3);

		//****************************************

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter<T3> GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask<T3> WhenAll()
		{
			List<Exception>? Exceptions = null;
			T3 Result = default!;

			try
			{
				await Task1;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				 await Task2;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				Result = await Task3;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return Result;
		}

		//****************************************

		internal ValueTask Task1 { get; }

		internal ValueTask Task2 { get; }

		internal ValueTask<T3> Task3 { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter3<T1, T2>
	{
		internal ValueTaskWaiter3(ValueTask<T1> task1, ValueTask<T2> task2, ValueTask task3) => (Task1, Task2, Task3) = (task1, task2, task3);

		//****************************************

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter<(T1, T2)> GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask<(T1, T2)> WhenAll()
		{
			List<Exception>? Exceptions = null;
			T1 Result1 = default!;
			T2 Result2 = default!;

			try
			{
				Result1 = await Task1;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				Result2 = await Task2;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				await Task3;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return (Result1, Result2);
		}

		//****************************************

		internal ValueTask<T1> Task1 { get; }

		internal ValueTask<T2> Task2 { get; }

		internal ValueTask Task3 { get; }
	}

	/// <summary>
	/// Allows a fluent syntax for awaiting multiple <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> instances
	/// </summary>
	public readonly struct ValueTaskWaiter3<T1, T2, T3>
	{
		internal ValueTaskWaiter3(ValueTask<T1> task1, ValueTask<T2> task2, ValueTask<T3> task3) => (Task1, Task2, Task3) = (task1, task2, task3);

		//****************************************

		/// <summary>
		/// Gets an awaiter for the supplied tasks
		/// </summary>
		/// <returns>An awaiter that waits on all the supplied tasks</returns>
		public ValueTaskAwaiter<(T1, T2, T3)> GetAwaiter() => WhenAll().GetAwaiter();

		//****************************************

		private async ValueTask<(T1, T2, T3)> WhenAll()
		{
			List<Exception>? Exceptions = null;
			T1 Result1 = default!;
			T2 Result2 = default!;
			T3 Result3 = default!;

			try
			{
				Result1 = await Task1;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				Result2 = await Task2;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			try
			{
				Result3 = await Task3;
			}
			catch (Exception e)
			{
				if (Exceptions == null)
					Exceptions = new List<Exception>();

				Exceptions.Add(e);
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return (Result1, Result2, Result3);
		}

		//****************************************

		internal ValueTask<T1> Task1 { get; }

		internal ValueTask<T2> Task2 { get; }

		internal ValueTask<T3> Task3 { get; }
	}
}
