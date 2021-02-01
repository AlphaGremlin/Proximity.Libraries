using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using Proximity.Collections;
//****************************************

namespace System.Collections.Generic
{
	/// <summary>
	/// Represents a collection that holds only weak references to its contents
	/// </summary>
	public sealed class WeakCollection<T> : IEnumerable<T>, IDisposable where T : class
	{	//****************************************
		private readonly List<GCReference> _Values;
		private readonly GCHandleType _HandleType;
		//****************************************
		
		/// <summary>
		/// Creates a new WeakCollection
		/// </summary>
		public WeakCollection() : this(GCHandleType.Weak)
		{
		}

		/// <summary>
		/// Creates a new WeakCollection
		/// </summary>
		/// <param name="handleType">The type of GCHandle to use</param>
		public WeakCollection(GCHandleType handleType)
		{
			_HandleType = handleType;
			_Values = new List<GCReference>();
		}
		
		/// <summary>
		/// Creates a new WeakCollection of references to the contents of the collection
		/// </summary>
		/// <param name="collection">The collection holding the items to weakly reference</param>
		public WeakCollection(IEnumerable<T> collection) : this(collection, GCHandleType.Weak)
		{
		}

		/// <summary>
		/// Creates a new WeakCollection of references to the contents of the collection
		/// </summary>
		/// <param name="collection">The collection holding the items to reference</param>
		/// <param name="handleType">The type of GCHandle to use</param>
		public WeakCollection(IEnumerable<T> collection, GCHandleType handleType)
		{
			if (collection is null)
				throw new ArgumentNullException(nameof(collection), "Collection cannot be null");

			_HandleType = handleType;

			_Values = new List<GCReference>();

			foreach (var MyItem in collection)
			{
				if (MyItem != null)
					_Values.Add(new GCReference(MyItem, handleType));
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		/// <exception cref="ArgumentNullException">Item was null</exception>
		public void Add(T item)
		{
			if (item is null)
				throw new ArgumentNullException(nameof(item), "Cannot add null to a Weak Collection");
			
			_Values.Add(new GCReference(item, _HandleType));
		}
		
		/// <summary>
		/// Adds a range of elements to the collection
		/// </summary>
		/// <param name="collection">The elements to add</param>
		/// <remarks>Ignores any null items, rather than throwing an exception</remarks>
		public void AddRange(IEnumerable<T> collection)
		{
			foreach (var MyItem in collection)
			{
				if (MyItem != null)
					_Values.Add(new GCReference(MyItem, _HandleType));
			}
		}

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the collection, otherwise false</returns>
		public bool Contains(T item)
		{
			if (item is null)
				return false;

			for (var Index = 0; Index < _Values.Count; Index++)
			{
				if ((T?)_Values[Index].Target == item)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public void Clear()
		{
			foreach (var MyHandle in _Values)
			{
				MyHandle.Dispose();
			}

			_Values.Clear();
		}
		
		/// <summary>
		/// Disposes of the Weak Dictionary, cleaning up any weak references
		/// </summary>
		public void Dispose()
		{
			foreach (var MyItem in _Values)
				MyItem.Dispose();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		/// <remarks>Will perform a compaction. May not be identical between enumerations</remarks>
		public Enumerator GetEnumerator() => new Enumerator(_Values);

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		/// <returns>True if the item was removed, false if it was not in the collection</returns>
		/// <remarks>Will perform a partial compaction, up to the point the target item is found</remarks>
		public bool Remove(T item)
		{
			if (item is null)
				return false;

			var Index = 0;

			while (Index < _Values.Count)
			{
				var Handle = _Values[Index];
				var TargetItem = (T?)Handle.Target;

				if (TargetItem == null)
				{
					_Values.RemoveAt(Index);

					Handle.Dispose();
				}
				else if (TargetItem == item)
				{
					_Values.RemoveAt(Index);

					Handle.Dispose();

					return true;
				}
				else
				{
					Index++;
				}
			}

			return false;
		}
		
		/// <summary>
		/// Creates a list of strong references to the contents of the collection
		/// </summary>
		/// <returns>A list of strong references to the collection</returns>
		/// <remarks>Will perform a compaction.</remarks>
		public ICollection<T> ToStrong()
		{	//****************************************
			var MyList = new List<T>(_Values.Count);
			//****************************************

			foreach (var MyItem in this)
			{
				MyList.Add(MyItem);
			}

			return MyList;
		}

		//****************************************

		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_Values);

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(_Values);

		//****************************************

		/// <summary>
		/// Enumerates the dictionary while avoiding memory allocations
		/// </summary>
		public struct Enumerator : IEnumerator<T>
		{	//****************************************
			private readonly List<GCReference> _List;

			private int _Index;
			//****************************************

			internal Enumerator(List<GCReference> list)
			{
				_List = list;
				_Index = 0;
				Current = null!;
			}

			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
				Current = null!;
			}

			/// <summary>
			/// Tries to move to the next item
			/// </summary>
			/// <returns>True if there's another item to enumerate, otherwise False</returns>
			public bool MoveNext()
			{
				for (; ; )
				{
					if (_Index >= _List.Count)
					{
						Current = null!;

						return false;
					}

					var Handle = _List[_Index];

					try
					{
						var Value = (T?)Handle.Target;

						if (Value != null)
						{
							Current = Value;

							_Index++;

							return true;
						}
					}
					catch (InvalidOperationException)
					{
						// The GCHandle was disposed
					}

					_List.RemoveAt(_Index);

					Handle.Dispose();
				}
			}

			void IEnumerator.Reset()
			{
				_Index = 0;
				Current = null!;
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public T Current { get; private set; }

			object IEnumerator.Current => Current;
		}
	}
}
