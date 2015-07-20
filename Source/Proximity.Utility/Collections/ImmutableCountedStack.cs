/****************************************\
 ImmutableCountedStack.cs
 Created: 2014-04-30
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
//****************************************

namespace Proximity.Utility.Collections
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
		public static ImmutableCountedStack<TItem> Create<TItem>()
		{
			return ImmutableCountedStack<TItem>.Empty;
		}
		
		/// <summary>
		/// Creates a new Immutable Counted Stack populated with one item
		/// </summary>
		/// <param name="item">The item to add to the stack</param>
		/// <returns>An Immutable Counted Stack populated with the given item</returns>
		public static ImmutableCountedStack<TItem> Create<TItem>(TItem item)
		{
			return new ImmutableCountedStack<TItem>(item, ImmutableCountedStack<TItem>.Empty, 1);
		}
		
		/// <summary>
		/// Creates a new Immutable Counted Stack populated with an array of items
		/// </summary>
		/// <param name="items">An array of items to add to the stack</param>
		/// <returns>An Immutable Counted Stack populated with the given items</returns>
		public static ImmutableCountedStack<TItem> Create<TItem>(params TItem[] items)
		{
			var NewStack = ImmutableCountedStack<TItem>.Empty;
			
			for(int Index = 0; Index < items.Length; Index++)
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
		public static ImmutableCountedStack<TItem> CreateRange<TItem>(IEnumerable<TItem> items)
		{
			var NewStack = ImmutableCountedStack<TItem>.Empty;
			
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
		public static void Push<TItem>(ref ImmutableCountedStack<TItem> stack, TItem item)
		{	//****************************************
			ImmutableCountedStack<TItem> OldStack, NewStack;
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
		public static bool TryPop<TItem>(ref ImmutableCountedStack<TItem> stack, out TItem item)
		{	//****************************************
			ImmutableCountedStack<TItem> OldStack, NewStack;
			//****************************************
			
			do
			{
				OldStack = stack;
				
				if (OldStack.IsEmpty)
				{
					item = default(TItem);
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
	public sealed class ImmutableCountedStack<TItem> : IEnumerable<TItem>
#if NET45
		, System.Collections.Immutable.IImmutableStack<TItem>
#endif
	{
		/// <summary>
		/// An empty immutable counted stack
		/// </summary>
		public static readonly ImmutableCountedStack<TItem> Empty = new ImmutableCountedStack<TItem>(default(TItem), null, 0);
		
		//****************************************
		private readonly TItem _Head;
		private readonly ImmutableCountedStack<TItem> _Tail;
		
		private readonly int _Count;
		//****************************************
		
		internal ImmutableCountedStack(TItem head, ImmutableCountedStack<TItem> tail, int count)
		{
			_Head = head;
			_Tail = tail;
			_Count = count;
		}
		
		//****************************************
		
		/// <summary>
		/// Clears the immutable stack
		/// </summary>
		/// <returns>An empty stack</returns>
		public ImmutableCountedStack<TItem> Clear()
		{
			return Empty;
		}
		
		/// <summary>
		/// Retrieves an enumerator for this stack
		/// </summary>
		/// <returns>The requested enumerator</returns>
		public IEnumerator<TItem> GetEnumerator()
		{
			return new StackEnumerator(this);
		}
		
		/// <summary>
		/// Peeks at the top item on the stack
		/// </summary>
		/// <returns>The top item</returns>
		/// <exception cref="InvalidOperationException">Thrown if the stack is empty</exception>
		public TItem Peek()
		{
			if (IsEmpty)
				throw new InvalidOperationException("Stack is empty");
			
			return _Head;
		}
		
		/// <summary>
		/// Removes the top item off the stack
		/// </summary>
		/// <returns>The stack with the top item removed</returns>
		/// <exception cref="InvalidOperationException">Thrown if the stack is empty</exception>
		public ImmutableCountedStack<TItem> Pop()
		{
			if (IsEmpty)
				throw new InvalidOperationException("Stack is empty");
			
			return _Tail;
		}
		
		/// <summary>
		/// Retrieves and removes the top item on the stack
		/// </summary>
		/// <param name="item">A reference that receives the top item</param>
		/// <returns>The stack with the top item removed</returns>
		/// <exception cref="InvalidOperationException">Thrown if the stack is empty</exception>
		public ImmutableCountedStack<TItem> Pop(out TItem item)
		{
			if (IsEmpty)
				throw new InvalidOperationException("Stack is empty");
			
			item = _Head;
			
			return _Tail;
		}
		
		/// <summary>
		/// Pushes an item onto the top of the stack
		/// </summary>
		/// <param name="item">The item to push</param>
		/// <returns>The stack with the new top item</returns>
		public ImmutableCountedStack<TItem> Push(TItem item)
		{
			return new ImmutableCountedStack<TItem>(item, this, _Count + 1);
		}
		
		//****************************************
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new StackEnumerator(this);
		}
		
#if NET45
		System.Collections.Immutable.IImmutableStack<TItem> System.Collections.Immutable.IImmutableStack<TItem>.Clear()
		{
			return Empty;
		}

		System.Collections.Immutable.IImmutableStack<TItem> System.Collections.Immutable.IImmutableStack<TItem>.Pop()
		{
			if (IsEmpty)
				throw new InvalidOperationException("Stack is empty");
			
			return _Tail;
		}

		System.Collections.Immutable.IImmutableStack<TItem> System.Collections.Immutable.IImmutableStack<TItem>.Push(TItem item)
		{
			return new ImmutableCountedStack<TItem>(item, this, _Count + 1);
		}
#endif
		//****************************************
		
		/// <summary>
		/// Gets whether this stack is empty
		/// </summary>
		public bool IsEmpty
		{
			get { return _Tail == null; }
		}
		
		/// <summary>
		/// Gets the number of items on the stack
		/// </summary>
		public int Count
		{
			get { return _Count; }
		}
		
		//****************************************
		
		private class StackEnumerator : IEnumerator<TItem>
		{	//****************************************
			private readonly ImmutableCountedStack<TItem> _Start;
			private ImmutableCountedStack<TItem> _Current;
			//****************************************
			
			public StackEnumerator(ImmutableCountedStack<TItem> stack)
			{
				_Start = stack;
			}
			
			//****************************************
			
			public TItem Current
			{
				get
				{
					if (_Current == null || _Current.IsEmpty)
						throw new InvalidOperationException();
					
					return _Current._Head;
				}
			}
			
			object IEnumerator.Current
			{
				get
				{
					if (_Current == null || _Current.IsEmpty)
						throw new InvalidOperationException();
					
					return _Current._Head;
				}
			}
			
			public void Dispose()
			{
			}
			
			public bool MoveNext()
			{
				if (_Current == null)
					_Current = _Start;
				else if (!_Current.IsEmpty)
					_Current = _Current._Tail;
				
				return !_Current.IsEmpty;
			}
			
			public void Reset()
			{
				_Current = null;
			}
		}
	}
}
