using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

namespace Proximity.Collections.Immutable
{
	/// <summary>
	/// Provides utility methods for creating <see cref="ImmutableCountedQueue{T}"/>
	/// </summary>
	public static class ImmutableCountedQueue
	{
		/// <summary>
		/// Creates a new Immutable Counted Queue
		/// </summary>
		/// <returns>An empty Immutable Counted Queue</returns>
		public static ImmutableCountedQueue<T> Create<T>() => ImmutableCountedQueue<T>.Empty;

		/// <summary>
		/// Creates a new Immutable Counted Queue populated with one item
		/// </summary>
		/// <param name="item">The item to add to the stack</param>
		/// <returns>An Immutable Counted Queue populated with the given item</returns>
		public static ImmutableCountedQueue<T> Create<T>(T item) => new(ImmutableStack<T>.Empty.Push(item), ImmutableStack<T>.Empty, 1);

		/// <summary>
		/// Creates a new Immutable Counted Queue populated with an array of items
		/// </summary>
		/// <param name="items">An array of items to add to the stack</param>
		/// <returns>An Immutable Counted Queue populated with the given items</returns>
		public static ImmutableCountedQueue<T> Create<T>(params T[] items)
		{
			var NewStack = ImmutableCountedQueue<T>.Empty;

			for (var Index = 0; Index < items.Length; Index++)
			{
				NewStack = NewStack.Enqueue(items[Index]);
			}

			return NewStack;
		}

		/// <summary>
		/// Creates a new Immutable Counted Queue populated with a set of items
		/// </summary>
		/// <param name="items">An enumerable object giving the items to add to the queue</param>
		/// <returns>An Immutable Counted Queue populated with the given items</returns>
		public static ImmutableCountedQueue<T> CreateRange<T>(IEnumerable<T> items)
		{
			var NewStack = ImmutableCountedQueue<T>.Empty;

			foreach (var MyItem in items)
			{
				NewStack = NewStack.Enqueue(MyItem);
			}

			return NewStack;
		}

		/// <summary>
		/// Performs an atomic enqueue onto an Immutable Counted Queue
		/// </summary>
		/// <param name="queue">The location of the Counted Queue to push onto</param>
		/// <param name="item">The item to push</param>
		public static void Enqueue<T>(ref ImmutableCountedQueue<T> queue, T item)
		{ //****************************************
			ImmutableCountedQueue<T> OldQueue;
			//****************************************

			do
			{
				OldQueue = queue;
			} while (Interlocked.CompareExchange(ref queue, OldQueue.Enqueue(item), OldQueue) != OldQueue);
		}

		/// <summary>
		/// Performs an atomic dequeue from an Immutable Counted Queue
		/// </summary>
		/// <param name="queue">The location of the Counted Queue to dequeuefrom</param>
		/// <param name="item">A variable that receives the dequeued item</param>
		/// <returns>True if an item was dequeued, or False if the queue was empty</returns>
		public static bool TryDequeue<T>(ref ImmutableCountedQueue<T> queue,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out T item)
		{ //****************************************
			ImmutableCountedQueue<T> OldQueue;
			//****************************************

			do
			{
				OldQueue = queue;

				if (OldQueue.IsEmpty)
				{
					item = default!;
					return false;
				}

			} while (Interlocked.CompareExchange(ref queue, OldQueue.Dequeue(out item), OldQueue) != OldQueue);

			return true;
		}
	}

	/// <summary>
	/// Provides an Immutable Queue that also maintains a counter
	/// </summary>
	public sealed class ImmutableCountedQueue<T> : IImmutableQueue<T>
	{ //****************************************
		private readonly ImmutableStack<T> _Forwards, _Backwards;

		private ImmutableStack<T>? _BackwardsReversed;
		//****************************************

		internal ImmutableCountedQueue(ImmutableStack<T> forwards, ImmutableStack<T> backwards, int count)
		{
			_Forwards = forwards;
			_Backwards = backwards;
			Count = count;
		}

		//****************************************

		/// <summary>
		/// Clears the queue
		/// </summary>
		/// <returns>An empty queue</returns>
		public ImmutableCountedQueue<T> Clear() => Empty;

		/// <summary>
		/// Dequeues the item at the front of the queue
		/// </summary>
		/// <returns>The queue without the front item</returns>
		public ImmutableCountedQueue<T> Dequeue()
		{
			var Forwards = _Forwards.Pop();

			if (!Forwards.IsEmpty)
				return new ImmutableCountedQueue<T>(Forwards, _Backwards, Count - 1);

			if (_Backwards.IsEmpty)
				return Empty;

			return new ImmutableCountedQueue<T>(BackwardsReversed, ImmutableStack<T>.Empty, Count - 1);
		}

		/// <summary>
		/// Dequeues and returns the item at the front of the queue
		/// </summary>
		/// <param name="value">Receives the value of the item at the front of the queue</param>
		/// <returns>The queue without the front item</returns>
		public ImmutableCountedQueue<T> Dequeue(out T value)
		{
			value = _Forwards.Peek();

			return Dequeue();
		}

