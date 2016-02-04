/****************************************\
 ImmutablePagedSet.cs
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
	/// Provides an immutable set that implements paging, with multiple minimum and maximum sections
	/// </summary>
	/// <typeparam name="TKey">The type of key for each item</typeparam>
	/// <typeparam name="TItem">The type of item</typeparam>
	[SecurityCritical]
	public abstract class ImmutablePagedSet<TKey, TItem> where TKey : IComparable<TKey>
	{	//****************************************
		private readonly ImmutableSortedSet<ImmutablePagedSetPage<TKey, TItem>> _Pages;
		//****************************************

		/// <summary>
		/// Creates a new Paged Set
		/// </summary>
		protected ImmutablePagedSet()
		{
			_Pages = ImmutableSortedSet<ImmutablePagedSetPage<TKey, TItem>>.Empty;
		}

		/// <summary>
		/// Creates a new Paged Set around an existing set
		/// </summary>
		/// <param name="pages"></param>
		protected ImmutablePagedSet(ImmutableSortedSet<ImmutablePagedSetPage<TKey, TItem>> pages)
		{
			_Pages = pages;
		}

		//****************************************

		/// <summary>
		/// Adds a range of items to the paged set as a new page, merging with existing overlapping pages
		/// </summary>
		/// <param name="items">The set of items to add</param>
		/// <param name="isStart">Whether this page is the start of the set</param>
		/// <param name="isFinish">Whether this page is the end of the set</param>
		/// <returns>A new immutable paged set with the new items</returns>
		public ImmutablePagedSet<TKey, TItem> AddRange(IEnumerable<TItem> items, bool isStart, bool isFinish)
		{	//****************************************
			var SortedItems = items.OrderBy(GetKey).ToArray();
			//****************************************

			// If we receive an empty set, we can't merge since we have no min or max
			if (SortedItems.Length == 0)
			{
				// No other pages, and we're both start and finish, create an empty set to signal this situation
				if (isStart && isFinish && _Pages.Count == 0)
				{
					return Create(_Pages.Add(ImmutablePagedSetPage<TKey, TItem>.Empty));
				}

				return this;
			}

			// Find the ranges
			var Min = GetKey(SortedItems[0]);
			var Max = GetKey(SortedItems[SortedItems.Length - 1]);

			return AddRange(SortedItems, Min, Max, isStart, isFinish);
		}

		/// <summary>
		/// Adds a range of items to the paged set as a new page, merging with existing overlapping pages
		/// </summary>
		/// <param name="items">The set of items to add</param>
		/// <param name="min">The minimum value for this page</param>
		/// <param name="max">The maximum value for this page</param>
		/// <param name="isStart">Whether this page is the start of the set</param>
		/// <param name="isFinish">Whether this page is the end of the set</param>
		/// <returns>A new immutable paged set with the new items</returns>
		public ImmutablePagedSet<TKey, TItem> AddRange(IEnumerable<TItem> items, TKey min, TKey max, bool isStart, bool isFinish)
		{	//****************************************
			var SortedItems = items.OrderBy(GetKey).ToArray();
			//****************************************

			// If we receive an empty set, we can't merge since we have no min or max
			if (SortedItems.Length == 0)
			{
				// No other pages, and we're both start and finish, create an empty set to signal this situation
				if (isStart && isFinish && _Pages.Count == 0)
				{
					return Create(_Pages.Add(new ImmutablePagedSetPage<TKey, TItem>(true, true, min, max)));
				}

				return this;
			}

			return AddRange(SortedItems, min, max, isStart, isFinish);
		}

		/// <summary>
		/// Adds a range of items to the paged set as a new page, merging with existing overlapping pages
		/// </summary>
		/// <param name="items">The set of items to add</param>
		/// <param name="min">The minimum value for this page</param>
		/// <param name="isStart">Whether this page is the start of the set</param>
		/// <param name="isFinish">Whether this page is the end of the set</param>
		/// <returns>A new immutable paged set with the new items</returns>
		public ImmutablePagedSet<TKey, TItem> AddRangeMin(IEnumerable<TItem> items, TKey min, bool isStart, bool isFinish)
		{	//****************************************
			var SortedItems = items.OrderBy(GetKey).ToArray();
			//****************************************

			// If we receive an empty set, we can't merge since we have no min or max
			if (SortedItems.Length == 0)
			{
				// No other pages, and we're both start and finish, create an empty set to signal this situation
				if (isStart && isFinish && _Pages.Count == 0)
				{
					return Create(_Pages.Add(new ImmutablePagedSetPage<TKey, TItem>(true, true, min, default(TKey))));
				}

				return this;
			}

			// Find the maximum and use the given minimum
			var Max = GetKey(SortedItems[SortedItems.Length - 1]);

			return AddRange(SortedItems, min, Max, isStart, isFinish);
		}

		/// <summary>
		/// Adds a range of items to the paged set as a new page, merging with existing overlapping pages
		/// </summary>
		/// <param name="items">The set of items to add</param>
		/// <param name="max">The maximum value for this page</param>
		/// <param name="isStart">Whether this page is the start of the set</param>
		/// <param name="isFinish">Whether this page is the end of the set</param>
		/// <returns>A new immutable paged set with the new items</returns>
		public ImmutablePagedSet<TKey, TItem> AddRangeMax(IEnumerable<TItem> items, TKey max, bool isStart, bool isFinish)
		{	//****************************************
			var SortedItems = items.OrderBy(GetKey).ToArray();
			//****************************************

			// If we receive an empty set, we can't merge since we have no min or max
			if (SortedItems.Length == 0)
			{
				// No other pages, and we're both start and finish, create an empty set to signal this situation
				if (isStart && isFinish && _Pages.Count == 0)
				{
					return Create(_Pages.Add(new ImmutablePagedSetPage<TKey, TItem>(true, true, default(TKey), max)));
				}

				return this;
			}

			// Find the maximum and use the given minimum
			var Min = GetKey(SortedItems[0]);

			return AddRange(SortedItems, Min, max, isStart, isFinish);
		}

		/// <summary>
		/// Adds a single item to the final finished page
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <returns>A new immutable paged set with the new item</returns>
		public ImmutablePagedSet<TKey, TItem> AppendToLatest(TItem item)
		{
			if (_Pages.Count == 0)
			{
				return Create(_Pages.Add(ImmutablePagedSetPage<TKey, TItem>.Empty.Append(new PagedSetItem<TKey, TItem>(GetKey(item), item))));
			}

			return Create(Replace(_Pages, _Pages.Max, _Pages.Max.Append(new PagedSetItem<TKey, TItem>(GetKey(item), item))));
		}

		/// <summary>
		/// Mark the final page as unfinished
		/// </summary>
		/// <returns>A new immutable paged set with the final page unfinished</returns>
		public ImmutablePagedSet<TKey, TItem> InvalidateFinish()
		{
			if (_Pages.Count == 0 || !_Pages.Max.IsFinish)
				return this;

			return Create(Replace(_Pages, _Pages.Max, _Pages.Max.WithFinish(false)));
		}

		/// <summary>
		/// Attempts to read records on and after the given key
		/// </summary>
		/// <param name="key">The key to read from</param>
		/// <param name="count">The number of records to try and read. If omitted, reads as many as are available</param>
		/// <returns>An enumeration of the desired records, or null if no data is available</returns>
		/// <remarks>The enumeration may contain more or less than the desired count, depending on availability</remarks>
		public PagedSetResults<TItem> TryReadAfter(TKey key, int? count)
		{	//****************************************
			int PageCount;
			IEnumerable<TItem> PageResults;
			//****************************************

			foreach (var MyPage in _Pages)
			{
				// Try and find a page that covers the key
				if (MyPage.Min.CompareTo(key) <= 0 && MyPage.Max.CompareTo(key) >= 0)
				{
					if (count.HasValue)
					{
						// We have a count specified
						PageCount = MyPage.TryGetAfter(key, out PageResults);

						// Are there enough results, or have we reached the end?
						if (PageCount >= count.Value || MyPage.IsFinish)
						{
							//Log.Debug("PagedSet seeking {0} found Page {1} to {2} with {3} of {4} items", key, MyPage.Min, MyPage.Max, Results.Count, count.Value);

							return new PagedSetResults<TItem>(PageResults, true);
						}

						//Log.Debug("PagedSet seeking {0} found Page {1} to {2} but only found {3} of {4} items", key, MyPage.Min, MyPage.Max, Results.Count, count.Value); 

						// Data is not fully paged in
						return new PagedSetResults<TItem>(PageResults, false);
					}
					else
					{
						// No count, are we a finish page?
						if (MyPage.IsFinish)
						{
							PageCount = MyPage.TryGetAfter(key, out PageResults);

							//Log.Debug("PagedSet seeking {0} found finish Page {1} to {2} with {3} of {4} items", key, MyPage.Min, MyPage.Max, Results.Count, count.Value);

							return new PagedSetResults<TItem>(PageResults, true);
						}

						//Log.Debug("PagedSet seeking {0} found Page {1} to {2} but it was not the finish page", key, MyPage.Min, MyPage.Max);

						// No, so we don't have everything, which means we need to page it in
						return PagedSetResults<TItem>.Empty;
					}
				}
			}

			// No page covered our key, are we requesting outside the cache?
			if (_Pages.Count != 0)
			{
				var MyPage = _Pages.Max;

				// Requesting after a complete cache, return an empty set
				if (MyPage.Max.CompareTo(key) < 0 && MyPage.IsFinish)
				{
					//Log.Debug("PagedSet seeking {0} was after the Finish page", key);

					return PagedSetResults<TItem>.EmptyComplete;
				}

				// Check if there's any data at all
				if (MyPage.IsStart && MyPage.IsFinish)
				{
					//Log.Debug("PagedSet seeking {0} found an empty set", key);

					return PagedSetResults<TItem>.EmptyComplete;
				}
			}

			//Log.Debug("PagedSet seeking {0} found no pages", key);

			// No page that covered our data was found
			return PagedSetResults<TItem>.Empty;
		}

		/// <summary>
		/// Attempts to read records from the start of the set
		/// </summary>
		/// <param name="count">The number of records to try and retrieve</param>
		/// <returns>An enumeration of the desired records, or null if no data is available</returns>
		/// <remarks>The enumeration may contain more or less than the desired count, depending on availability</remarks>
		public PagedSetResults<TItem> TryReadAfter(int? count)
		{
			if (_Pages.Count == 0 || !_Pages.Min.IsStart)
			{
				//Log.Debug("PagedSet was empty or did not have a start page");

				return PagedSetResults<TItem>.Empty;
			}

			if (count.HasValue)
			{
				if (_Pages.Min.Count >= count.Value)
					return new PagedSetResults<TItem>(_Pages.Min.Items, true);

				//Log.Debug("PagedSet found only {0} of {1} items", _Pages.Max.Items.Count, count.Value);

				return new PagedSetResults<TItem>(_Pages.Min.Items, false);
			}

			if (!_Pages.Min.IsFinish)
			{
				//Log.Debug("PagedSet did not have a starting page");

				return new PagedSetResults<TItem>(_Pages.Min.Items, false);
			}

			return new PagedSetResults<TItem>(_Pages.Min.Items, true);
		}

		/// <summary>
		/// Attempts to read records on and before the given key
		/// </summary>
		/// <param name="key">The key to read to</param>
		/// <param name="count">The number of records to try and read. If omitted, reads as many as are available</param>
		/// <returns>An enumeration of the desired records, or null if no data is available</returns>
		/// <remarks>The enumeration may contain more or less than the desired count, depending on availability</remarks>
		public PagedSetResults<TItem> TryReadBefore(TKey key, int? count)
		{	//****************************************
			int PageCount;
			IEnumerable<TItem> PageResults;
			//****************************************

			foreach (var MyPage in _Pages)
			{
				// Try and find a page that covers the key
				if (MyPage.Min.CompareTo(key) <= 0 && MyPage.Max.CompareTo(key) >= 0)
				{
					if (count.HasValue)
					{
						// We have a count specified
						PageCount = MyPage.TryGetBefore(key, out PageResults);

						// Are there enough results, or have we reached the start?
						if (PageCount >= count.Value || MyPage.IsStart)
						{
							//Log.Debug("PagedSet seeking {0} found Page {1} to {2} with {3} of {4} items", key, MyPage.Min, MyPage.Max, Results.Count, count.Value);

							return new PagedSetResults<TItem>(PageResults, true);
						}

						//Log.Debug("PagedSet seeking {0} found Page {1} to {2} but only found {3} of {4} items", key, MyPage.Min, MyPage.Max, Results.Count, count.Value); 

						// Data is not currently paged in, return what we have
						return new PagedSetResults<TItem>(PageResults, false);
					}
					else
					{
						// No count, are we a start page?
						if (MyPage.IsStart)
						{
							PageCount = MyPage.TryGetBefore(key, out PageResults);

							//Log.Debug("PagedSet seeking {0} found start Page {1} to {2} with {3} of {4} items", key, MyPage.Min, MyPage.Max, Results.Count, count.Value);

							return new PagedSetResults<TItem>(PageResults, true);
						}

						//Log.Debug("PagedSet seeking {0} found Page {1} to {2} but it was not the start page", key, MyPage.Min, MyPage.Max);

						// No, so we don't have everything, which means we need to page it in
						return PagedSetResults<TItem>.Empty;
					}
				}
			}

			// No page covered our key, are we requesting outside the cache?
			if (_Pages.Count != 0)
			{
				var MyPage = _Pages.Min;

				// Requesting before a complete cache, return an empty set
				if (MyPage.Min.CompareTo(key) > 0 && MyPage.IsStart)
				{
					//Log.Debug("PagedSet seeking {0} was before the Start page", key);

					return PagedSetResults<TItem>.EmptyComplete;
				}

				// Check if there's any data at all
				if (MyPage.IsStart && MyPage.IsFinish)
				{
					//Log.Debug("PagedSet seeking {0} found an empty set", key);

					return PagedSetResults<TItem>.EmptyComplete;
				}
			}

			//Log.Debug("PagedSet seeking {0} found no pages", key);

			// No page that covered our data was found
			return PagedSetResults<TItem>.Empty;
		}

		/// <summary>
		/// Attempts to read records from the end of the set
		/// </summary>
		/// <param name="count">The number of records to try and retrieve</param>
		/// <returns>An enumeration of the desired records, or null if no data is available</returns>
		/// <remarks>The enumeration may contain more or less than the desired count, depending on availability</remarks>
		public PagedSetResults<TItem> TryReadBefore(int? count)
		{
			if (_Pages.Count == 0 || !_Pages.Max.IsFinish)
			{
				//Log.Debug("PagedSet was empty or did not have a finish page");

				return PagedSetResults<TItem>.Empty;
			}

			if (count.HasValue)
			{
				if (_Pages.Max.Count >= count.Value)
					return new PagedSetResults<TItem>(_Pages.Max.Items, true);

				//Log.Debug("PagedSet found only {0} of {1} items", _Pages.Max.Items.Count, count.Value);

				return new PagedSetResults<TItem>(_Pages.Max.Items, false);
			}

			if (!_Pages.Max.IsStart)
			{
				//Log.Debug("PagedSet did not have a starting page");

				return new PagedSetResults<TItem>(_Pages.Max.Items, false);
			}

			return new PagedSetResults<TItem>(_Pages.Max.Items, true);
		}

		//****************************************

		/// <summary>
		/// When overridden, retrieves the key for an item
		/// </summary>
		/// <param name="item">The item to retrieve the key from</param>
		/// <returns>The key for the given item</returns>
		protected abstract TKey GetKey(TItem item);

		/// <summary>
		/// When overridden, creates a new instance of the immutable set
		/// </summary>
		/// <param name="pages">The new set of pages to use</param>
		/// <returns>A new immutable set containing the provided pages</returns>
		protected abstract ImmutablePagedSet<TKey, TItem> Create(ImmutableSortedSet<ImmutablePagedSetPage<TKey, TItem>> pages);

		//****************************************

		private ImmutablePagedSet<TKey, TItem> AddRange(TItem[] items, TKey min, TKey max, bool isStart, bool isFinish)
		{	//****************************************
			ImmutablePagedSetPage<TKey, TItem> OverlapsWith = null;
			List<ImmutablePagedSetPage<TKey, TItem>> OverlappingPages = null;
			ImmutableSortedSet<ImmutablePagedSetPage<TKey, TItem>>.Builder NewPages = null;
			//****************************************

			// We have a set of items
			var PageItems = items.Select((item) => new PagedSetItem<TKey, TItem>(GetKey(item), item));

			// Do we overlap with any existing pages?
			foreach (var MyPage in _Pages)
			{
				if ((MyPage.Min.CompareTo(max) <= 0 && MyPage.Max.CompareTo(min) >= 0) || MyPage.Count == 0)
				{
					if (OverlapsWith != null)
					{
						// We overlap with multiple pages
						if (OverlappingPages == null)
							OverlappingPages = new List<ImmutablePagedSetPage<TKey, TItem>>();

						OverlappingPages.Add(MyPage);
					}
					else
					{
						OverlapsWith = MyPage;
					}
				}
			}

			NewPages = _Pages.ToBuilder();

			// No matching page we overlap with
			if (OverlapsWith == null)
			{
				// Is there already a start or finish page?
				if (_Pages.Count != 0)
				{
					if (isStart && _Pages.Min.IsStart)
					{
						// If our maximum is less than the minimum page, we can't take over as start page
						if (_Pages.Min.Min.CompareTo(max) < 0)
							throw new InvalidOperationException("IsStart when there's already a lower start page");

						Replace(NewPages, NewPages.Min, NewPages.Min.WithStart(false)); // This page is taking over as the start page
					}

					if (isFinish && _Pages.Max.IsFinish)
					{
						// If our minimum is greater than the maximum page, we can't take over as the finish page
						if (_Pages.Max.Max.CompareTo(min) > 0)
							throw new InvalidOperationException("IsFinish when there's already a finish page");

						Replace(NewPages, NewPages.Max, NewPages.Max.WithFinish(false)); // This page is taking over as the finish page
					}
				}

				NewPages.Add(new ImmutablePagedSetPage<TKey, TItem>(PageItems, isStart, isFinish, min, max));

				//Log.Debug("PagedSet adding new page from {0}{1} to {2}{3} ({4} items)", Min, isStart ? " (Start)" : "", Max, isFinish ? " (Finish)" : "", PageItems.Count());
			}
			else if (OverlappingPages == null)
			{
				Replace(NewPages, OverlapsWith, OverlapsWith.Merge(PageItems, min, max));

				/*
				Log.Debug("PagedSet merging page from {0}{1} to {2}{3} with {4}{5} to {6}{7}", 
					Min, isStart ? " (Start)" : "", Max, isFinish ? " (Finish)" : "",
					OverlapsWith.Min, OverlapsWith.IsStart ? " (Start)" : "", OverlapsWith.Max, OverlapsWith.IsFinish ? " (Finish)" : "");
				*/
			}
			else
			{
				// Remove the pages that overlap
				foreach (var MyPage in OverlappingPages)
				{
					NewPages.Remove(MyPage);
				}

				var FirstPage = OverlappingPages[0];
				var LastPage = OverlappingPages[OverlappingPages.Count - 1];

				/*
				Log.Debug("PagedSet merging page from {0}{1} to {2}{3} with {4} other pages {5}{6} to {7}{8}",
					Min, isStart ? " (Start)" : "", Max, isFinish ? " (Finish)" : "",
					OverlappingPages.Count,
					FirstPage.Min, FirstPage.IsStart ? " (Start)" : "", LastPage.Max, LastPage.IsFinish ? " (Finish)" : "");
				*/

				// Merge the first and last pages with the added pages
				NewPages.Add(FirstPage.Merge(PageItems.Concat(LastPage.SetItems), min, max));
			}

			return Create(NewPages.ToImmutable());
		}

		//****************************************

		/// <summary>
		/// Gets a read-only set of the available pages
		/// </summary>
		public ISet<ImmutablePagedSetPage<TKey, TItem>> Pages
		{
			get { return _Pages; }
		}

		/// <summary>
		/// Gets the minimum page, if any
		/// </summary>
		public ImmutablePagedSetPage<TKey, TItem> Min
		{
			get { return _Pages.Min; }
		}

		/// <summary>
		/// Gets the maximum page
		/// </summary>
		public ImmutablePagedSetPage<TKey, TItem> Max
		{
			get { return _Pages.Max; }
		}

		/// <summary>
		/// Gets the total number of items
		/// </summary>
		public int TotalItems
		{
			get { return _Pages.Sum(page => page.Count); }
		}

		//****************************************

		private static ImmutableSortedSet<ImmutablePagedSetPage<TKey, TItem>> Replace(ImmutableSortedSet<ImmutablePagedSetPage<TKey, TItem>> target, ImmutablePagedSetPage<TKey, TItem> oldItem, ImmutablePagedSetPage<TKey, TItem> newItem)
		{
			if (object.ReferenceEquals(oldItem, newItem))
				return target;

			var MyBuilder = target.ToBuilder();

			MyBuilder.Remove(oldItem);
			MyBuilder.Add(newItem);

			return MyBuilder.ToImmutable();
		}

		private static void Replace(ImmutableSortedSet<ImmutablePagedSetPage<TKey, TItem>>.Builder target, ImmutablePagedSetPage<TKey, TItem> oldItem, ImmutablePagedSetPage<TKey, TItem> newItem)
		{
			if (object.ReferenceEquals(oldItem, newItem))
				return;

			target.Remove(oldItem);
			target.Add(newItem);
		}

		//****************************************

	}
}
#endif