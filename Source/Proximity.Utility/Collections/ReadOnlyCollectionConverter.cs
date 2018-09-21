/****************************************\
 ReadOnlyCollectionConvert.cs
 Created: 2015-02-05
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a read-only wrapper around a Collection that converts to another type
	/// </summary>
	public abstract class ReadOnlyCollectionConverter<TSource, TTarget> : ICollection<TTarget>
#if !NET40
		, IReadOnlyCollection<TTarget>
#endif
	{	//****************************************
#if NET40
		private readonly ICollection<TSource> _Collection;
#else
		private readonly IReadOnlyCollection<TSource> _Collection;
#endif
		//****************************************

		/// <summary>
		/// Creates a new read-only wrapper around a collection that converts to another type
		/// </summary>
		/// <param name="collection">The collection to wrap as read-only</param>
		public ReadOnlyCollectionConverter(ICollection<TSource> collection)
		{
#if NET40
			_Collection = collection;
#else
			_Collection = collection as IReadOnlyCollection<TSource> ?? new ReadOnlyCollection<TSource>(collection);
#endif
		}

#if !NET40
		/// <summary>
		/// Creates a new read-only wrapper around a read-only collection that converts to another type
		/// </summary>
		/// <param name="collection">The collection to wrap as read-only</param>
		public ReadOnlyCollectionConverter(IReadOnlyCollection<TSource> collection)
		{
			_Collection = collection;
		}
#endif

		//****************************************

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TTarget item)
		{	//****************************************
			var SourceValue = ConvertFrom(item);
			//****************************************

#if NET40
			return _Collection.Contains(SourceValue);
#else
			// If our source implements ICollection, use it and convert back
			if (_Collection is ICollection<TSource>)
				return ((ICollection<TSource>)_Collection).Contains(SourceValue);

			// No ICollection, so convert back and search for it the hard way
			return System.Linq.Enumerable.Contains(_Collection, SourceValue);
#endif
		}

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TTarget[] array, int arrayIndex)
		{
			foreach (var MyItem in _Collection)
			{
				array[arrayIndex++] = ConvertTo(MyItem);
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TTarget> GetEnumerator()
		{
			return new Enumerator(this);
		}

		//****************************************

		/// <summary>
		/// Converts the source into the desired target type
		/// </summary>
		/// <param name="value">The value to convert</param>
		/// <returns>The converted value</returns>
		protected abstract TTarget ConvertTo(TSource value);

		/// <summary>
		/// Converts the target type back to the source type
		/// </summary>
		/// <param name="value">The converted value</param>
		/// <returns>The original value</returns>
		protected virtual TSource ConvertFrom(TTarget value)
		{
			throw new NotSupportedException("Conversion is one-way");
		}
		
		//****************************************

		void ICollection<TTarget>.Add(TTarget item)
		{
			throw new NotSupportedException("Collection is read-only");
		}

		void ICollection<TTarget>.Clear()
		{
			throw new NotSupportedException("Collection is read-only");
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(this);
		}

		IEnumerator<TTarget> IEnumerable<TTarget>.GetEnumerator()
		{
			return new Enumerator(this);
		}

		bool ICollection<TTarget>.Remove(TTarget item)
		{
			throw new NotSupportedException("Collection is read-only");
		}

		//****************************************

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count
		{
			get { return _Collection.Count; }
		}

		/// <summary>
		/// Gets the source collection
		/// </summary>
#if NET40
		public ICollection<TSource> Parent
#else
		public IReadOnlyCollection<TSource> Parent
#endif
		{
			get { return _Collection; }
		}

		bool ICollection<TTarget>.IsReadOnly
		{
			get { return true; }
		}

		//****************************************

		/// <summary>
		/// Enumerates the collection while avoiding memory allocations
		/// </summary>
		public struct Enumerator : IEnumerator<TTarget>
		{	//****************************************
			private readonly ReadOnlyCollectionConverter<TSource, TTarget> _Parent;
			private readonly IEnumerator<TSource> _Source;

			private TTarget _Current;
			//****************************************

			internal Enumerator(ReadOnlyCollectionConverter<TSource, TTarget> parent)
			{
				_Parent = parent;
				_Source = parent._Collection.GetEnumerator();
				_Current = default(TTarget);
			}

			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
				_Source.Dispose();
				_Current = default(TTarget);
			}

			/// <summary>
			/// Tries to move to the next item
			/// </summary>
			/// <returns>True if there's another item to enumerate, otherwise False</returns>
			public bool MoveNext()
			{
				if (!_Source.MoveNext())
					return false;

				_Current = _Parent.ConvertTo(_Source.Current);

				return true;
			}

			void IEnumerator.Reset()
			{
				_Source.Dispose();
				_Current = default(TTarget);
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public TTarget Current
			{
				get { return _Current; }
			}

			object IEnumerator.Current
			{
				get { return _Current; }
			}
		}
	}
}