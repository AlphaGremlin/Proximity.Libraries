using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
//****************************************

namespace System.Collections.ReadOnly
{
	/// <summary>
	/// Represents a read-only wrapper around a Collection that converts to another type
	/// </summary>
	/// <remarks>If <typeparamref name="TInput"/> is derived from <typeparamref name="TOutput"/>, consider <see cref="ReadOnlyCollectionDowncast{TDerived, TBase}"/></remarks>
	public class ReadOnlyCollectionConverter<TInput, TOutput> : ICollection<TOutput>, IReadOnlyCollection<TOutput>
	{ //****************************************
		private readonly Func<TInput, TOutput> _Conversion;
		private readonly Func<TOutput, TInput>? _ReverseConversion;
		//****************************************

		/// <summary>
		/// Creates a new read-only wrapper around a collection that converts to another type
		/// </summary>
		/// <param name="collection">The collection to wrap as read-only</param>
		/// <param name="conversion">A function that converts the input to the output</param>
		/// <param name="reverseConversion">An optional function to convert the output to the input, if possible</param>
		public ReadOnlyCollectionConverter(ICollection<TInput> collection, Func<TInput, TOutput> conversion, Func<TOutput, TInput>? reverseConversion = null)
		{
			Parent = collection as IReadOnlyCollection<TInput> ?? new ReadOnlyCollection<TInput>(collection);
			_Conversion = conversion;
			_ReverseConversion = reverseConversion;
		}

		/// <summary>
		/// Creates a new read-only wrapper around a read-only collection that converts to another type
		/// </summary>
		/// <param name="collection">The collection to wrap as read-only</param>
		/// <param name="conversion">A function that converts the input to the output</param>
		/// <param name="reverseConversion">An optional function to convert the output to the input, if possible</param>
		public ReadOnlyCollectionConverter(IReadOnlyCollection<TInput> collection, Func<TInput, TOutput> conversion, Func<TOutput, TInput>? reverseConversion = null)
		{
			Parent = collection;
			_Conversion = conversion;
			_ReverseConversion = reverseConversion;
		}

		//****************************************

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TOutput item)
		{
			// If there's no reversal operation, do it the slow way
			if (_ReverseConversion == null)
				return Parent.Select(_Conversion).Contains(item);

			var SourceValue = _ReverseConversion(item);

			// If our source implements ICollection, use it and convert back
			if (Parent is ICollection<TInput> SourceCollection)
				return SourceCollection.Contains(SourceValue);

			// No ICollection, so convert back and search for it the hard way
			return Enumerable.Contains(Parent, SourceValue);
		}

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TOutput[] array, int arrayIndex)
		{
			foreach (var MyItem in Parent)
			{
				array[arrayIndex++] = _Conversion(MyItem);
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TOutput> GetEnumerator() => new Enumerator(this);

		//****************************************

		void ICollection<TOutput>.Add(TOutput item) => throw new NotSupportedException("Collection is read-only");

		void ICollection<TOutput>.Clear() => throw new NotSupportedException("Collection is read-only");

		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

		IEnumerator<TOutput> IEnumerable<TOutput>.GetEnumerator() => new Enumerator(this);

		bool ICollection<TOutput>.Remove(TOutput item) => throw new NotSupportedException("Collection is read-only");

		//****************************************

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count => Parent.Count;

		/// <summary>
		/// Gets the source collection
		/// </summary>
		public IReadOnlyCollection<TInput> Parent { get; }

		bool ICollection<TOutput>.IsReadOnly => true;

		//****************************************

		/// <summary>
		/// Enumerates the collection while avoiding memory allocations
		/// </summary>
		public struct Enumerator : IEnumerator<TOutput>
		{	//****************************************
			private readonly Func<TInput, TOutput> _Conversion;
			private readonly IEnumerator<TInput> _Source;
			//****************************************

			internal Enumerator(ReadOnlyCollectionConverter<TInput, TOutput> parent)
			{
				_Conversion = parent._Conversion;
				_Source = parent.Parent.GetEnumerator();
				Current = default!;
			}

			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
				_Source.Dispose();
				Current = default!;
			}

			/// <summary>
			/// Tries to move to the next item
			/// </summary>
			/// <returns>True if there's another item to enumerate, otherwise False</returns>
			public bool MoveNext()
			{
				if (!_Source.MoveNext())
					return false;

				Current = _Conversion(_Source.Current);

				return true;
			}

			void IEnumerator.Reset()
			{
				_Source.Dispose();
				Current = default!;
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public TOutput Current { get; private set; }

			object? IEnumerator.Current => Current;
		}
	}
}
