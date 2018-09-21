/****************************************\
 PagedSetItem.cs
 Created: 2014-08-05
\****************************************/
using System;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Description of PagedSetItem.
	/// </summary>
	internal struct PagedSetItem<TKey, TItem> : IComparable<PagedSetItem<TKey, TItem>> where TKey : IComparable<TKey>
	{	//****************************************
		private readonly TKey _Key;
		private readonly TItem _Item;
		//****************************************

		internal PagedSetItem(TKey key)
		{
			_Key = key;
			_Item = default(TItem);
		}

		internal PagedSetItem(TKey key, TItem item)
		{
			_Key = key;
			_Item = item;
		}

		//****************************************

		int IComparable<PagedSetItem<TKey, TItem>>.CompareTo(PagedSetItem<TKey, TItem> other)
		{
			return _Key.CompareTo(other._Key);
		}

		//****************************************

		internal TKey Key
		{
			get { return _Key; }
		}

		internal TItem Item
		{
			get { return _Item; }
		}
	}
}
