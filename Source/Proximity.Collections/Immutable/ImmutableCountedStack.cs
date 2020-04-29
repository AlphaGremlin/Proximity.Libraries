using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;
//****************************************

namespace System.Collections.Immutable
{
	/// <summary>
	/// Provides methods for creating an ImmutableCountedStack
	/// </summary>
	public static class ImmutableCountedStack
	{
		/// <summary>
		/// Creates a new Immutable Counted Stack
		/// </summary>
		/// <returns>An empty Immutable Counted Stack</returns>
		public static ImmutableCountedStack<T> Create<T>() => ImmutableCountedStack<T>.Empty;

		/// <summary>
		/// Creates a new Immutable Counted Stack populated with one item
		/// </summary>
		/// <param name="item">The item to add to the stack</param>
		/// <returns>An Immutable Counted Stack populated with the given item</returns>
		public static ImmutableCountedStack<T> Create<T>(T item) => new ImmutableCountedStack<T>(item, ImmutableCountedStack<T>.Empty, 1);

		/// <summary>
		/// Creates a new Immutable Counted Stack populated with an array of items
		/// </summary>
		/// <param name="items">An array of items to add to the stack</param>
		/// <returns>An Immutable Counted Stack populated with the given items</returns>
		public static ImmutableCountedStack<T> Create<T>(params T[] items)
		{
			var NewStack = ImmutableCountedStack<T>.Empty;
			
			for(var Index = 0; Index < items.Length; Index++)
			{
				NewStack = NewStack.Push(items[Index]);
			}
			
			return NewStack;
		}
		
		/// <summary>
		/// Creates a new Immutable Counted Stack populated with a set of items
		/// </summary>
		/// <param name="items">An enumerable object giving the items to add to the stack</param>
		/// <returns>An Immutable Counted Stack populated with the given items</returns>
		public static ImmutableCountedStack<T> CreateRange<T>(IEnumerable<T> items)
		{
			var NewStack = ImmutableCountedStack<T>.Empty;
			
			foreach(var MyItem in items)
			{
				NewStack = NewStack.Push(MyItem);
			}
			
			return NewStack;
		}
		
		/// <summary>
		/// Performs an atomic push onto an immutable counted stack
		/// </summary>
		/// <param name="stack">The location of the counted stack to push onto</param>
		/// <param name="item">The item to push</param>
		public static void Push<T>(ref ImmutableCountedStack<T> stack, T item)
		{	//****************************************
			ImmutableCountedStack<T> OldStack, NewStack;
			//****************************************
			
			do
			{
				OldStack = stack;
				NewStack = OldStack.Push(item);
			} while (Interlocked.CompareExchange(ref stack, NewStack, OldStack) != OldStack);
		}
		
		/// <summary>
		/// Performs an atomic pop from an immutable counted stack
		/// </summary>
		/// <param name="stack">The location of the counted stack to pop from</param>
		/// <param name="item">A variable that receives the popped item</param>
		/// <returns>True if an item was popped, or False if the stack was empty</returns>
		public static bool TryPop<T>(ref ImmutableCountedStack<T> stack,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out T item)
		{	//****************************************
			ImmutableCountedStack<T> OldStack, NewStack;
			//****************************************
			
			do
			{
				OldStack = stack;
				
				if (OldStack.IsEmpty)
				{
					item = default!;
					return false;
				}
				
				NewStack = OldStack.Pop(out item);
			} while (Interlocked.CompareExchange(ref stack, NewStack, OldStack) != OldStack);
			
			return true;
		}
	}
	
	/// <summary>
	/// Provides an Immutable Stack that also maintains a counter
	/// </summary>
	[Serializable]
	public sealed class ImmutableCountedStack<T> : /*ISerializable,*/ IEnumerable<T>, IImmutableStack<T>, IReadOnlyCollection<T>
	{
		/// <summary>
		/// An empty immutable counted stack
		/// </summary>
		public static readonly ImmutableCountedStack<T> Empty = new ImmutableCountedStack<T>(default!, null, 0);

		//****************************************
		private readonly T _Head;
		private readonly ImmutableCountedStack<T>? _Tail;
		//****************************************

		internal ImmutableCountedStack(T head, ImmutableCountedStack<T>? tail, int count)
		{
			_Head = head;
			_Tail = tail;
			Count = count;
		}

		//****************************************

