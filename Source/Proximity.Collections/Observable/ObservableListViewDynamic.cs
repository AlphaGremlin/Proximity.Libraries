using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
//****************************************

namespace System.Collections.Observable
{
	/// <summary>
	/// Provides an Observable View over a list that performs sorting based on a comparer and dynamically updates based on property changes
	/// </summary>
	/// <typeparam name="T">The type of the values in the list</typeparam>
	public class ObservableListViewDynamic<T> : ObservableListView<T> where T : class, INotifyPropertyChanged
	{ //****************************************
		private readonly ConditionalWeakTable<T, T> _PreviousValues = new ConditionalWeakTable<T, T>();

		private readonly Func<T, T> _CloneMethod;
		//****************************************

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		public ObservableListViewDynamic(IList<T> source) : this(source, null, (IComparer<T>?)null, null, null)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="cloneMethod">A method to clone the object for sorting and filtering purposes</param>
		public ObservableListViewDynamic(IList<T> source, Func<T, T>? cloneMethod) : this(source, cloneMethod, (IComparer<T>?)null, null, null)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="cloneMethod">A method to clone the object for sorting and filtering purposes</param>
		/// <param name="comparison">A delegate to perform the comparison with</param>
		public ObservableListViewDynamic(IList<T> source, Func<T, T>? cloneMethod, Comparison<T>? comparison) : this(source, cloneMethod, comparison, null, null)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="cloneMethod">A method to clone the object for sorting and filtering purposes</param>
		/// <param name="comparer">The comparer to use for sorting</param>
		public ObservableListViewDynamic(IList<T> source, Func<T, T>? cloneMethod, IComparer<T>? comparer) : this(source, cloneMethod, comparer, null, null)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="cloneMethod">A method to clone the object for sorting and filtering purposes</param>
		/// <param name="filter">A filter to apply to the source list</param>
		public ObservableListViewDynamic(IList<T> source, Func<T, T>? cloneMethod, Predicate<T>? filter) : this(source, cloneMethod, (IComparer<T>?)null, filter, null)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="cloneMethod">A method to clone the object for sorting and filtering purposes</param>
		/// <param name="comparison">A delegate to perform the comparison with</param>
		/// <param name="filter">A filter to apply to the source list</param>
		public ObservableListViewDynamic(IList<T> source, Func<T, T>? cloneMethod, Comparison<T>? comparison, Predicate<T>? filter) : this(source, cloneMethod, comparison, filter, null)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="cloneMethod">A method to clone the object for sorting and filtering purposes</param>
		/// <param name="comparer">The comparer to use for sorting</param>
		/// <param name="filter">A filter to apply to the source list</param>
		public ObservableListViewDynamic(IList<T> source, Func<T, T>? cloneMethod, IComparer<T>? comparer, Predicate<T>? filter) : this(source, cloneMethod, comparer, filter, null)
		{
		}
		
		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="cloneMethod">A method to clone the object for sorting and filtering purposes</param>
		/// <param name="comparison">A delegate to perform the comparison with</param>
		/// <param name="filter">A filter to apply to the source list</param>
		/// <param name="maximum">The maximum number of items to show</param>
		public ObservableListViewDynamic(IList<T> source, Func<T, T>? cloneMethod, Comparison<T>? comparison, Predicate<T>? filter, int? maximum) : base(source, comparison, filter, maximum)
		{
			_CloneMethod = cloneMethod ?? CreateCloneMethod();

			foreach (var MyItem in this)
			{
				MyItem.PropertyChanged += OnChildPropertyChanged;

				_PreviousValues.Add(MyItem, _CloneMethod(MyItem));
			}
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="cloneMethod">A method to clone the object for sorting and filtering purposes</param>
		/// <param name="comparer">The comparer to use for sorting</param>
		/// <param name="filter">A filter to apply to the source list</param>
		/// <param name="maximum">The maximum number of items to show</param>
		public ObservableListViewDynamic(IList<T> source, Func<T, T>? cloneMethod, IComparer<T>? comparer, Predicate<T>? filter, int? maximum) : base(source, comparer, filter, maximum)
		{
			_CloneMethod = cloneMethod ?? CreateCloneMethod();

			foreach (var MyItem in this)
			{
				MyItem.PropertyChanged += OnChildPropertyChanged;

				_PreviousValues.Add(MyItem, _CloneMethod(MyItem));
			}
		}

		//****************************************

		/// <inheritdoc />
		[SecuritySafeCritical]
		public override void Dispose()
		{
			base.Dispose();

			foreach (var MyItem in this)
			{
				MyItem.PropertyChanged -= OnChildPropertyChanged;

				_PreviousValues.Remove(MyItem);
			}
		}

		//****************************************

		/// <inheritdoc />
		[SecuritySafeCritical]
		protected override void OnItemsReset(T[] oldItems)
		{
			// Unsubscribe from the old items
			foreach (var MyItem in oldItems)
			{
				MyItem.PropertyChanged -= OnChildPropertyChanged;

				_PreviousValues.Remove(MyItem);
			}

			// Subscribe to the new ones
			foreach (var MyItem in Items)
			{
				MyItem.PropertyChanged += OnChildPropertyChanged;

				_PreviousValues.Add(MyItem, _CloneMethod(MyItem));
			}
		}

		/// <inheritdoc />
		[SecuritySafeCritical]
		protected override void OnItemAdded(T newItem, int index)
		{
			newItem.PropertyChanged += OnChildPropertyChanged;

			_PreviousValues.Add(newItem, _CloneMethod(newItem));
		}

		/// <inheritdoc />
		[SecuritySafeCritical]
		protected override void OnItemRemoved(T oldItem, int index)
		{
			oldItem.PropertyChanged -= OnChildPropertyChanged;

			_PreviousValues.Remove(oldItem);
		}

		/// <inheritdoc />
		[SecuritySafeCritical]
		protected override void OnItemReplaced(T newItem, T oldItem, int index)
		{
			_PreviousValues.Add(newItem, _CloneMethod(newItem));

			oldItem.PropertyChanged -= OnChildPropertyChanged;
			newItem.PropertyChanged += OnChildPropertyChanged;

			_PreviousValues.Remove(oldItem);
		}

		//****************************************

		[SecuritySafeCritical]
		private void OnChildPropertyChanged(object sender, PropertyChangedEventArgs e)
		{	//****************************************
			var CurrentItem = (T)sender;
			//****************************************
			
			// No need to do anything if there's only one item
			if (Count == 1)
				return;

			// What did we look like before?
			if (!_PreviousValues.TryGetValue(CurrentItem, out var PreviousItem))
				throw new InvalidOperationException("Unknown Child Item");

			// Has our sort changed relative to our previous state?
			if (Comparer.Compare(CurrentItem, PreviousItem) == 0)
				return; // No, so we don't even need to update the previous item

			// Sorting has definitely changed
			// Update the previous sort position
			_PreviousValues.Remove(CurrentItem);
			_PreviousValues.Add(CurrentItem, _CloneMethod(CurrentItem));

			//****************************************
			
			// Where were we before?
			var OldIndex = SearchOldPosition(CurrentItem, PreviousItem);

			if (OldIndex == -1)
				throw new InvalidOperationException("Unable to locate Child Item");

			var NewIndex = SearchNewPosition(CurrentItem, PreviousItem);

			// Tell the underlying ListView to resort the item at that position
			ResortItem(CurrentItem, OldIndex, NewIndex, false);

			//VerifyList();
		}

		//****************************************

		private int SearchOldPosition(T currentValue, T previousValue)
		{ //****************************************
			var MyItems = Items;
			var MyComparer = Comparer;
			int LowIndex = 0, HighIndex = MyItems.Count - 1;
			//****************************************

			while (LowIndex <= HighIndex)
			{
				var MiddleIndex = LowIndex + ((HighIndex - LowIndex) >> 1);
				var MiddleValue = MyItems[MiddleIndex];

				// If the reference is found, return its index
				if (object.ReferenceEquals(MiddleValue, currentValue))
					return MiddleIndex;

				// Compare against the previous value, since that's what we're searching for the index of
				var Result = MyComparer.Compare(MiddleValue, previousValue);

				if (Result == 0)
				{
					// We might match exactly because there are duplicates
					LowIndex = MiddleIndex;

					// Check below our current match
					while (LowIndex > 0)
					{
						MiddleValue = MyItems[--LowIndex];

						if (object.ReferenceEquals(MiddleValue, currentValue))
							return LowIndex;

						if (MyComparer.Compare(MiddleValue, previousValue) != 0)
							break;
					}

					HighIndex = MiddleIndex;

					// Check above our current match
					while (HighIndex < MyItems.Count - 1)
					{
						MiddleValue = MyItems[++HighIndex];

						if (object.ReferenceEquals(MiddleValue, currentValue))
							return HighIndex;

						if (MyComparer.Compare(MiddleValue, previousValue) != 0)
							break;
					}

					// No match found
					return -1;
				}

				if (Result < 0)
					LowIndex = MiddleIndex + 1;
				else
					HighIndex = MiddleIndex - 1;
			}

			return -1; // No match found
		}

		private int SearchNewPosition(T currentValue, T previousValue)
		{ //****************************************
			var MyItems = Items;
			var MyComparer = Comparer;
			int LowIndex = 0, HighIndex = MyItems.Count - 1;
			//****************************************

			while (LowIndex <= HighIndex)
			{
				var MiddleIndex = LowIndex + ((HighIndex - LowIndex) >> 1);
				var MiddleValue = MyItems[MiddleIndex];

				// If the reference is found, use the old value for comparisions
				if (object.ReferenceEquals(MiddleValue, currentValue))
					MiddleValue = previousValue;

				// Compare against the current value, since that's what we're searching for the new index of
				var Result = MyComparer.Compare(MiddleValue, currentValue);

				if (Result == 0)
					return MiddleIndex;

				if (Result < 0)
					LowIndex = MiddleIndex + 1;
				else
					HighIndex = MiddleIndex - 1;
			}

			return LowIndex; // No match found
		}

		//****************************************

		private static Func<T, T> CreateCloneMethod()
		{
			if (typeof(ICloneable).IsAssignableFrom(typeof(T)))
				return CloneWithICloneable;

			throw new NotSupportedException("TValue must implement ICloneable");
		}

		private static T CloneWithICloneable(T source) => (T)((ICloneable)source).Clone();
	}
}
