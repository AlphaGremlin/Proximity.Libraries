/****************************************\
 ObservableSortedSubset.cs
 Created: 2015-10-05
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Proximity.Utility.Events;
//****************************************

namespace Proximity.Utility.Collections
{
	public class ObservableSortedSubset<TValue> : ISet<TValue>, IList<TValue>, IList, INotifyPropertyChanged, INotifyCollectionChanged
	{	//****************************************
		private readonly ObservableSortedSet<TValue> _Parent;

		private readonly TValue _Min, _Max;
		//****************************************

		internal ObservableSortedSubset(ObservableSortedSet<TValue> parent)
		{
			_Parent = parent;
			_Parent.CollectionChanged += WeakDelegateSlim.CreateFor<NotifyCollectionChangedEventHandler>(OnCollectionChanged);
		}

		//****************************************

		private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			throw new NotImplementedException();
		}

	}
}
