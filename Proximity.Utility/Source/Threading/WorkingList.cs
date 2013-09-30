/****************************************\
 WorkingList.cs
 Created: 2011-08-02
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Represents a list of working values
	/// </summary>
	/// <remarks>
	/// <para>The list cannot be added to, however items can be replaced or set to null</para>
	/// <para>Changes to the size of the list (Update methods, or EnsureCapacity) will wipe its contents</para>
	/// </remarks>
	public class WorkingList<TValue> : IList<TValue>
	{	//****************************************
		private int _WorkingLength;
		private TValue[] _WorkingArray;
		//****************************************
		
		/// <summary>
		/// Creates a new, empty Working List
		/// </summary>
		public WorkingList()
		{
			_WorkingArray = new TValue[0];
		}
		
		//****************************************
		
		/// <summary>
		/// Updates the Working List with a new set of values
		/// </summary>
		/// <param name="source">The collection containing the values we want</param>
		public void Update(ICollection<TValue> source)
		{
			_WorkingLength = source.Count;
			
			if (_WorkingArray.Length < _WorkingLength)
				_WorkingArray = new TValue[_WorkingLength];
			
			source.CopyTo(_WorkingArray, 0);
		}
		
		/// <summary>
		/// Updates the Working List with a new set of values, ensuring any previous values are cleared
		/// </summary>
		/// <param name="source">The collection containing the values we want</param>
		public void UpdateAndClear(ICollection<TValue> source)
		{
			_WorkingLength = source.Count;
			
			if (_WorkingArray.Length < _WorkingLength)
				_WorkingArray = new TValue[_WorkingLength];
			
			source.CopyTo(_WorkingArray, 0);
			
			Array.Clear(_WorkingArray, _WorkingLength, _WorkingArray.Length - _WorkingLength);
		}
		
		/// <summary>
		/// Ensures the Working List has a given number of values available
		/// </summary>
		/// <param name="capacity">The number of values we want to have accessible</param>
		public void EnsureCapacity(int capacity)
		{
			if (_WorkingArray.Length < capacity)
				_WorkingArray = new TValue[capacity];
			
			_WorkingLength = capacity;
		}
		
		/// <summary>
		/// Clears the Working List of any previous values
		/// </summary>
		public void Clear()
		{
			Array.Clear(_WorkingArray, 0, _WorkingLength);
		}
		
		//****************************************
		
		int IList<TValue>.IndexOf(TValue item)
		{
			return Array.IndexOf<TValue>(_WorkingArray, item);
		}
		
		void IList<TValue>.Insert(int index, TValue item)
		{
			throw new NotSupportedException();
		}
		
		void IList<TValue>.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}
		
		void ICollection<TValue>.Add(TValue item)
		{
			throw new NotSupportedException();
		}
		
		bool ICollection<TValue>.Contains(TValue item)
		{
			return Array.IndexOf<TValue>(_WorkingArray, item) != -1;
		}
		
		void ICollection<TValue>.CopyTo(TValue[] array, int arrayIndex)
		{
			Array.Copy(_WorkingArray, 0, array, arrayIndex, _WorkingLength - arrayIndex);
		}
		
		bool ICollection<TValue>.Remove(TValue item)
		{
			throw new NotSupportedException();
		}
		
		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
		{
			return new WorkingEnumerator<TValue>(_WorkingArray, _WorkingLength);
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new WorkingEnumerator<TValue>(_WorkingArray, _WorkingLength);
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the current number of values in the Working List
		/// </summary>
		/// <remarks>This does not represent the total working capacity</remarks>
		public int Count
		{
			get { return _WorkingLength; }
		}
		
		/// <summary>
		/// Gets/Sets a value within the Working List
		/// </summary>
		public TValue this[int index]
		{
			get { return _WorkingArray[index]; }
			set { _WorkingArray[index] = value; }
		}
		
		bool ICollection<TValue>.IsReadOnly
		{
			get { return false; }
		}
	}
}
