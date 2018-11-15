using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proximity.Utility.Tests
{
	public sealed class ObservableMonitor<TValue>
	{ //****************************************
		private readonly List<TValue> _Values = new List<TValue>();

		private readonly EqualityComparer<TValue> _Comparer;
		//****************************************

		public ObservableMonitor(IEnumerable<TValue> values)
		{
			_Comparer = EqualityComparer<TValue>.Default;
			_Values.AddRange(values);

			var Observable = values as INotifyCollectionChanged;

			Observable.CollectionChanged += OnCollectionChanged;
		}

		//****************************************

		public TValue[] ToArray() => _Values.ToArray();

		//****************************************

		private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
			case NotifyCollectionChangedAction.Reset:
				_Values.Clear();
				_Values.AddRange((IEnumerable<TValue>)sender);
				break;

			case NotifyCollectionChangedAction.Add:
				for (int Index = 0; Index < e.NewItems.Count; Index++)
					_Values.Insert(e.NewStartingIndex + Index, (TValue)e.NewItems[Index]);
				break;

			case NotifyCollectionChangedAction.Remove:
				for (int Index = 0; Index < e.OldItems.Count; Index++)
				{
					var OldValue = _Values[e.OldStartingIndex];

					if (!_Comparer.Equals(OldValue, (TValue)e.OldItems[Index]))
						throw new InvalidOperationException("Observable is out of sync");

					_Values.RemoveAt(e.OldStartingIndex);
				}
				break;

			case NotifyCollectionChangedAction.Move:
				// Verify the old items are what we expect
				for (int Index = 0; Index < e.OldItems.Count; Index++)
				{
					var OldValue = _Values[e.OldStartingIndex + Index];

					if (!_Comparer.Equals(OldValue, (TValue)e.OldItems[Index]))
						throw new InvalidOperationException("Observable is out of sync");
				}

				for (int Index = 0; Index < e.OldItems.Count; Index++)
					_Values.RemoveAt(e.OldStartingIndex);

				for (int Index = 0; Index < e.NewItems.Count; Index++)
					_Values.Insert(e.NewStartingIndex + Index, (TValue)e.NewItems[Index]);
				break;

			case NotifyCollectionChangedAction.Replace:
				// Verify the old items are what we expect
				for (int Index = 0; Index < e.OldItems.Count; Index++)
				{
					var OldValue = _Values[e.OldStartingIndex + Index];

					if (!_Comparer.Equals(OldValue, (TValue)e.OldItems[Index]))
						throw new InvalidOperationException("Observable is out of sync");
				}

				for (int Index = 0; Index < e.NewItems.Count; Index++)
					_Values[e.NewStartingIndex + Index] = (TValue)e.NewItems[Index];
				break;
			}
		}
	}
}
