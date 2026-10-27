using System.Collections.Observable;
using System.Collections.Specialized;
using NUnit.Framework;
//****************************************

namespace Proximity.Collections.Tests;

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

			Assert.That(MyView, Is.EqualTo(MyInnerRecords).AsCollection, $"Iteration {Index}");

			MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
		}

		//****************************************

		var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

		Array.Sort(MySortedRecords);

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection);

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

			Assert.That(MyView, Is.EqualTo(MyInnerRecords).AsCollection, $"Iteration {Index}");

			MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
		}

		//****************************************

		var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

		Array.Sort(MySortedRecords);
		if (MySortedRecords.Length > 256)
			Array.Resize(ref MySortedRecords, 256);

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

		Thread.Sleep(1);
	}

	//****************************************

	[Test(), MaxTime(1000)]
	public void EventAdd()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableList<int>(1);
		NotifyCollectionChangedEventArgs? MyEventArgs = null;
		//****************************************

		var MyView = new ObservableListView<int>(MyRecords);

		MyView.CollectionChanged += (sender, e) => MyEventArgs = e;

		MyRecords.Add(42);

		//****************************************

		Assert.That(MyView, Has.Count.EqualTo(1), "Item count does not match");
		Assert.That(MyEventArgs, Is.Not.Null, "No Event Raised");
		Assert.That(MyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
		Assert.That(MyEventArgs.NewItems, Is.Not.Null, "No New Items");
		Assert.That(MyEventArgs.NewStartingIndex, Is.Zero, "Starting Index incorrect");
		Assert.That(MyEventArgs.NewItems, Has.Count.EqualTo(1), "New Items Count incorrect");
		Assert.That(MyEventArgs.NewItems[0], Is.EqualTo(42), "New Items Value incorrect");
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

		Assert.That(MyView, Has.Count.EqualTo(1024), "Item count does not match");
		Assert.That(EventCount, Is.EqualTo(1024), "Event Count does not match");
	}

	[Test(), MaxTime(1000)]
	public void EventReplace()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableList<int>(1);
		NotifyCollectionChangedEventArgs? MyEventArgs = null;
		//****************************************

		var MyView = new ObservableListView<int>(MyRecords);

		MyRecords.Add(42);

		MyView.CollectionChanged += (sender, e) => MyEventArgs = e;

		MyRecords[0] = 84;

		//****************************************

		Assert.That(MyView, Has.Count.EqualTo(1), "Item count does not match");
		Assert.That(MyEventArgs, Is.Not.Null, "No Event Raised");
		Assert.That(MyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Replace));

		Assert.That(MyEventArgs.OldItems, Is.Not.Null, "No Old Items");
		Assert.That(MyEventArgs.OldStartingIndex, Is.Zero, "Starting Index incorrect");
		Assert.That(MyEventArgs.OldItems, Has.Count.EqualTo(1), "Old Items Count incorrect");
		Assert.That(MyEventArgs.OldItems[0], Is.EqualTo(42), "Old Items Value incorrect");

		Assert.That(MyEventArgs.NewItems, Is.Not.Null, "No New Items");
		Assert.That(MyEventArgs.NewStartingIndex, Is.Zero, "Starting Index incorrect");
		Assert.That(MyEventArgs.NewItems, Has.Count.EqualTo(1), "New Items Count incorrect");
		Assert.That(MyEventArgs.NewItems[0], Is.EqualTo(84), "New Items Value incorrect");
	}

	[Test(), MaxTime(1000)]
	public void EventMove()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableList<int>(1);
		NotifyCollectionChangedEventArgs? MyFirstEventArgs = null, MySecondEventArgs = null;
		//****************************************

		var MyView = new ObservableListView<int>(MyRecords);

		for (var Index = 0; Index < 1024; Index++)
			MyRecords.Add(MyRandom.Next(short.MaxValue));

		MyView.CollectionChanged += (sender, e) => { if (MyFirstEventArgs == null) MyFirstEventArgs = e; else MySecondEventArgs = e; };

		var OldValue = MyRecords[512];
		var NewValue = OldValue + 1024;

		MyRecords[512] = NewValue;

		//****************************************

		Assert.That(MyView, Has.Count.EqualTo(1024), "Item count does not match");
		Assert.That(MyFirstEventArgs, Is.Not.Null, "No First Event Raised");
		Assert.That(MyFirstEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
		Assert.That(MySecondEventArgs, Is.Not.Null, "No Second Event Raised");
		Assert.That(MySecondEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));

		Assert.That(MyFirstEventArgs.OldItems, Is.Not.Null, "No Old Items");
		Assert.That(MyFirstEventArgs.OldItems, Has.Count.EqualTo(1), "Old Items Count incorrect");
		Assert.That(MyFirstEventArgs.OldItems[0], Is.EqualTo(OldValue), "Old Items Value incorrect");

		Assert.That(MySecondEventArgs.NewItems, Is.Not.Null, "No New Items");
		Assert.That(MySecondEventArgs.NewItems, Has.Count.EqualTo(1), "New Items Count incorrect");
		Assert.That(MySecondEventArgs.NewItems[0], Is.EqualTo(NewValue), "New Items Value incorrect");
	}

	[Test(), MaxTime(1000)]
	public void EventRemove()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableList<int>(1);
		NotifyCollectionChangedEventArgs? MyEventArgs = null;
		//****************************************

		var MyView = new ObservableListView<int>(MyRecords);

		MyRecords.Add(42);

		MyView.CollectionChanged += (sender, e) => MyEventArgs = e;

		MyRecords.RemoveAt(0);

		//****************************************

		Assert.That(MyView.Count, Is.Zero, "Item count does not match");
		Assert.That(MyEventArgs, Is.Not.Null, "No Event Raised");
		Assert.That(MyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
		Assert.That(MyEventArgs.OldItems, Is.Not.Null, "No Old Items");
		Assert.That(MyEventArgs.OldStartingIndex, Is.Zero, "Starting Index incorrect");
		Assert.That(MyEventArgs.OldItems, Has.Count.EqualTo(1), "Old Items Count incorrect");
		Assert.That(MyEventArgs.OldItems[0], Is.EqualTo(42), "Old Items Value incorrect");
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

		Assert.That(MyView.Count, Is.Zero, "Item count does not match");
		Assert.That(EventCount, Is.EqualTo(1024), "Event Count does not match");
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

		Assert.That(MyTopView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyTopView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyTopView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyTopView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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
			Assert.That(MyTopView, Is.EqualTo(MyView).AsCollection, $"Iteration {Index}");

			MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
		}

		//****************************************

		var MySortedRecords = MyRecords.ToArray();

		Array.Sort(MySortedRecords);
		if (MySortedRecords.Length > maximum)
			Array.Resize(ref MySortedRecords, maximum);

		Assert.That(MyTopView, Is.EqualTo(MySortedRecords).AsCollection);

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

		Assert.That(MyTopView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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

		Assert.That(MyTopView, Is.EqualTo(MySortedRecords).AsCollection, $"Bad Seed was {MySeed}");

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
