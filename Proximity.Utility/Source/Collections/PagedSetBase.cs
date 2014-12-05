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
	/// Provides a set that implements paging, with multiple minimum and maximum sections
	/// </summary>
	/// <typeparam name="TKey">The type of key for each item</typeparam>
	/// <typeparam name="TItem">The type of item</typeparam>
	public abstract class PagedSetBase<TKey, TItem> where TKey : IComparable<TKey>
	{	//****************************************
		private readonly SortedSet<PagedSetPage<TKey, TItem>> _Pages = new SortedSet<PagedSetPage<TKey, TItem>>();
		private ReadOnlySet<PagedSetPage<TKey, TItem>> _PagesReadOnly;
		//****************************************
		
		/// <summary>
		/// Creates a new Paged Set
		/// </summary>
		protected PagedSetBase()
		{
			_PagesReadOnly = new ReadOnlySet<PagedSetPage<TKey, TItem>>(_Pages);
		}
		
		//****************************************

		public void AddRange(IEnumerable<TItem> items, bool isStart, bool isFinish)
		{	//****************************************
			var SortedItems = items.OrderBy(GetKey).ToArray();
			PagedSetPage<TKey, TItem> OverlapsWith = null;
			List<PagedSetPage<TKey, TItem>> OverlappingPages = null;
			//****************************************

			// If we receive an empty set, we can't merge since we have no min or max
			if (SortedItems.Length == 0)
			{
				// No other pages, and we're both start and finish, create an empty set to signal this situation
				if (isStart && isFinish && _Pages.Count == 0)
				{
					_Pages.Add(new PagedSetPage<TKey, TItem>(true, true));
				}

				return;
			}

			// We have a set of items
			var Min = GetKey(SortedItems[0]);
			var Max = GetKey(SortedItems[SortedItems.Length - 1]);
			var PageItems = SortedItems.Select((item) => new PagedSetItem<TKey, TItem>(GetKey(item), item));

			// Do we overlap with any existing pages?
			foreach (var MyPage in _Pages)
			{
				if ((MyPage.Min.CompareTo(Max) <= 0 && MyPage.Max.CompareTo(Min) >= 0) || MyPage.Items.Count == 0)
				{
					if (OverlapsWith != null)
					{
						// We overlap with multiple pages
						if (OverlappingPages == null)
							OverlappingPages = new List<PagedSetPage<TKey, TItem>>();

						OverlappingPages.Add(MyPage);
					}
					else
					{
						OverlapsWith = MyPage;
					}
				}
			}

			// No matching page we overlap with
			if (OverlapsWith == null)
			{
				// Is there already a start or finish page?
				if (_Pages.Count != 0)
				{
					if (isStart && _Pages.Min.IsStart)
					{
						// If our maximum is less than the minimum page, we can't take over as start page
						if (_Pages.Min.Min.CompareTo(Max) < 0)
							throw new InvalidOperationException("IsStart when there's already a lower start page");

						_Pages.Min.IsStart = false; // This page is taking over as the start page
					}

					if (isFinish && _Pages.Max.IsFinish)
					{
						// If our minimum is greater than the maximum page, we can't take over as the finish page
						if (_Pages.Max.Max.CompareTo(Min) > 0)
						throw new InvalidOperationException("IsFinish when there's already a finish page");

						_Pages.Max.IsFinish = false; // This page is taking over as the finish page
					}
				}

				_Pages.Add(new PagedSetPage<TKey, TItem>(PageItems, isStart, isFinish));

				//Log.Debug("PagedSet adding new page from {0}{1} to {2}{3} ({4} items)", Min, isStart ? " (Start)" : "", Max, isFinish ? " (Finish)" : "", PageItems.Count());
			}
			else if (OverlappingPages == null)
			{
				_Pages.Remove(OverlapsWith);

				OverlapsWith.Merge(PageItems);

				_Pages.Add(OverlapsWith);

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
					_Pages.Remove(MyPage);
				}

				var FirstPage = OverlappingPages[0];
				var LastPage = OverlappingPages[OverlappingPages.Count - 1];

				/*
				Log.Debug("PagedSet merging page from {0}{1} to {2}{3} with {4} other pages {5}{6} to {7}{8}",
					Min, isStart ? " (Start)" : "", Max, isFinish ? " (Finish)" : "",
					OverlappingPages.Count,
					FirstPage.Min, FirstPage.IsStart ? " (Start)" : "", LastPage.Max, LastPage.IsFinish ? " (Finish)" : "");
				*/

				// Merge the first page with the added pages
				FirstPage.Merge(PageItems);
				// Merge in the last page
				FirstPage.Merge(LastPage.Items);

				_Pages.Add(FirstPage);
			}
		}

		public void AppendToFinish(TItem item)
		{
			if (_Pages.Count == 0 || !_Pages.Max.IsFinish)
				return;

			_Pages.Max.Append(new PagedSetItem<TKey, TItem>(GetKey(item), item));
		}

		public void InvalidateFinish()
		{
			if (_Pages.Count == 0 || !_Pages.Max.IsFinish)
				return;

			_Pages.Max.IsFinish = false;
		}

		public IEnumerable<TItem> TryReadBefore(TKey key, int? count)
		{
			foreach (var MyPage in _Pages)
			{
				// Try and find a page that covers the key
				if (MyPage.Min.CompareTo(key) <= 0 && MyPage.Max.CompareTo(key) >= 0)
				{
					if (count.HasValue)
					{
						// We have a count specified
						var Results = MyPage.GetBefore(key);

						// Are there enough results, or have we reached the start?
						if (Results.Count >= count.Value || MyPage.IsStart)
						{
							//Log.Debug("PagedSet seeking {0} found Page {1} to {2} with {3} of {4} items", key, MyPage.Min, MyPage.Max, Results.Count, count.Value);

							return Results.Select(FromItem);
						}

						//Log.Debug("PagedSet seeking {0} found Page {1} to {2} but only found {3} of {4} items", key, MyPage.Min, MyPage.Max, Results.Count, count.Value); 

						// Data is not currently paged in
						// HACK: Return it anyway, prevents some annyoing bugs with trade history
						return Results.Select(FromItem);
					}
					else
					{
						// No count, are we a start page?
						if (MyPage.IsStart)
						{
							var Results = MyPage.GetBefore(key);

							//Log.Debug("PagedSet seeking {0} found start Page {1} to {2} with {3} of {4} items", key, MyPage.Min, MyPage.Max, Results.Count, count.Value);

							return Results.Select(FromItem);
						}

						//Log.Debug("PagedSet seeking {0} found Page {1} to {2} but it was not the start page", key, MyPage.Min, MyPage.Max);

						// No, so we don't have everything, which means we need to page it in
						return null;
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

					return Enumerable.Empty<TItem>();
				}

				// Check if there's any data at all
				if (MyPage.IsStart && MyPage.IsFinish)
				{
					//Log.Debug("PagedSet seeking {0} found an empty set", key);

					return Enumerable.Empty<TItem>();
				}
			}

			//Log.Debug("PagedSet seeking {0} found no pages", key);

			// No page that covered our data was found
			return null;
		}

		public IEnumerable<TItem> TryReadBefore(int? count)
		{
			if (_Pages.Count == 0 || !_Pages.Max.IsFinish)
			{
				//Log.Debug("PagedSet was empty or did not have a finish page");

				return null;
			}

			if (count.HasValue)
			{
				if (_Pages.Max.Items.Count >= count.Value)
					return _Pages.Max.Items.Select(FromItem);

				//Log.Debug("PagedSet found only {0} of {1} items", _Pages.Max.Items.Count, count.Value);

				return null;
			}

			if (!_Pages.Max.IsStart)
			{
				//Log.Debug("PagedSet did not have a starting page");

				return null;
			}

			return _Pages.Max.Items.Select(FromItem);
		}

		//****************************************

		/// <summary>
		/// When overridden, retrieves the key for an item
		/// </summary>
		/// <param name="item">The item to retrieve the key from</param>
		/// <returns>The key for the given item</returns>
		protected abstract TKey GetKey(TItem item);

		//****************************************

		private TItem FromItem(PagedSetItem<TKey, TItem> item)
		{
			return item.Item;
		}

		//****************************************

		public ISet<PagedSetPage<TKey, TItem>> Pages
		{
			get
			{
				if (_PagesReadOnly == null)
					_PagesReadOnly = new ReadOnlySet<PagedSetPage<TKey, TItem>>(_Pages);
				
				return _PagesReadOnly;
			}
		}

		/// <summary>
		/// Gets the total number of items
		/// </summary>
		public int TotalItems
		{
			get { return _Pages.Sum(page => page.Items.Count); }
		}
	}
}
