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
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a collection that holds only weak references to its contents
	/// </summary>
	public sealed class WeakCollection<TItem> : IEnumerable<TItem> where TItem : class
	{	//****************************************
		private List<GCHandle> _Values;
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
		/// <remarks>Will perform a compaction</remarks>
		public bool Remove(TItem item)
		{
			int Index = 0;
			
			do
			{
				var Handle = _Values[Index];
				TItem TargetItem = (TItem)Handle.Target;
				
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
			} while (Index < _Values.Count);
			
			return false;
		}
		
		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
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
			return new WeakEnumerator(_Values);
		}
		
		//****************************************
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new WeakEnumerator(_Values);
		}
		
		private GCHandle CreateFrom(TItem item)
		{
			return GCHandle.Alloc(item, GCHandleType.Weak);
		}
		
		//****************************************
		
		/// <summary>
		/// Gets a list of strong references to the contents of the collection
		/// </summary>
		public IList<TItem> Values
		{
			get { return new List<TItem>(_Values.Select((handle) => (TItem)handle.Target).Where((item) => item != null)); }
		}
		
		//****************************************
		
		private class WeakEnumerator : IEnumerator<TItem>
		{//****************************************
			private List<GCHandle> _Source;
			private int _Index;
			private TItem _Current;
			//****************************************
			
			internal WeakEnumerator(List<GCHandle> source)
			{
				_Source = source;
				_Index = -1;
				_Current = null;
			}
			
			//****************************************
			
			void IDisposable.Dispose()
			{
			}
			
			public bool MoveNext()
			{
				_Index++;
				
				while (_Index < _Source.Count)
				{
					var Handle = _Source[_Index];
					
					_Current = (TItem)Handle.Target;
					
					if (_Current != null)
						return true;
					
					_Source.RemoveAt(_Index);
					
					Handle.Free();
				}

				return false;
			}
			
			public void Reset()
			{
				_Index = -1;
			}
			
			//****************************************
			
			object IEnumerator.Current
			{
				get { return _Current; }
			}
			
			public TItem Current
			{
				get { return _Current; }
			}
		}
	}
}
