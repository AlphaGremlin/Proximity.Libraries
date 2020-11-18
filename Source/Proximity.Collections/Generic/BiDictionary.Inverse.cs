using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Collections.Generic
{
	public partial class BiDictionary<TLeft, TRight> : IReadOnlyBiDictionary<TLeft, TRight>
	{
		/// <summary>
		/// Gets an inverse wrapper around this Dictionary
		/// </summary>
		public InverseDictionary Inverse { get; }

		IReadOnlyBiDictionary<TRight, TLeft> IReadOnlyBiDictionary<TLeft, TRight>.Inverse => Inverse;

		//****************************************

		/// <summary>
		/// Provides access to the bi-directional dictionary with the left/right reversed
		/// </summary>
		public sealed class InverseDictionary : IDictionary<TRight, TLeft>, IReadOnlyBiDictionary<TRight, TLeft>, IList<KeyValuePair<TRight, TLeft>>, IReadOnlyList<KeyValuePair<TRight, TLeft>>, IList, IDictionary
		{
			internal InverseDictionary(BiDictionary<TLeft, TRight> dictionary)
			{
				Inverse = dictionary;
			}

			//****************************************

			/// <inheritdoc/>
			public void Add(TRight right, TLeft left) => Inverse.Add(left, right);

			/// <summary>
			/// Adds a new element to the Dictionary
			/// </summary>
			/// <param name="item">The element to add</param>
			public void Add(KeyValuePair<TRight, TLeft> item) => Inverse.Add(Swap(item));

			/// <summary>
			/// Clears all elements from the Dictionary
			/// </summary>
			public void Clear() => Inverse.Clear();

			/// <inheritdoc/>
			public bool Contains(KeyValuePair<TRight, TLeft> item) => Inverse.Contains(Swap(item));

			/// <summary>
			/// Copies the contents of the Dictionary to an array
			/// </summary>
			/// <param name="array">The destination array</param>
			/// <param name="arrayIndex">The offset to start copying to</param>
			public void CopyTo(KeyValuePair<TRight, TLeft>[] array, int arrayIndex)
			{
				var Size = Inverse._Size;
				var Entries = Inverse._Entries;

				if (arrayIndex + Size > array.Length)
					throw new ArgumentOutOfRangeException(nameof(arrayIndex));

				for (var Index = 0; Index < Size; Index++)
				{
					array[arrayIndex + Index] = Entries[Index].InverseItem;
				}
			}

			/// <inheritdoc/>
			public InverseEnumerator GetEnumerator() => new InverseEnumerator(Inverse);

			/// <summary>
			/// Determines the index of a specific item in the list
			/// </summary>
			/// <param name="item">The item to locate</param>
			/// <returns>The index of the item if found, otherwise -1</returns>
			public int IndexOf(KeyValuePair<TRight, TLeft> item) => Inverse.IndexOf(Swap(item));

			/// <summary>
			/// Removes an element from the dictionary
			/// </summary>
			/// <param name="right">The right of the element to remove</param>
			public bool Remove(TRight right)
			{
				if (right == null)
					throw new ArgumentNullException(nameof(right));

				var Index = Inverse.IndexOfRight(right);

				if (Index == -1)
					return false;

				Inverse.RemoveAt(Index);

				return true;
			}

			/// <summary>
			/// Removes a specific Left and Right pair from the dictionary
			/// </summary>
			/// <param name="item">The element to remove</param>
			public bool Remove(KeyValuePair<TRight, TLeft> item) => Inverse.Remove(Swap(item));

			/// <summary>
			/// Removes the element at the specified index
			/// </summary>
			/// <param name="index">The index of the element to remove</param>
			public void RemoveAt(int index) => Inverse.RemoveAt(index);

			/// <summary>
			/// Converts the contents of the Dictionary to an array
			/// </summary>
			/// <returns>The resulting array</returns>
			public KeyValuePair<TRight, TLeft>[] ToArray()
			{
				var Copy = new KeyValuePair<TRight, TLeft>[Count];

				CopyTo(Copy, 0);

				return Copy;
			}

			/// <summary>
			/// Tries to add an item to the Dictionary
			/// </summary>
			/// <param name="left">The key of the item</param>
			/// <param name="right">The value to associate with the key</param>
			/// <returns>True if the item was added, otherwise False</returns>
			public bool TryAdd(TRight right, TLeft left) => Inverse.TryAdd(new KeyValuePair<TLeft, TRight>(left, right), out _);

			/// <summary>
			/// Tries to add an item to the Dictionary
			/// </summary>
			/// <param name="left">The key of the item</param>
			/// <param name="right">The value to associate with the key</param>
			/// <param name="index">Receives the index of the new key/value pair, if added</param>
			/// <returns>True if the item was added, otherwise False</returns>
			public bool TryAdd(TRight right, TLeft left, out int index) => Inverse.TryAdd(new KeyValuePair<TLeft, TRight>(left, right), out index);

			/// <summary>
			/// Tries to add an item to the Dictionary
			/// </summary>
			/// <param name="item">The key/value pair to add</param>
			/// <returns>True if the item was added, otherwise False</returns>
			public bool TryAdd(KeyValuePair<TRight, TLeft> item) => Inverse.TryAdd(Swap(item), out _);

			/// <summary>
			/// Tries to add an item to the Dictionary
			/// </summary>
			/// <param name="item">The key/value pair to add</param>
			/// <param name="index">Receives the index of the new key/value pair, if added</param>
			/// <returns>True if the item was added, otherwise False</returns>
			public bool TryAdd(KeyValuePair<TRight, TLeft> item, out int index) => Inverse.TryAdd(Swap(item), out index);

			/// <summary>
			/// Tries to remove an item from the dictionary
			/// </summary>
			/// <param name="right">The Right of the item to remove</param>
			/// <param name="left">Receives the Left of the removed item</param>
			/// <returns>True if the item was removed, otherwise False</returns>
			public bool TryRemove(TRight right,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out TLeft left)
			{
				var Index = Inverse.IndexOfRight(right);

				if (Index == -1)
				{
					left = default!;
					return false;
				}

				left = Inverse._Entries[Index].Left;

				Inverse.RemoveAt(Index);

				return true;
			}

			//****************************************

			void ICollection<KeyValuePair<TRight, TLeft>>.Clear() => Inverse.Clear();

			bool IDictionary<TRight, TLeft>.ContainsKey(TRight key) => Inverse.ContainsRight(key);

			bool IReadOnlyDictionary<TRight, TLeft>.ContainsKey(TRight key) => Inverse.ContainsRight(key);

			IEnumerator<KeyValuePair<TRight, TLeft>> IEnumerable<KeyValuePair<TRight, TLeft>>.GetEnumerator() => new InverseEnumerator(Inverse);

			IEnumerator IEnumerable.GetEnumerator() => new InverseEnumerator(Inverse);

			bool IDictionary<TRight, TLeft>.TryGetValue(TRight key, out TLeft value) => Inverse.TryGetLeft(key, out value!);

			bool IReadOnlyDictionary<TRight, TLeft>.TryGetValue(TRight key, out TLeft value) => Inverse.TryGetLeft(key, out value!);

			void IList.Insert(int index, object value) => throw new NotSupportedException();

			void IList<KeyValuePair<TRight, TLeft>>.Insert(int index, KeyValuePair<TRight, TLeft> item) => throw new NotSupportedException();

			void ICollection.CopyTo(Array array, int index) => CopyTo((KeyValuePair<TRight, TLeft>[])array, index);

			void IDictionary.Add(object key, object value)
			{
				if (!(key is TRight Right))
					throw new ArgumentException("Not a supported key", nameof(key));

				if (!(value is TLeft Left))
					throw new ArgumentException("Not a supported value", nameof(value));

				Add(Right, Left);
			}

			bool IDictionary.Contains(object value)
			{
				if (value is KeyValuePair<TRight, TLeft> Pair)
					return Contains(Pair);

				if (value is DictionaryEntry Entry && Entry.Key is TRight Right && Entry.Value is TLeft Left)
					return Contains(new KeyValuePair<TRight, TLeft>(Right, Left));

				return false;
			}

			IDictionaryEnumerator IDictionary.GetEnumerator() => new InverseDictionaryEnumerator(Inverse);

			void IDictionary.Remove(object value)
			{
				if (value is KeyValuePair<TRight, TLeft> Pair)
					Remove(Pair);
				else if (value is DictionaryEntry Entry && Entry.Key is TRight Right && Entry.Value is TLeft Left)
					Remove(new KeyValuePair<TRight, TLeft>(Right, Left));
			}

			int IList.Add(object value)
			{
				if (value is KeyValuePair<TRight, TLeft> Pair)
				{
					if (TryAdd(Pair, out var Index))
						return Index;

					return -1;
				}

				if (value is DictionaryEntry Entry && Entry.Key is TRight Right && Entry.Value is TLeft Left)
				{
					if (TryAdd(new KeyValuePair<TRight, TLeft>(Right, Left), out var Index))
						return Index;

					return -1;
				}

				throw new ArgumentException("Not a supported value", nameof(value));
			}

			bool IList.Contains(object value)
			{
				if (value is KeyValuePair<TRight, TLeft> Pair)
					return Contains(Pair);

				if (value is DictionaryEntry Entry && Entry.Key is TRight Right && Entry.Value is TLeft Left)
					return Contains(new KeyValuePair<TRight, TLeft>(Right, Left));

				return false;
			}

			int IList.IndexOf(object value)
			{
				if (value is KeyValuePair<TRight, TLeft> Pair)
					return IndexOf(Pair);

				if (value is DictionaryEntry Entry && Entry.Key is TRight Right && Entry.Value is TLeft Left)
					return IndexOf(new KeyValuePair<TRight, TLeft>(Right, Left));

				return -1;
			}

			void IList.Remove(object value)
			{
				if (value is KeyValuePair<TRight, TLeft> Pair)
					Remove(Pair);
				else if (value is DictionaryEntry Entry && Entry.Key is TRight Right && Entry.Value is TLeft Left)
					Remove(new KeyValuePair<TRight, TLeft>(Right, Left));
			}

			//****************************************

			/// <summary>
			/// Gets the number of items in the collection
			/// </summary>
			public int Count => Inverse.Count;

			/// <summary>
			/// Gets/Sets the value corresponding to the provided key
			/// </summary>
			[System.Runtime.CompilerServices.IndexerName("Item")]
			public TLeft this[TRight key]
			{
				get
				{
					if (Inverse.TryGetLeft(key, out var ResultValue))
						return ResultValue;

					throw new KeyNotFoundException();
				}
				set
				{
					var Item = new KeyValuePair<TLeft, TRight>(value, key);

					if (!Inverse.TryAdd(Item))
						throw new ArgumentException("Left or Right already exist in the dictionary");
				}
			}

			/// <summary>
			/// Gets the original dictionary
			/// </summary>
			public BiDictionary<TLeft, TRight> Inverse { get; }

			IReadOnlyBiDictionary<TLeft, TRight> IReadOnlyBiDictionary<TRight, TLeft>.Inverse => Inverse;

			ICollection IDictionary.Keys => Inverse.Rights;

			ICollection IDictionary.Values => Inverse.Lefts;

			ICollection<TRight> IDictionary<TRight, TLeft>.Keys => Inverse.Rights;

			ICollection<TLeft> IDictionary<TRight, TLeft>.Values => Inverse.Lefts;

			IEnumerable<TRight> IReadOnlyDictionary<TRight, TLeft>.Keys => Inverse.Rights;

			IEnumerable<TLeft> IReadOnlyDictionary<TRight, TLeft>.Values => Inverse.Lefts;

			bool ICollection<KeyValuePair<TRight, TLeft>>.IsReadOnly => false;

			bool IDictionary.IsFixedSize => false;

			bool IDictionary.IsReadOnly => false;

			bool IList.IsFixedSize => false;

			bool IList.IsReadOnly => false;

			KeyValuePair<TRight, TLeft> IReadOnlyList<KeyValuePair<TRight, TLeft>>.this[int index] => Swap(Inverse.GetByIndex(index));

			KeyValuePair<TRight, TLeft> IList<KeyValuePair<TRight, TLeft>>.this[int index]
			{
				get => Swap(Inverse.GetByIndex(index));
				set => throw new NotSupportedException();
			}

			object? IDictionary.this[object key]
			{
				get
				{
					if (key is TRight Right)
						return this[Right];

					throw new KeyNotFoundException();
				}
				set
				{
					if (!(key is TRight Right))
						throw new ArgumentException("Not a supported key", nameof(key));

					if (!(value is TLeft Left))
						throw new ArgumentException("Not a supported value", nameof(value));

					if (!Inverse.TryAdd(Left, Right))
						throw new ArgumentException("Left or Right already exist in the dictionary");
				}
			}

			object IList.this[int index]
			{
				get => Swap(Inverse.GetByIndex(index));
				set => throw new NotSupportedException();
			}

			object ICollection.SyncRoot => this;

			bool ICollection.IsSynchronized => false;

			//****************************************

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static KeyValuePair<TRight, TLeft> Swap(KeyValuePair<TLeft, TRight> item) => new KeyValuePair<TRight, TLeft>(item.Value, item.Key);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static KeyValuePair<TLeft, TRight> Swap(KeyValuePair<TRight, TLeft> item) => new KeyValuePair<TLeft, TRight>(item.Value, item.Key);
		}

		/// <summary>
		/// Enumerates the dictionary with Second as the Key while avoiding memory allocations
		/// </summary>
		public struct InverseEnumerator : IEnumerator<KeyValuePair<TRight, TLeft>>, IEnumerator
		{ //****************************************
			private readonly BiDictionary<TLeft, TRight> _Parent;

			private int _Index;
			//****************************************

			internal InverseEnumerator(BiDictionary<TLeft, TRight> parent)
			{
				_Parent = parent;
				_Index = 0;
				Current = default;
			}

			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
				Current = default;
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
					Current = default;

					return false;
				}

				Current = _Parent._Entries[_Index++].InverseItem;

				return true;
			}

			/// <inheritdoc />
			public void Reset()
			{
				_Index = 0;
				Current = default;
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public KeyValuePair<TRight, TLeft> Current { get; private set; }

			object IEnumerator.Current => Current;
		}

		private sealed class InverseDictionaryEnumerator : IDictionaryEnumerator
		{ //****************************************
			private InverseEnumerator _Parent;
			//****************************************

			internal InverseDictionaryEnumerator(BiDictionary<TLeft, TRight> parent)
			{
				_Parent = new InverseEnumerator(parent);
			}

			//****************************************

			public bool MoveNext() => _Parent.MoveNext();

			public void Reset() => _Parent.Reset();

			//****************************************

			public DictionaryEntry Current
			{
				get
				{
					var Current = _Parent.Current;

					return new DictionaryEntry(Current.Key, Current.Value);
				}
			}

			object IEnumerator.Current => Current;

			DictionaryEntry IDictionaryEnumerator.Entry => Current;

			object? IDictionaryEnumerator.Key => _Parent.Current.Key;

			object? IDictionaryEnumerator.Value => _Parent.Current.Value;
		}
	}
}
