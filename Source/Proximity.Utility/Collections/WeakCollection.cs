/****************************************\
 WeakCollection.cs
 Created: 2013-08-16
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a collection that holds only weak references to its contents
	/// </summary>
	public sealed class WeakCollection<TItem> : IEnumerable<TItem> where TItem : class
	{	//****************************************
		private readonly List<GCHandle> _Values;
		//****************************************
		
		/// <summary>
		/// Creates a new WeakCollection
		/// </summary>
		public WeakCollection()
		{
			_Values = new List<GCHandle>();
		}
		
		/// <summary>
		/// Creates a new WeakCollection of references to the contents of the collection
		/// </summary>
		/// <param name="collection">The collection holding the items to weakly reference</param>
		public WeakCollection(IEnumerable<TItem> collection)
		{
			_Values = new List<GCHandle>(collection.Where(item => item != null).Select(CreateFrom));
		}
		
		//****************************************
		
		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		/// <exception cref="ArgumentNullException">Item was null</exception>
		public void Add(TItem item)
		{
			if (item == null)
				throw new ArgumentNullException("Cannot add null to a Weak Collection");
			
			_Values.Add(CreateFrom(item));
		}
		
		/// <summary>
		/// Adds a range of elements to the collection
		/// </summary>
		/// <param name="collection">The elements to add</param>
		/// <remarks>Ignores any null items, rather than throwing an exception</remarks>
		public void AddRange(IEnumerable<TItem> collection)
		{
			_Values.AddRange(collection.Where(item => item != null).Select(CreateFrom));
		}
		
		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		/// <returns>True if the item was removed, false if it was not in the collection</returns>
		/// <remarks>Will perform a partial compaction, up to the point the target item is found</remarks>
		[SecuritySafeCritical]
		public bool Remove(TItem item)
		{
			int Index = 0;
			
			while (Index < _Values.Count)
			{
				var Handle = _Values[Index];
				var TargetItem = (TItem)Handle.Target;
				
				if (TargetItem == null)
				{
					_Values.RemoveAt(Index);
					
					Handle.Free();
				}
				else if (TargetItem == item)
				{
					_Values.RemoveAt(Index);
					
					Handle.Free();
					
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
		/// Removes all elements from the collection
		/// </summary>
		[SecuritySafeCritical]
		public void Clear()
		{
			foreach(var MyHandle in _Values)
			{
				MyHandle.Free();
			}
			
			_Values.Clear();
		}
		
		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		/// <remarks>Will perform a compaction. May not be identical between enumerations</remarks>
		public IEnumerator<TItem> GetEnumerator()
		{
			return GetContents().GetEnumerator();
		}
		
		/// <summary>
		/// Creates a list of strong references to the contents of the collection
		/// </summary>
		/// <returns>A list of strong references to the collection</returns>
		/// <remarks>Will perform a compaction.</remarks>
		public IList<TItem> ToStrongList()
		{
			return GetContents().ToList();
		}
		
		//****************************************
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetContents().GetEnumerator();
		}

		[SecuritySafeCritical]
		private GCHandle CreateFrom(TItem item)
		{
			return GCHandle.Alloc(item, GCHandleType.Weak);
		}

		[SecuritySafeCritical]
		private TItem ValueAt(int index)
		{
			var Handle = _Values[index];
			var TargetItem = (TItem)Handle.Target;

			if (TargetItem == null)
			{
				_Values.RemoveAt(index);

				Handle.Free();
			}

			return TargetItem;
		}
		
		private IEnumerable<TItem> GetContents()
		{
			int Index = 0;
			
			while (Index < _Values.Count)
			{
				// Iterators are SecurityTransparent, so we have to use an accessor method
				var TargetItem = ValueAt(Index);

				if (TargetItem != null)
				{
					yield return TargetItem;
					
					Index++;
				}
			}
		}
	}
}
