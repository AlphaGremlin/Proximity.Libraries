using System;
using System.Collections.Generic;
using System.Collections.Observable;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using NUnit.Framework;
//****************************************

namespace Proximity.Collections.Tests
{
	[TestFixture]
	public class ObservableListViewTests
	{
		[Test(), MaxTime(2000), Repeat(20)]
		public void Comparer()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, ReverseComparer<int>.Default);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords, ReverseComparer<int>.Default);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(20)]
		public void PrePopulate()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(5)]
		public void PrePopulateFilter([Values(16, 24, 777, 1024)] int count)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(count);
			//****************************************

			for (var Index = 0; Index < count; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterMoreThan512);

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterMoreThan512).ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(5)]
		public void PrePopulateMaximum([Values(256, 512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, maximum);

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(20)]
		public void Populate()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(20)]
		public void PopulateFilter()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(5)]
		public void PopulateMaximum([Values(256, 512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, maximum);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}
		
		[Test(), MaxTime(2000), Repeat(5)]
		public void PopulateMaximumFilter([Values(256, 512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512, maximum);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(20)]
		public void PopulateBeginEnd()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			MyRecords.BeginUpdate();

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			MyRecords.EndUpdate();

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(20)]
		public void PopulateBeginEndFilter()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512);

			MyRecords.BeginUpdate();

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			MyRecords.EndUpdate();

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(5)]
		public void PopulateBeginEndMaximum([Values(256, 512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, maximum);

			MyRecords.BeginUpdate();

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			MyRecords.EndUpdate();

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(20)]
		public void Replace()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}
		
		[Test(), MaxTime(2000), Repeat(20)]
		public void ReplaceFilter()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(20)]
		public void ReplaceFilterSeed([Values(357656222)] int seed)
		{ //****************************************
			var MyRandom = new Random(seed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 1024; Index++)
			{
				var MyInnerRecords = MyRecords.Where(FilterLessThan512).ToArray();

				Array.Sort(MyInnerRecords);

				CollectionAssert.AreEqual(MyInnerRecords, MyView, "Iteration {0}", Index);

				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(5)]
		public void ReplaceMaximum([Values(256, 512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, maximum);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(10)]
		public void ReplaceMaximumFilter([Values(256, 512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512, maximum);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(10)]
		public void ReplaceMaximumFilterSeed()
		{ //****************************************
			var MySeed = 356114168;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512, 256);

			for (var Index = 0; Index < 1024; Index++)
			{
				MyRecords.Add(MyRandom.Next(short.MaxValue));
			}

			for (var Index = 0; Index < 1024; Index++)
			{
				var MyInnerRecords = MyRecords.Where(FilterLessThan512).ToArray();

				Array.Sort(MyInnerRecords);
				if (MyInnerRecords.Length > 256)
					Array.Resize(ref MyInnerRecords, 256);

				CollectionAssert.AreEqual(MyInnerRecords, MyView, "Iteration {0}", Index);

				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > 256)
				Array.Resize(ref MySortedRecords, 256);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(10)]
		public void ReplaceMaximumFilter2([Values(256, 512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterMoreThan512, maximum);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterMoreThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(20)]
		public void Remove()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 512; Index++)
			{
				MyRecords.RemoveAt(MyRandom.Next(MyRecords.Count));
			}

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(20)]
		public void RemoveFilter()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 512; Index++)
			{
				MyRecords.RemoveAt(MyRandom.Next(MyRecords.Count));
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(5)]
		public void RemoveMaximum([Values(256, 512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, maximum);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 512; Index++)
			{
				MyRecords.RemoveAt(MyRandom.Next(MyRecords.Count));
			}

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}
		
		[Test(), MaxTime(2000), Repeat(5)]
		public void RemoveMaximumFilter([Values(256, 512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512, maximum);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 512; Index++)
			{
				MyRecords.RemoveAt(MyRandom.Next(MyRecords.Count));
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(5)]
		public void RemoveMaximumFilter2([Values(256, 512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterMoreThan512, maximum);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 512; Index++)
			{
				MyRecords.RemoveAt(MyRandom.Next(MyRecords.Count));
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterMoreThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		//****************************************

		[Test(), MaxTime(1000)]
		public void EventAdd()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1);
			NotifyCollectionChangedEventArgs MyEventArgs = null;
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			MyView.CollectionChanged += (sender, e) => MyEventArgs = e;

			MyRecords.Add(42);

			//****************************************

			Assert.AreEqual(1, MyView.Count, "Item count does not match");
			Assert.IsNotNull(MyEventArgs, "No Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Add, MyEventArgs.Action);
			Assert.IsNotNull(MyEventArgs.NewItems, "No New Items");
			Assert.AreEqual(0, MyEventArgs.NewStartingIndex, "Starting Index incorrect");
			Assert.AreEqual(1, MyEventArgs.NewItems.Count, "New Items Count incorrect");
			Assert.AreEqual(42, MyEventArgs.NewItems[0], "New Items Value incorrect");
		}

		[Test(), MaxTime(1000)]
		public void EventAddMany()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			var EventCount = 0;
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			MyView.CollectionChanged += (sender, e) => { if (e.Action == NotifyCollectionChangedAction.Add) EventCount++; };

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			Assert.AreEqual(1024, MyView.Count, "Item count does not match");
			Assert.AreEqual(1024, EventCount, "Event Count does not match");
		}

		[Test(), MaxTime(1000)]
		public void EventReplace()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1);
			NotifyCollectionChangedEventArgs MyEventArgs = null;
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			MyRecords.Add(42);

			MyView.CollectionChanged += (sender, e) => MyEventArgs = e;

			MyRecords[0] = 84;

			//****************************************

			Assert.AreEqual(1, MyView.Count, "Item count does not match");
			Assert.IsNotNull(MyEventArgs, "No Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Replace, MyEventArgs.Action);

			Assert.IsNotNull(MyEventArgs.OldItems, "No Old Items");
			Assert.AreEqual(0, MyEventArgs.OldStartingIndex, "Starting Index incorrect");
			Assert.AreEqual(1, MyEventArgs.OldItems.Count, "Old Items Count incorrect");
			Assert.AreEqual(42, MyEventArgs.OldItems[0], "Old Items Value incorrect");

			Assert.IsNotNull(MyEventArgs.NewItems, "No New Items");
			Assert.AreEqual(0, MyEventArgs.NewStartingIndex, "Starting Index incorrect");
			Assert.AreEqual(1, MyEventArgs.NewItems.Count, "New Items Count incorrect");
			Assert.AreEqual(84, MyEventArgs.NewItems[0], "New Items Value incorrect");
		}

		[Test(), MaxTime(1000)]
		public void EventMove()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1);
			NotifyCollectionChangedEventArgs MyFirstEventArgs = null, MySecondEventArgs = null;
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			MyView.CollectionChanged += (sender, e) => { if (MyFirstEventArgs == null) MyFirstEventArgs = e; else MySecondEventArgs = e; };

			var OldValue = MyRecords[512];
			var NewValue = OldValue + 1024;

			MyRecords[512] = NewValue;

			//****************************************

			Assert.AreEqual(1024, MyView.Count, "Item count does not match");
			Assert.IsNotNull(MyFirstEventArgs, "No First Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Remove, MyFirstEventArgs.Action);
			Assert.IsNotNull(MySecondEventArgs, "No Second Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Add, MySecondEventArgs.Action);

			Assert.IsNotNull(MyFirstEventArgs.OldItems, "No Old Items");
			Assert.AreEqual(1, MyFirstEventArgs.OldItems.Count, "Old Items Count incorrect");
			Assert.AreEqual(OldValue, MyFirstEventArgs.OldItems[0], "Old Items Value incorrect");

			Assert.IsNotNull(MySecondEventArgs.NewItems, "No New Items");
			Assert.AreEqual(1, MySecondEventArgs.NewItems.Count, "New Items Count incorrect");
			Assert.AreEqual(NewValue, MySecondEventArgs.NewItems[0], "New Items Value incorrect");
		}

		[Test(), MaxTime(1000)]
		public void EventRemove()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1);
			NotifyCollectionChangedEventArgs MyEventArgs = null;
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			MyRecords.Add(42);

			MyView.CollectionChanged += (sender, e) => MyEventArgs = e;

			MyRecords.RemoveAt(0);

			//****************************************

			Assert.AreEqual(0, MyView.Count, "Item count does not match");
			Assert.IsNotNull(MyEventArgs, "No Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Remove, MyEventArgs.Action);
			Assert.IsNotNull(MyEventArgs.OldItems, "No Old Items");
			Assert.AreEqual(0, MyEventArgs.OldStartingIndex, "Starting Index incorrect");
			Assert.AreEqual(1, MyEventArgs.OldItems.Count, "Old Items Count incorrect");
			Assert.AreEqual(42, MyEventArgs.OldItems[0], "Old Items Value incorrect");
		}

		[Test(), MaxTime(1000)]
		public void EventRemoveMany()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			var EventCount = 0;
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			MyView.CollectionChanged += (sender, e) => { if (e.Action == NotifyCollectionChangedAction.Remove) EventCount++; };

			while (MyRecords.Count > 0)
				MyRecords.RemoveAt(MyRecords.Count - 1);

			//****************************************

			Assert.AreEqual(0, MyView.Count, "Item count does not match");
			Assert.AreEqual(1024, EventCount, "Event Count does not match");
		}

		[Test(), MaxTime(2000), Repeat(5)]
		public void EventMaximum([Values(256, 512, 1024)] int maximum)
		{ //****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, maximum);
			var MyTopView = new ObservableListView<int>(MyView);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyTopView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(5)]
		public void EventMaximumFilter([Values(256, 512, 1024)] int maximum)
		{ //****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512, maximum);
			var MyTopView = new ObservableListView<int>(MyView);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyTopView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(5)]
		public void EventReplaceMaximum([Values(256, 512, 1024)] int maximum)
		{ //****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, maximum);
			var MyTopView = new ObservableListView<int>(MyView);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyTopView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}
		
		[Test(), MaxTime(2000), Repeat(10)]
		public void EventReplaceMaximumFilter([Values(256, 512, 1024)] int maximum)
		{ //****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512, maximum);
			var MyTopView = new ObservableListView<int>(MyView);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyTopView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Sequential()]
		public void EventReplaceMaximumSeed([Values(355591720, 356919648)] int seed, [Values(256, 512)] int maximum)
		{ //****************************************
			var MyRandom = new Random(seed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, maximum);
			var MyTopView = new ObservableListView<int>(MyView);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 1024; Index++)
			{
				CollectionAssert.AreEqual(MyView, MyTopView, "Iteration {0}", Index);

				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyTopView);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(5)]
		public void EventRemoveMaximum([Values(256, 512, 1024)] int maximum)
		{ //****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, maximum);
			var MyTopView = new ObservableListView<int>(MyView);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 512; Index++)
			{
				MyRecords.RemoveAt(MyRandom.Next(MyRecords.Count));
			}

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyTopView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(5)]
		public void EventRemoveMaximumFilter([Values(256, 512, 1024)] int maximum)
		{ //****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512, maximum);
			var MyTopView = new ObservableListView<int>(MyView);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (var Index = 0; Index < 512; Index++)
			{
				MyRecords.RemoveAt(MyRandom.Next(MyRecords.Count));
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyTopView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		//****************************************

		private bool FilterLessThan512(int value)
		{
			return value < 512;
		}

		private bool FilterMoreThan512(int value)
		{
			return value > 512;
		}
	}
}
