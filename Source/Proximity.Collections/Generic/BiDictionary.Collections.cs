using System;
using System.Collections.Generic;
using System.Text;

namespace System.Collections.Generic
{
	public partial class BiDictionary<TLeft, TRight>
	{
		/// <summary>
		/// Gets a read-only collection of the dictionary Left values
		/// </summary>
		public LeftCollection Lefts { get; }

		/// <summary>
		/// Gets a read-only collection of the dictionary Right values
		/// </summary>
		public RightCollection Rights { get; }

		//****************************************

		/// <summary>
		/// Represents the common implementation for the bi-directional Dictionary collections
		/// </summary>
		public abstract class Collection<T> : IList<T>, IReadOnlyList<T>, IList
		{
			internal Collection(BiDictionary<TLeft, TRight> dictionary) => Dictionary = dictionary;

			//****************************************

			/// <inheritdoc/>
			public abstract bool Contains(T item);

			/// <inheritdoc/>
			public abstract void CopyTo(T[] array, int arrayIndex);

			/// <inheritdoc />
			public abstract int IndexOf(T item);

			/// <inheritdoc/>
			public IEnumerator<T> GetEnumerator() => InternalGetEnumerator();

			//****************************************

			IEnumerator IEnumerable.GetEnumerator() => InternalGetEnumerator();

			void ICollection.CopyTo(Array array, int index)
			{
				if (array is T[] MyArray)
				{
					CopyTo(MyArray, index);

					return;
				}

				throw new ArgumentException("Cannot copy to target array");
			}

			bool IList.Contains(object value)
			{
				if (value is T Item)
					return Contains(Item);

				return false;
			}

			int IList.IndexOf(object value)
			{
				if (value is T Item)
					return IndexOf(Item);

				return -1;
			}

			private protected abstract IEnumerator<T> InternalGetEnumerator();

			void ICollection<T>.Add(T item) => throw new NotSupportedException();

			int IList.Add(object item) => throw new NotSupportedException();

			void ICollection<T>.Clear() => throw new NotSupportedException();

			void IList.Clear() => throw new NotSupportedException();

			void IList.Insert(int index, object value) => throw new NotSupportedException();

			void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

			bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

			void IList.Remove(object item) => throw new NotSupportedException();

			void IList.RemoveAt(int index) => throw new NotSupportedException();

			void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

			//****************************************

			/// <inheritdoc />
			public abstract T this[int index] { get; }

			/// <inheritdoc/>
			public int Count => Dictionary.Count;

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

			object ICollection.SyncRoot => Dictionary;

			bool ICollection.IsSynchronized => false;

			internal BiDictionary<TLeft, TRight> Dictionary { get; }
		}

		/// <summary>
		/// Provides a read-only left wrapper
		/// </summary>
		public sealed class LeftCollection : Collection<TLeft>
		{
			internal LeftCollection(BiDictionary<TLeft, TRight> dictionary) : base(dictionary)
			{
			}

			//****************************************

			/// <inheritdoc/>
			public override bool Contains(TLeft item) => Dictionary.ContainsLeft(item);

			/// <inheritdoc/>
			public override void CopyTo(TLeft[] array, int arrayIndex)
			{ //****************************************
				var MyValues = Dictionary._Entries;
				var MySize = Dictionary._Size;
				//****************************************

				for (var Index = 0; Index < MySize; Index++)
					array[arrayIndex++] = MyValues[Index].Left;
			}

			/// <inheritdoc/>
			public new LeftEnumerator GetEnumerator() => new LeftEnumerator(Dictionary);

			/// <inheritdoc/>
			public override int IndexOf(TLeft item) => Dictionary.IndexOfLeft(item);

			/// <inheritdoc />
			public override TLeft this[int index] => Dictionary.GetByIndex(index).Key;

			//****************************************

			private protected override IEnumerator<TLeft> InternalGetEnumerator() => GetEnumerator();
		}

		/// <summary>
		/// Provides a read-only right wrapper
		/// </summary>
		public sealed class RightCollection : Collection<TRight>
		{
			internal RightCollection(BiDictionary<TLeft, TRight> dictionary) : base(dictionary)
			{
			}

			//****************************************

			/// <inheritdoc/>
			public override bool Contains(TRight item) => Dictionary.ContainsRight(item);

			/// <inheritdoc/>
			public override void CopyTo(TRight[] array, int arrayIndex)
			{ //****************************************
				var MyValues = Dictionary._Entries;
				var MySize = Dictionary._Size;
				//****************************************

				for (var Index = 0; Index < MySize; Index++)
					array[arrayIndex++] = MyValues[Index].Right;
			}

			/// <inheritdoc/>
			public new RightEnumerator GetEnumerator() => new RightEnumerator(Dictionary);

			/// <inheritdoc/>
			public override int IndexOf(TRight item) => Dictionary.IndexOfRight(item);

			/// <inheritdoc />
			public override TRight this[int index] => Dictionary.GetByIndex(index).Value;

			//****************************************

			private protected override IEnumerator<TRight> InternalGetEnumerator() => GetEnumerator();
		}

		/// <summary>
		/// Enumerates the dictionary Lefts while avoiding memory allocations
		/// </summary>
		public struct LeftEnumerator : IEnumerator<TLeft>, IEnumerator
		{ //****************************************
			private readonly BiDictionary<TLeft, TRight> _Parent;

			private int _Index;
			//****************************************

			internal LeftEnumerator(BiDictionary<TLeft, TRight> parent)
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

				Current = _Parent._Entries[_Index++].Left;

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
			public TLeft Current { get; private set; }

			object IEnumerator.Current => Current!;
		}

		/// <summary>
		/// Enumerates the dictionary Rights while avoiding memory allocations
		/// </summary>
		public struct RightEnumerator : IEnumerator<TRight>, IEnumerator
		{ //****************************************
			private readonly BiDictionary<TLeft, TRight> _Parent;

			private int _Index;
			//****************************************

			internal RightEnumerator(BiDictionary<TLeft, TRight> parent)
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

				Current = _Parent._Entries[_Index++].Right;

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
			public TRight Current { get; private set; }

			object IEnumerator.Current => Current!;
		}
	}
}
