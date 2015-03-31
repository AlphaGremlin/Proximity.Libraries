/****************************************\
 PagedSetBase.cs
 Created: 2014-08-05
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Describes a page in a Paged Set
	/// </summary>
	public class PagedSetPage<TKey, TItem> : IComparable<PagedSetPage<TKey, TItem>> where TKey : IComparable<TKey>
	{	//****************************************
		private readonly SortedSet<PagedSetItem<TKey, TItem>> _Items;
		private bool _IsStart, _IsFinish;
		private DateTime _AccessTime;
		//****************************************

		internal PagedSetPage(bool isStart, bool isFinish)
		{
			_IsStart = isStart;
			_IsFinish = isFinish;

			_AccessTime = DateTime.Now;
			_Items = new SortedSet<PagedSetItem<TKey, TItem>>();
		}

		internal PagedSetPage(IEnumerable<PagedSetItem<TKey, TItem>> items, bool isStart, bool isFinish)
		{
			_IsStart = isStart;
			_IsFinish = isFinish;

			_AccessTime = DateTime.Now;
			_Items = new SortedSet<PagedSetItem<TKey, TItem>>(items);
		}

		//****************************************

		int IComparable<PagedSetPage<TKey, TItem>>.CompareTo(PagedSetPage<TKey, TItem> other)
		{
			return this.Min.CompareTo(other.Min);
		}

		//****************************************

		internal void Append(PagedSetItem<TKey, TItem> item)
		{
			_Items.Add(item);
		}

		internal void Merge(IEnumerable<PagedSetItem<TKey, TItem>> items)
		{
			_Items.UnionWith(items);
		}

		internal ISet<PagedSetItem<TKey, TItem>> GetBefore(TKey key)
		{
			return _Items.GetViewBetween(_Items.Min, new PagedSetItem<TKey, TItem>(key));
		}

		//****************************************

		/// <summary>
		/// Gets the maximum key on this page
		/// </summary>
		public TKey Max
		{
			get { return _Items.Max.Key; }
		}

		/// <summary>
		/// Gets the minimum key on this page
		/// </summary>
		public TKey Min
		{
			get { return _Items.Min.Key; }
		}

		internal bool IsStart
		{
			get { return _IsStart; }
			set { _IsStart = value; }
		}

		internal bool IsFinish
		{
			get { return _IsFinish; }
			set { _IsFinish = value; }
		}

		internal ISet<PagedSetItem<TKey, TItem>> Items
		{
			get { return _Items; }
		}
	}
}
