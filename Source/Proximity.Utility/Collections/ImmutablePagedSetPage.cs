/****************************************\
 ImmutablePagedSetPage.cs
 Created: 2015-06-24
\****************************************/
#if !NET40
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Describes a page in a Paged Set
	/// </summary>
	[SecurityCritical]
	public class ImmutablePagedSetPage<TKey, TItem> : IComparable<ImmutablePagedSetPage<TKey, TItem>> where TKey : IComparable<TKey>
	{
		internal static readonly ImmutablePagedSetPage<TKey, TItem> Empty = new ImmutablePagedSetPage<TKey, TItem>(true, true, default(TKey), default(TKey));

		//****************************************
		private readonly ImmutableSortedSet<PagedSetItem<TKey, TItem>> _Items;
		private readonly bool _IsStart, _IsFinish;
		private TKey _Min, _Max;
		//****************************************

		private ImmutablePagedSetPage(ImmutableSortedSet<PagedSetItem<TKey, TItem>> items, bool isStart, bool isFinish, TKey min, TKey max)
		{
			_IsStart = isStart;
			_IsFinish = isFinish;

			_Items = items;
			_Min = min;
			_Max = max;
		}

		internal ImmutablePagedSetPage(bool isStart, bool isFinish, TKey min, TKey max)
		{
			_IsStart = isStart;
			_IsFinish = isFinish;

			_Items = ImmutableSortedSet<PagedSetItem<TKey, TItem>>.Empty;
			_Min = min;
			_Max = max;
		}

		internal ImmutablePagedSetPage(IEnumerable<PagedSetItem<TKey, TItem>> items, bool isStart, bool isFinish)
		{
			_IsStart = isStart;
			_IsFinish = isFinish;

			_Items = ImmutableSortedSet.CreateRange<PagedSetItem<TKey, TItem>>(items);
			_Min = _Items.Min.Key;
			_Max = _Items.Max.Key;
		}

		internal ImmutablePagedSetPage(IEnumerable<PagedSetItem<TKey, TItem>> items, bool isStart, bool isFinish, TKey min, TKey max)
		{
			_IsStart = isStart;
			_IsFinish = isFinish;

			_Items = ImmutableSortedSet.CreateRange<PagedSetItem<TKey, TItem>>(items);
			_Min = min;
			_Max = max;
		}

		//****************************************

		[SecuritySafeCritical]
		int IComparable<ImmutablePagedSetPage<TKey, TItem>>.CompareTo(ImmutablePagedSetPage<TKey, TItem> other)
		{
			return _Min.CompareTo(_Min);
		}

		//****************************************

		internal ImmutablePagedSetPage<TKey, TItem> Append(PagedSetItem<TKey, TItem> item)
		{
			var NewItems = _Items.Add(item);

			if (object.ReferenceEquals(NewItems, _Items))
				return this;

			return new ImmutablePagedSetPage<TKey, TItem>(NewItems, _IsStart, _IsFinish, GetMin(_Min, item.Key), GetMax(_Max, item.Key));
		}

		internal ImmutablePagedSetPage<TKey, TItem> Merge(IEnumerable<PagedSetItem<TKey, TItem>> items)
		{
			var NewItems = _Items.Union(items);

			if (object.ReferenceEquals(NewItems, _Items))
				return this;

			return new ImmutablePagedSetPage<TKey, TItem>(NewItems, _IsStart, _IsFinish, GetMin(NewItems.Min.Key, _Min), GetMax(NewItems.Max.Key, _Max));
		}

		internal ImmutablePagedSetPage<TKey, TItem> Merge(IEnumerable<PagedSetItem<TKey, TItem>> items, TKey min, TKey max)
		{
			var NewItems = _Items.Union(items);

			if (object.ReferenceEquals(NewItems, _Items))
				return this;

			return new ImmutablePagedSetPage<TKey, TItem>(NewItems, _IsStart, _IsFinish, GetMin(_Min, min), GetMax(_Max, max));
		}

		internal ImmutablePagedSetPage<TKey, TItem> MergeMin(IEnumerable<PagedSetItem<TKey, TItem>> items, TKey min)
		{
			var NewItems = _Items.Union(items);

			if (object.ReferenceEquals(NewItems, _Items))
				return this;

			return new ImmutablePagedSetPage<TKey, TItem>(NewItems, _IsStart, _IsFinish, GetMin(_Min, min), GetMax(NewItems.Max.Key, _Max));
		}

		internal ImmutablePagedSetPage<TKey, TItem> MergeMax(IEnumerable<PagedSetItem<TKey, TItem>> items, TKey max)
		{
			var NewItems = _Items.Union(items);

			if (object.ReferenceEquals(NewItems, _Items))
				return this;

			return new ImmutablePagedSetPage<TKey, TItem>(NewItems, _IsStart, _IsFinish, GetMin(NewItems.Min.Key, _Min), GetMax(_Max, max));
		}

		internal ImmutablePagedSetPage<TKey, TItem> WithStart(bool isStart)
		{
			return (isStart == _IsStart) ? this : new ImmutablePagedSetPage<TKey, TItem>(_Items, isStart, _IsFinish, _Min, _Max);
		}

		internal ImmutablePagedSetPage<TKey, TItem> WithFinish(bool isFinish)
		{
			return (isFinish == _IsFinish) ? this : new ImmutablePagedSetPage<TKey, TItem>(_Items, _IsStart, isFinish, _Min, _Max);
		}

		internal int TryGetAfter(TKey key, out IEnumerable<TItem> results)
		{	//****************************************
			var TopIndex = _Items.NearestAboveAscending(new PagedSetItem<TKey, TItem>(key));
			//****************************************

			if (TopIndex == -1)
			{
				results = null;

				return 0;
			}

			results = _Items.Skip(TopIndex).Select(FromItem);

			return _Items.Count - TopIndex;
		}

		internal int TryGetBefore(TKey key, out IEnumerable<TItem> results)
		{	//****************************************
			var TopIndex = _Items.NearestBelowAscending(new PagedSetItem<TKey, TItem>(key));
			//****************************************

			if (TopIndex == -1)
			{
				results = null;

				return 0;
			}

			results = _Items.Take(TopIndex + 1).Select(FromItem);

			return TopIndex + 1;
		}

		//****************************************

		/// <summary>
		/// Gets the maximum key on this page
		/// </summary>
		public TKey MaxKey
		{
			get { return _Items.Max.Key; }
		}

		/// <summary>
		/// Gets the minimum key on this page
		/// </summary>
		public TKey MinKey
		{
			get { return _Items.Min.Key; }
		}

		/// <summary>
		/// Gets the maximum range on this page
		/// </summary>
		public TKey Max
		{
			get { return _Max; }
		}

		/// <summary>
		/// Gets the minimum range on this page
		/// </summary>
		public TKey Min
		{
			get { return _Min; }
		}

		/// <summary>
		/// Gets whether this page is the start page
		/// </summary>
		public bool IsStart
		{
			get { return _IsStart; }
		}

		/// <summary>
		/// Gets whether this page is the finish page
		/// </summary>
		public bool IsFinish
		{
			get { return _IsFinish; }
		}

		/// <summary>
		/// Gets the number of items in this page
		/// </summary>
		public int Count
		{
			get { return _Items.Count; }
		}

		/// <summary>
		/// Gets an enumeration of the items in this page
		/// </summary>
		public IEnumerable<TItem> Items
		{
			get { return _Items.Select(FromItem); }
		}

		/// <summary>
		/// Gets a read-only set of the items in this page
		/// </summary>
		internal ISet<PagedSetItem<TKey, TItem>> SetItems
		{
			get { return _Items; }
		}

		//****************************************

		private static TKey GetMax(TKey left, TKey right)
		{
			return (left.CompareTo(right) >= 0) ? left : right;
		}

		private static TKey GetMin(TKey left, TKey right)
		{
			return (left.CompareTo(right) <= 0) ? left : right;
		}

		private static TItem FromItem(PagedSetItem<TKey, TItem> item)
		{
			return item.Item;
		}
	}
}
#endif