		/// <summary>
		/// Enqueues an item at the end of the queue
		/// </summary>
		/// <param name="value">The value to enqueue</param>
		/// <returns>The queue with a new trailing item</returns>
		public ImmutableCountedQueue<T> Enqueue(T value)
		{
			if (IsEmpty)
				return new ImmutableCountedQueue<T>(ImmutableStack<T>.Empty.Push(value), ImmutableStack<T>.Empty, Count + 1);

			return new ImmutableCountedQueue<T>(_Forwards, _Backwards.Push(value), Count + 1);
		}

		/// <summary>
		/// Gets an enumerator for the queue
		/// </summary>
		/// <returns>An enumerator for the current state of the queue</returns>
		public Enumerator GetEnumerator() => new(this);

		/// <summary>
		/// Retrieves the first item in the queue
		/// </summary>
		/// <returns>The first item in the queue</returns>
		public T Peek() => _Forwards.Peek();

		/// <summary>
		/// Retrieves a read-only reference to the first item in the queue
		/// </summary>
		/// <returns>The first item in the queue</returns>
		public ref readonly T PeekRef() => ref _Forwards.PeekRef();

		/// <summary>
		/// Tries to retrieve an item from the queue
		/// </summary>
		/// <param name="value">Receives the first item in the queue, if any</param>
		/// <returns>True if the queue has one item, otherwise False</returns>
		public bool TryPeek(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out T value)
		{
			if (IsEmpty)
			{
				value = default!;

				return false;
			}

			value = _Forwards.Peek();

			return true;
		}

		//****************************************

		IImmutableQueue<T> IImmutableQueue<T>.Clear() => Clear();

		IImmutableQueue<T> IImmutableQueue<T>.Dequeue() => Dequeue();

		IImmutableQueue<T> IImmutableQueue<T>.Enqueue(T value) => Enqueue(value);

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => IsEmpty ? Enumerable.Empty<T>().GetEnumerator() : new Enumerator(this);

		IEnumerator IEnumerable.GetEnumerator() => IsEmpty ? Enumerable.Empty<T>().GetEnumerator() : new Enumerator(this);

		//****************************************

		/// <summary>
		/// Gets the number of items currently in the queue
		/// </summary>
		public int Count { get; }

		/// <summary>
		/// Gets whether the queue is empty or not
		/// </summary>
		public bool IsEmpty => _Forwards.IsEmpty;

		private ImmutableStack<T> BackwardsReversed
		{
			get
			{
				if (_BackwardsReversed == null)
				{
					var Reversed = ImmutableStack<T>.Empty;

					for (var Forwards = _Forwards; !Forwards.IsEmpty; Forwards = Forwards.Pop())
					{
						Reversed = Reversed.Push(Forwards.Peek());
					}

					_BackwardsReversed = Reversed;
				}

				return _BackwardsReversed;
			}
		}

		//****************************************

		/// <summary>
		/// An empty immutable counted queue
		/// </summary>
		public static ImmutableCountedQueue<T> Empty { get; } = new ImmutableCountedQueue<T>(ImmutableStack<T>.Empty, ImmutableStack<T>.Empty, 0);

		//****************************************

		/// <summary>
		/// Provides enumeration services for the queue
		/// </summary>
		public struct Enumerator : IEnumerator<T>
		{ //****************************************
			private readonly ImmutableCountedQueue<T> _Queue;

			private ImmutableStack<T>? _Forwards, _Backwards;
			//****************************************

			internal Enumerator(ImmutableCountedQueue<T> queue)
			{
				_Queue = queue;
				_Forwards = null;
				_Backwards = null;
			}

			//****************************************

			/// <summary>
			/// Tries to move to the next item
			/// </summary>
			/// <returns>True if there's another item to enumerate, otherwise False</returns>
			public bool MoveNext()
			{
				if (_Forwards == null)
				{
					_Forwards = _Queue._Forwards;
					_Backwards = _Queue.BackwardsReversed;
				}
				else if (!_Forwards.IsEmpty)
				{
					_Forwards = _Forwards.Pop();
				}
				else if (_Backwards!.IsEmpty)
				{
					_Backwards = _Backwards.Pop();
				}

				return !_Forwards.IsEmpty || !_Backwards!.IsEmpty;
			}

			void IDisposable.Dispose()
			{
			}

			void IEnumerator.Reset()
			{
				_Forwards = null;
				_Backwards = null;
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public T Current
			{
				get
				{
					if (_Forwards == null)
						throw new InvalidOperationException();

					if (!_Forwards.IsEmpty)
						return _Forwards.Peek();

					if (!_Backwards!.IsEmpty)
						return _Backwards.Peek();

					throw new InvalidOperationException();
				}
			}

			object? IEnumerator.Current => Current;
		}
	}
}