		/// <summary>
		/// Clears the immutable stack
		/// </summary>
		/// <returns>An empty stack</returns>
		public ImmutableCountedStack<T> Clear() => Empty;

		/// <summary>
		/// Copies the contents of the stack in oldest-to-newest order to an array
		/// </summary>
		/// <param name="array">The array to copy to</param>
		/// <param name="index">The index into the array to start writing at</param>
		public void CopyTo(T[] array, int index)
		{	//****************************************
			var Node = this;
			//****************************************

			index += Count;

			while (Node.Count > 0)
			{
				array[--index] = Node._Head;

				// Assume non-null, since Tail is only null when count is 0
				Node = Node._Tail!;
			}
		}

		/// <summary>
		/// Retrieves an enumerator for this stack
		/// </summary>
		/// <returns>The requested enumerator</returns>
		public Enumerator GetEnumerator() => new Enumerator(this);

		/// <summary>
		/// Peeks at the top item on the stack
		/// </summary>
		/// <returns>The top item</returns>
		/// <exception cref="InvalidOperationException">Thrown if the stack is empty</exception>
		public T Peek()
		{
			if (_Tail == null)
				throw new InvalidOperationException("Stack is empty");
			
			return _Head;
		}
		
		/// <summary>
		/// Removes and discards the top item off the stack
		/// </summary>
		/// <returns>The stack with the top item removed</returns>
		/// <exception cref="InvalidOperationException">Thrown if the stack is empty</exception>
		public ImmutableCountedStack<T> Pop()
		{
			if (_Tail == null)
				throw new InvalidOperationException("Stack is empty");
			
			return _Tail;
		}
		
		/// <summary>
		/// Retrieves and removes the top item on the stack
		/// </summary>
		/// <param name="item">A reference that receives the top item</param>
		/// <returns>The stack with the top item removed</returns>
		/// <exception cref="InvalidOperationException">Thrown if the stack is empty</exception>
		public ImmutableCountedStack<T> Pop(out T item)
		{
			if (_Tail == null)
				throw new InvalidOperationException("Stack is empty");
			
			item = _Head;
			
			return _Tail;
		}

		/// <summary>
		/// Pushes an item onto the top of the stack
		/// </summary>
		/// <param name="item">The item to push</param>
		/// <returns>The stack with the new top item</returns>
		public ImmutableCountedStack<T> Push(T item) => new ImmutableCountedStack<T>(item, this, Count + 1);

		//****************************************

		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

		IImmutableStack<T> IImmutableStack<T>.Clear() => Empty;

		IImmutableStack<T> IImmutableStack<T>.Pop()
		{
			if (_Tail == null)
				throw new InvalidOperationException("Stack is empty");
			
			return _Tail;
		}

		IImmutableStack<T> IImmutableStack<T>.Push(T item) => new ImmutableCountedStack<T>(item, this, Count + 1);

		//****************************************

		/// <summary>
		/// Gets whether this stack is empty
		/// </summary>
		public bool IsEmpty => _Tail == null;

		/// <summary>
		/// Gets the number of items on the stack
		/// </summary>
		public int Count { get; }

		//****************************************

		/// <summary>
		/// Enumerates the stack while avoiding memory allocations
		/// </summary>
		public struct Enumerator: IEnumerator<T>
		{	//****************************************
			private readonly ImmutableCountedStack<T> _Start;
			private ImmutableCountedStack<T> _Current;
			//****************************************

			internal Enumerator(ImmutableCountedStack<T> stack)
			{
				_Start = stack;
				_Current = null!;
			}
			
			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
			}

			/// <summary>
			/// Tries to move to the next item
			/// </summary>
			/// <returns>True if there's another item to enumerate, otherwise False</returns>
			public bool MoveNext()
			{
				if (_Current == null)
					_Current = _Start;
				else if (_Current._Tail != null)
					_Current = _Current._Tail;

				return !_Current.IsEmpty;
			}

			void IEnumerator.Reset()
			{
				_Current = null!;
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public T Current
			{
				get
				{
					if (_Current == null || _Current.IsEmpty)
						throw new InvalidOperationException();
					
					return _Current._Head;
				}
			}
			
			object? IEnumerator.Current
			{
				get
				{
					if (_Current == null || _Current.IsEmpty)
						throw new InvalidOperationException();
					
					return _Current._Head;
				}
			}
		}
	}
}
