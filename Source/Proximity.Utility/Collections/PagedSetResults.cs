/****************************************\
 PagedSetResults.cs
 Created: 2015-10-28
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Describes the results of a request from a Paged Set or Immutable Paged Set
	/// </summary>
	/// <typeparam name="TItem">The type of item in the Paged Set</typeparam>
	public struct PagedSetResults<TItem>
	{
		/// <summary>
		/// Gets an empty result set
		/// </summary>
		public static PagedSetResults<TItem> Empty = new PagedSetResults<TItem>(null, false);

		/// <summary>
		/// Gets an empty result set
		/// </summary>
		public static PagedSetResults<TItem> EmptyComplete = new PagedSetResults<TItem>(null, true);

		//****************************************
		private readonly IEnumerable<TItem> _Results;
		private bool _IsComplete;
		//****************************************

		internal PagedSetResults(IEnumerable<TItem> results, bool isComplete)
		{
			_Results = results ?? Enumerable.Empty<TItem>();
			_IsComplete = isComplete;
		}

		//****************************************

		/// <summary>
		/// Gets whether we were able to retrieve a complete result set
		/// </summary>
		public bool IsComplete
		{
			get { return _IsComplete; }
		}

		/// <summary>
		/// Gets the retrieved items
		/// </summary>
		public IEnumerable<TItem> Results
		{
			get { return _Results; }
		}
	}
}
