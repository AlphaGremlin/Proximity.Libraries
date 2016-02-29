/****************************************\
 ObservableListViewDynamic.cs
 Created: 2016-02-22
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Provides an Observable View over a list that performs sorting based on a comparer and dynamically updates based on property changes
	/// </summary>
	/// <typeparam name="TValue">The type of the values in the list</typeparam>
	public class ObservableListViewDynamic<TValue> : ObservableListView<TValue> where TValue : class, INotifyPropertyChanged
	{	//****************************************
		private readonly HashSet<string> _PropertyNames;
		//****************************************
		
		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		public ObservableListViewDynamic(IList<TValue> source) : this(source, (IComparer<TValue>)null, null, null)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparison">A delegate to perform the comparison with</param>
		public ObservableListViewDynamic(IList<TValue> source, Comparison<TValue> comparison) : this(source, comparison, null, null)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparer">The comparer to use for sorting</param>
		public ObservableListViewDynamic(IList<TValue> source, IComparer<TValue> comparer) : this(source, comparer, null, null)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="filter">A filter to apply to the source list</param>
		public ObservableListViewDynamic(IList<TValue> source, Predicate<TValue> filter) : this(source, (IComparer<TValue>)null, filter, null)
		{
		}
		
		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparison">A delegate to perform the comparison with</param>
		/// <param name="filter">A filter to apply to the source list</param>
		public ObservableListViewDynamic(IList<TValue> source, Comparison<TValue> comparison, Predicate<TValue> filter) : this(source, comparison, filter, null)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparer">The comparer to use for sorting</param>
		/// <param name="filter">A filter to apply to the source list</param>
		public ObservableListViewDynamic(IList<TValue> source, IComparer<TValue> comparer, Predicate<TValue> filter) : this(source, comparer, filter, null)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		public ObservableListViewDynamic(IList<TValue> source, IEnumerable<string> propertyNames) : this(source, (IComparer<TValue>)null, null, propertyNames)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparison">A delegate to perform the comparison with</param>
		public ObservableListViewDynamic(IList<TValue> source, Comparison<TValue> comparison, IEnumerable<string> propertyNames) : this(source, comparison, null, propertyNames)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparer">The comparer to use for sorting</param>
		public ObservableListViewDynamic(IList<TValue> source, IComparer<TValue> comparer, IEnumerable<string> propertyNames) : this(source, comparer, null, propertyNames)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="filter">A filter to apply to the source list</param>
		public ObservableListViewDynamic(IList<TValue> source, Predicate<TValue> filter, IEnumerable<string> propertyNames) : this(source, (IComparer<TValue>)null, filter, propertyNames)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparison">A delegate to perform the comparison with</param>
		/// <param name="filter">A filter to apply to the source list</param>
		public ObservableListViewDynamic(IList<TValue> source, Comparison<TValue> comparison, Predicate<TValue> filter, IEnumerable<string> propertyNames) : base(source, comparison, filter)
		{
			if (propertyNames != null)
				_PropertyNames = new HashSet<string>(propertyNames);

			foreach (var MyItem in this)
				MyItem.PropertyChanged += OnChildPropertyChanged;
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparer">The comparer to use for sorting</param>
		/// <param name="filter">A filter to apply to the source list</param>
		public ObservableListViewDynamic(IList<TValue> source, IComparer<TValue> comparer, Predicate<TValue> filter, IEnumerable<string> propertyNames) : base(source, comparer, filter)
		{
			if (propertyNames != null)
				_PropertyNames = new HashSet<string>(propertyNames);

			foreach (var MyItem in this)
				MyItem.PropertyChanged += OnChildPropertyChanged;
		}

		//****************************************

		/// <inheritdoc />
		public override void Dispose()
		{
			base.Dispose();

			foreach (var MyItem in this)
				MyItem.PropertyChanged -= OnChildPropertyChanged;
		}

		/// <inheritdoc />
		protected override void OnCollectionChanged(TValue[] oldItems)
		{
			foreach (var MyItem in oldItems)
				MyItem.PropertyChanged -= OnChildPropertyChanged;

			base.OnCollectionChanged(oldItems);
		}

		/// <inheritdoc />
		protected override void OnCollectionChanged(NotifyCollectionChangedAction action, TValue changedItem, int index)
		{
			switch (action)
			{
			case NotifyCollectionChangedAction.Add:
				changedItem.PropertyChanged += OnChildPropertyChanged;
				break;

			case NotifyCollectionChangedAction.Remove:
				changedItem.PropertyChanged -= OnChildPropertyChanged;
				break;
			}

			base.OnCollectionChanged(action, changedItem, index);
		}

		/// <inheritdoc />
		protected override void OnCollectionChanged(NotifyCollectionChangedAction action, TValue newItem, TValue oldItem, int index)
		{
			oldItem.PropertyChanged -= OnChildPropertyChanged;
			newItem.PropertyChanged += OnChildPropertyChanged;

			base.OnCollectionChanged(action, newItem, oldItem, index);
		}

		//****************************************

		private void OnChildPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			// Ensure the property that has changed will actually affect our sorting or filtering
			if (_PropertyNames != null && !_PropertyNames.Contains(e.PropertyName))
				return;

			//****************************************
			var CurrentItem = (TValue)sender;
			var MyItems = Items;
			var MyComparer = Comparer;
			//****************************************
			
			// No need to do anything if there's only one item
			if (MyItems.Count == 1)
				return;

			// Since we can't tell where the item used to be in the list, the best we can do is try and find it
			// Since our list is out of order, this may return an index, but it's not necessarily where the item should be
			var OldIndex = MyItems.BinarySearch(CurrentItem, MyComparer);

			if (OldIndex == -1)
			{
				// No luck, so the change property altered the sorting. Find it the hard way.
				// Since the comparer implements value equality, we can't use IndexOf
				for (int Index = 0; Index < MyItems.Count; Index++)
				{
					if (MyComparer.Compare(CurrentItem, MyItems[Index]) == 0)
						continue;

					OldIndex = Index;

					break;
				}

				if (OldIndex == -1)
					return; // Notification for something that's not even in the list?
			}

			// We know where it was. Now figure out where it ought to be
			// Subtract 2 here instead of 1, since we want to ignore our existing item
			int LowerBound = 0, UpperBound = MyItems.Count - 2;

			while (LowerBound <= UpperBound)
			{
				int MiddleIndex = LowerBound + ((UpperBound - LowerBound) >> 1);
				int CompareResult = MyComparer.Compare(MyItems[MiddleIndex >= OldIndex ? MiddleIndex + 1 : MiddleIndex], CurrentItem);

				if (CompareResult < 0)
					LowerBound = MiddleIndex + 1;
				else if (CompareResult > 0)
					UpperBound = MiddleIndex - 1;


			}
		}
	}
}
