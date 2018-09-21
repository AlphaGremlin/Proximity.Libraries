/****************************************\
 PagedSet.cs
 Created: 2014-12-05
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
	/// Provdes a Paged Set that takes a key selector
	/// </summary>
	public sealed class PagedSet<TKey, TItem> : PagedSetBase<TKey, TItem> where TKey : IComparable<TKey>
	{	//****************************************
		private readonly Func<TItem, TKey> _KeySelector;
		//****************************************
		
		/// <summary>
		/// Creates a new Paged Set
		/// </summary>
		/// <param name="keySelector">The key selector to use to extra the key of an item</param>
		public PagedSet(Func<TItem, TKey> keySelector) : base()
		{
			_KeySelector = keySelector;
		}
		
		//****************************************
		
		/// <inheritdoc />
		protected override TKey GetKey(TItem item)
		{
			return _KeySelector(item);
		}
	}
}
