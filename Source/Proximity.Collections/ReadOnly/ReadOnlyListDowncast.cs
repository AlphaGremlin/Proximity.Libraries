using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//****************************************

namespace System.Collections.ReadOnly
{
	/// <summary>
	/// Represents a read-only wrapper around a list that also converts to a base type
	/// </summary>
	public class ReadOnlyListDowncast<TDerived, TBase> : IList<TBase>, IReadOnlyList<TBase> where TDerived : class, TBase where TBase : class
	{	//****************************************
		private readonly IList<TDerived> _Source;
		//****************************************
		
		/// <summary>
		/// Creates a new read-only wrapper around a list
		/// </summary>
		/// <param name="source">The list to wrap</param>
		public ReadOnlyListDowncast(IList<TDerived> source)
		{
			_Source = source;
		}
		
		//****************************************
		
		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public int IndexOf(TBase item)
		{
			if (item is TDerived Item)
				return _Source.IndexOf(Item);

			return -1;
		}
		
		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TBase item)
		{
			if (item is TDerived Item)
				return _Source.Contains(Item);

			return false;
		}
		
		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TBase[] array, int arrayIndex)
		{
			// Array Contravariance
			_Source.CopyTo((TDerived[])array, arrayIndex);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TBase> GetEnumerator() => _Source.GetEnumerator();

		//****************************************

		void IList<TBase>.Insert(int index, TBase item) => throw new NotSupportedException("List is read-only");

		void IList<TBase>.RemoveAt(int index) => throw new NotSupportedException("List is read-only");

		void ICollection<TBase>.Add(TBase item) => throw new NotSupportedException("List is read-only");

		void ICollection<TBase>.Clear() => throw new NotSupportedException("List is read-only");

		IEnumerator<TBase> IEnumerable<TBase>.GetEnumerator() => _Source.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _Source.GetEnumerator();

		bool ICollection<TBase>.Remove(TBase item) => throw new NotSupportedException("List is read-only");

		//****************************************

		/// <summary>
		/// Gets the item at the requested index
		/// </summary>
		public TBase this[int index] => _Source[index];

		/// <summary>
		/// Gets the number of items in the list
		/// </summary>
		public int Count => _Source.Count;

		TBase IList<TBase>.this[int index]
		{
			get => _Source[index];
			set => throw new NotSupportedException("List is read-only");
		}

		bool ICollection<TBase>.IsReadOnly => true;
	}
}
