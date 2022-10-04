using System.Text;

namespace System.Collections.Generic
{
	public sealed partial class OrderedDictionary<TKey, TValue>
	{
		/// <summary>
		/// Provides a base class for the Indexed Dictionary collections
		/// </summary>
		/// <typeparam name="T">The type of item</typeparam>
		public abstract class Collection<T> : IList<T>, IReadOnlyList<T>, IList
		{
			internal Collection(OrderedDictionary<TKey, TValue> parent)
			{
				Parent = parent;
			}

			//****************************************

			/// <inheritdoc />
			public abstract bool Contains(T item);

			/// <inheritdoc />
			public abstract void CopyTo(T[] array, int arrayIndex);

			/// <inheritdoc />
			public IEnumerator<T> GetEnumerator() => InternalGetEnumerator();

			/// <inheritdoc />
			public abstract int IndexOf(T item);

			private protected abstract IEnumerator<T> InternalGetEnumerator();

			void ICollection.CopyTo(Array array, int index)
			{
				if (array is T[] MyArray)
				{
					CopyTo(MyArray, index);

					return;
				}

				throw new ArgumentException("Cannot copy to target array");
			}

			bool IList.Contains(object? value)
			{
				if (value is T Item)
					return Contains(Item);

				return false;
			}

			int IList.IndexOf(object? value)
			{
				if (value is T Item)
					return IndexOf(Item);

				return -1;
			}

			IEnumerator IEnumerable.GetEnumerator() => InternalGetEnumerator();

			void ICollection<T>.Add(T item) => throw new NotSupportedException();

			int IList.Add(object? item) => throw new NotSupportedException();

			void ICollection<T>.Clear() => throw new NotSupportedException();

			void IList.Clear() => throw new NotSupportedException();

			void IList.Insert(int index, object? value) => throw new NotSupportedException();

			void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

			bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

			void IList.Remove(object? item) => throw new NotSupportedException();

			void IList.RemoveAt(int index) => throw new NotSupportedException();

			void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

			//****************************************

			/// <inheritdoc />
			public abstract T this[int index] { get; }

			/// <inheritdoc />
			public int Count => Parent.Count;

			bool ICollection<T>.IsReadOnly => true;

			bool IList.IsFixedSize => false;

			bool IList.IsReadOnly => true;

			T IList<T>.this[int index]
			{
				get => this[index];
				set => throw new NotSupportedException();
			}

			object? IList.this[int index]
			{
				get => this[index];
				set => throw new NotSupportedException();
			}

			object ICollection.SyncRoot => Parent;

			bool ICollection.IsSynchronized => false;

			/// <summary>
			/// Gets the parent Dictionary
			/// </summary>
			protected OrderedDictionary<TKey, TValue> Parent { get; }
		}

		/// <summary>
		/// Provides a read-only keys wrapper
		/// </summary>
		public sealed class KeyCollection : Collection<TKey>
		{
			internal KeyCollection(OrderedDictionary<TKey, TValue> parent) : base(parent)
			{
			}

			//****************************************

			/// <inheritdoc />
			public override bool Contains(TKey item) => Parent.ContainsKey(item);

			/// <inheritdoc />
			public override void CopyTo(TKey[] array, int arrayIndex)
			{
				var Count = Parent._Size;

				if (arrayIndex + Count > array.Length)
					throw new ArgumentOutOfRangeException(nameof(arrayIndex));

				var Entries = Parent._Entries;

				for (var Index = 0; Index < Count; Index++)
				{
					array[arrayIndex + Index] = Entries[Index].Key;
				}
			}

			/// <inheritdoc />
			public new KeyEnumerator GetEnumerator() => new(Parent);

			/// <inheritdoc />
			public override int IndexOf(TKey item) => Parent.EntryIndexOfKey(item);

			/// <inheritdoc />
			public override TKey this[int index] => Parent.GetRef(index).Key;

			//****************************************

			private protected override IEnumerator<TKey> InternalGetEnumerator() => GetEnumerator();
		}

		/// <summary>
		/// Provides a read-only values wrapper
		/// </summary>
		public sealed class ValueCollection : Collection<TValue>
		{
			internal ValueCollection(OrderedDictionary<TKey, TValue> parent) : base(parent)
			{
			}

			//****************************************

			/// <inheritdoc />
			public override bool Contains(TValue item) => Parent.ContainsValue(item);

			/// <inheritdoc />
			public override void CopyTo(TValue[] array, int arrayIndex)
			{
				var Count = Parent._Size;

				if (arrayIndex + Count > array.Length)
					throw new ArgumentOutOfRangeException(nameof(arrayIndex));

				var Entries = Parent._Entries;

				for (var Index = 0; Index < Count; Index++)
				{
					array[arrayIndex + Index] = Entries[Index].Value;
				}
			}

			/// <inheritdoc />
			public new ValueEnumerator GetEnumerator() => new(Parent);

			/// <inheritdoc />
			public override int IndexOf(TValue item) => Parent.IndexOfValue(item);

			/// <inheritdoc />
			public override TValue this[int index] => Parent.GetRef(index).Value;

			//****************************************

			private protected override IEnumerator<TValue> InternalGetEnumerator() => GetEnumerator();
		}

		/// <summary>
		/// Enumerates the dictionary keys while avoiding memory allocations
		/// </summary>
		public struct KeyEnumerator : IEnumerator<TKey>, IEnumerator
		{ //****************************************
			private readonly OrderedDictionary<TKey, TValue> _Parent;

			private int _Index;
			//****************************************

			internal KeyEnumerator(OrderedDictionary<TKey, TValue> parent)
			{
				_Parent = parent;
				_Index = 0;
				Current = default!;
			}

			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
				Current = default!;
			}

			/// <summary>
			/// Tries to move to the next item
			/// </summary>
			/// <returns>True if there's another item to enumerate, otherwise False</returns>
			public bool MoveNext()
			{
				if (_Index >= _Parent._Size)
				{
					_Index = _Parent._Size + 1;
					Current = default!;

					return false;
				}

				Current = _Parent.GetRef(_Index++).Key;

				return true;
			}

			void IEnumerator.Reset()
			{
				_Index = 0;
				Current = default!;
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public TKey Current { get; private set; }

			object IEnumerator.Current => Current!;
		}

		/// <summary>
		/// Enumerates the dictionary values while avoiding memory allocations
		/// </summary>
		public struct ValueEnumerator : IEnumerator<TValue>, IEnumerator
		{ //****************************************
			private readonly OrderedDictionary<TKey, TValue> _Parent;

			private int _Index;
			//****************************************

			internal ValueEnumerator(OrderedDictionary<TKey, TValue> parent)
			{
				_Parent = parent;
				_Index = 0;
				Current = default!;
			}

			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
				Current = default!;
			}

			/// <summary>
			/// Tries to move to the next item
			/// </summary>
			/// <returns>True if there's another item to enumerate, otherwise False</returns>
			public bool MoveNext()
			{
				if (_Index >= _Parent._Size)
				{
					_Index = _Parent._Size + 1;
					Current = default!;

					return false;
				}

				Current = _Parent.GetRef(_Index++).Value;

				return true;
			}

			void IEnumerator.Reset()
			{
				_Index = 0;
				Current = default!;
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public TValue Current { get; private set; }

			object IEnumerator.Current => Current!;
		}
	}
}
