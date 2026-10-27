using System.Collections.Observable;
using System.Collections.Specialized;
using NUnit.Framework;

#pragma warning disable IDE0028 // Simplify collection initialization

namespace Proximity.Collections.Tests;

[TestFixture]
public class ObservableSortedListTests
{
	[Test(), Repeat(2)]
	public void Add()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableSortedList<int, int>();

		var MyDictionary = new Dictionary<int, int>(1024);
		var MySortedList = new SortedList<int, int>(1024);
		//****************************************

		while (MyDictionary.Count < 1024)
		{
			MyDictionary[MyRandom.Next()] = MyRandom.Next();
		}

		foreach (var MyPair in MyDictionary)
		{
			MySortedList.Add(MyPair.Key, MyPair.Value);
			MyRecords.Add(MyPair.Key, MyPair.Value);
		}

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(1024), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyRecords, Is.EqualTo(MySortedList).AsCollection, $"Collections don't match. Bad Seed was {MySeed}");

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void AddCapacity()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableSortedList<int, int>(1024);

		var MyDictionary = new Dictionary<int, int>(1024);
		var MySortedList = new SortedList<int, int>(1024);
		//****************************************

		while (MyDictionary.Count < 1024)
		{
			MyDictionary[MyRandom.Next()] = MyRandom.Next();
		}

		foreach (var MyPair in MyDictionary)
		{
			MySortedList.Add(MyPair.Key, MyPair.Value);
			MyRecords.Add(MyPair.Key, MyPair.Value);
		}

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(1024), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyRecords, Is.EqualTo(MySortedList).AsCollection, $"Collections don't match. Bad Seed was {MySeed}");

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void AddCollide()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableSortedList<CollideStruct, int>();

		var MyDictionary = new Dictionary<CollideStruct, int>(64);
		//****************************************

		for (var Index = 0; Index < 64; Index++)
		{
			CollideStruct Key;
			int Value;

			do
			{
				Key = new CollideStruct(MyRandom.Next());
			} while (MyDictionary.ContainsKey(Key));

			Value = MyRandom.Next();

			MyDictionary.Add(Key, Value);
			MyRecords.Add(Key, Value);
		}

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(64), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyRecords, Is.EquivalentTo(MyDictionary), $"Collections don't match. Bad Seed was {MySeed}");

		Thread.Sleep(1);
	}

	[Test()]
	public void AddExists()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		//****************************************

		MyRecords.Add(10, 84);

		//****************************************

		try
		{
			MyRecords.Add(10, 84);

			Assert.Fail("Add succeeded");
		}
		catch (ArgumentException)
		{
		}
	}

	[Test(), Repeat(2)]
	public void AddRange()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableSortedList<int, int>(1024);

		var MyDictionary = new Dictionary<int, int>(1024);
		var MySortedList = new SortedList<int, int>(1024);
		//****************************************

		while (MyDictionary.Count < 1024)
		{
			MyDictionary[MyRandom.Next()] = MyRandom.Next();
		}

		foreach (var MyPair in MyDictionary)
		{
			MySortedList.Add(MyPair.Key, MyPair.Value);
		}

		MyRecords.AddRange(MySortedList);

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(1024), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyRecords, Is.EqualTo(MySortedList).AsCollection, $"Collections don't match. Bad Seed was {MySeed}");

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void AddRangeCollide()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableSortedList<CollideStruct, int>(64);

		var MyDictionary = new Dictionary<CollideStruct, int>(64);
		//****************************************

		for (var Index = 0; Index < 64; Index++)
		{
			CollideStruct Key;
			int Value;

			do
			{
				Key = new CollideStruct(MyRandom.Next());
			} while (MyDictionary.ContainsKey(Key));

			Value = MyRandom.Next();

			MyDictionary.Add(Key, Value);
		}

		MyRecords.AddRange(MyDictionary);

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(64), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyRecords, Is.EquivalentTo(MyDictionary), $"Collections don't match. Bad Seed was {MySeed}");

		Thread.Sleep(1);
	}

	[Test()]
	public void AddRangeDuplicateInRange()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>(64);
		//****************************************

		try
		{
			MyRecords.AddRange(new[] { new KeyValuePair<int, int>(1, 1), new KeyValuePair<int, int>(2, 2), new KeyValuePair<int, int>(3, 3), new KeyValuePair<int, int>(1, 4) });

			Assert.Fail("Range succeeded");
		}
		catch (ArgumentException)
		{
		}

		//****************************************

		Assert.That(MyRecords.Count, Is.Zero, "Items were added");
	}

	[Test()]
	public void AddRangeDuplicateInDictionary()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>(64);
		//****************************************

		MyRecords[9] = 1;
		MyRecords[10] = 2;
		MyRecords[11] = 3;
		MyRecords[12] = 4;

		try
		{
			MyRecords.AddRange(new[] { new KeyValuePair<int, int>(1, 1), new KeyValuePair<int, int>(2, 2), new KeyValuePair<int, int>(3, 3), new KeyValuePair<int, int>(9, 4) });

			Assert.Fail("Range succeeded");
		}
		catch (ArgumentException)
		{
		}

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(4), "Items were added");
	}

	[Test(), Repeat(2)]
	public void AddRangePrePopulated()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableSortedList<int, int>(1024);

		var MyDictionary = new Dictionary<int, int>(1024);
		var MySortedList = new SortedList<int, int>(1024);

		var MySecondSet = new List<KeyValuePair<int, int>>(512);
		//****************************************

		while (MyDictionary.Count < 512)
		{
			MyDictionary[MyRandom.Next()] = MyRandom.Next();
		}

		foreach (var MyPair in MyDictionary)
		{
			MySortedList.Add(MyPair.Key, MyPair.Value);
			MyRecords.Add(MyPair.Key, MyPair.Value);
		}

		while (MyDictionary.Count < 1024)
		{
			MyDictionary[MyRandom.Next()] = MyRandom.Next();
		}

		MySecondSet.AddRange(MyDictionary.Except(MySortedList));

		foreach (var MyPair in MySecondSet)
			MySortedList.Add(MyPair.Key, MyPair.Value);

		MyRecords.AddRange(MySecondSet);

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(1024), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyRecords, Is.EqualTo(MySortedList).AsCollection, $"Collections don't match. Bad Seed was {MySeed}");

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void AddRangePrePopulatedCollide()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableSortedList<CollideStruct, int>(64);

		var MyDictionary = new Dictionary<CollideStruct, int>(64);
		var MySecondSet = new List<KeyValuePair<CollideStruct, int>>(32);
		//****************************************

		for (var Index = 0; Index < 32; Index++)
		{
			CollideStruct Key;
			int Value;

			do
			{
				Key = new CollideStruct(MyRandom.Next());
			} while (MyDictionary.ContainsKey(Key));

			Value = MyRandom.Next();

			MyDictionary.Add(Key, Value);
			MyRecords.Add(Key, Value);
		}

		for (var Index = 0; Index < 32; Index++)
		{
			CollideStruct Key;
			int Value;

			do
			{
				Key = new CollideStruct(MyRandom.Next());
			} while (MyDictionary.ContainsKey(Key));

			Value = MyRandom.Next();

			MyDictionary.Add(Key, Value);
			MySecondSet.Add(new KeyValuePair<CollideStruct, int>(Key, Value));
		}

		MyRecords.AddRange(MySecondSet);

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(64), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyRecords, Is.EquivalentTo(MyDictionary), $"Collections don't match. Bad Seed was {MySeed}");

		Thread.Sleep(1);
	}

	[Test()]
	public void GetIndex()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(((IList<KeyValuePair<int, int>>)MyRecords)[0], Is.EqualTo(new KeyValuePair<int, int>(10, 42)));
	}

	[Test()]
	public void GetIndexOutOfRange()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		try
		{
			var Pair = ((IList<KeyValuePair<int, int>>)MyRecords)[1];

			Assert.Fail("Key found");
		}
		catch (ArgumentOutOfRangeException)
		{
		}

		try
		{
			var Pair = ((IList<KeyValuePair<int, int>>)MyRecords)[-1];

			Assert.Fail("Key found");
		}
		catch (ArgumentOutOfRangeException)
		{
		}
	}

	[Test()]
	public void GetKey()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords[10], Is.EqualTo(42));
	}

	[Test()]
	public void GetKeyMissing()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		try
		{
			var Pair = MyRecords[12];

			Assert.Fail("Key found");
		}
		catch (KeyNotFoundException)
		{
		}
	}

	[Test()]
	public void IndexOf()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOf(new KeyValuePair<int, int>(10, 42)), Is.Zero);
	}

	[Test()]
	public void IndexOfKey()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOfKey(10), Is.Zero);
	}

	[Test()]
	public void IndexOfKeyCollideMissing()
	{	//****************************************
		var MyRecords = new ObservableSortedList<CollideStruct, int>();
		//****************************************

		MyRecords[new CollideStruct(10)] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOfKey(new CollideStruct(11)), Is.EqualTo(-1));
	}

	[Test()]
	public void IndexOfKeyCollideMissing2()
	{	//****************************************
		var MyRecords = new ObservableSortedList<CollideStruct, int>();
		//****************************************

		MyRecords[new CollideStruct(10)] = 42;
		MyRecords[new CollideStruct(int.MaxValue)] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOfKey(new CollideStruct(11)), Is.EqualTo(-1));
	}

	[Test()]
	public void IndexOfKeyMissing()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOfKey(11), Is.EqualTo(-1));
	}

	[Test()]
	public void IndexOfMissingKey()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOf(new KeyValuePair<int, int>(11, 12)), Is.EqualTo(-1));
	}

	[Test()]
	public void IndexOfMissingValue()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOf(new KeyValuePair<int, int>(10, 30)), Is.EqualTo(-1));
	}

	[Test(), Repeat(2)]
	public void PrePopulate()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);

		var MyDictionary = new Dictionary<int, int>(1024);
		var MySortedList = new SortedList<int, int>(1024);
		//****************************************

		while (MyDictionary.Count < 1024)
		{
			MyDictionary[MyRandom.Next()] = MyRandom.Next();
		}

		foreach (var MyPair in MyDictionary)
		{
			MySortedList.Add(MyPair.Key, MyPair.Value);
		}

		var MyRecords = new ObservableSortedList<int, int>(MyDictionary);

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(1024), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MySortedList.SequenceEquivalent(MyRecords), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void Remove()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);

		var MyDictionary = new SortedList<int, int>(1024);
		//****************************************

		for (var Index = 0; Index < 1024; Index++)
		{
			int Key, Value;

			do
			{
				Key = MyRandom.Next();
			} while (MyDictionary.ContainsKey(Key));

			Value = MyRandom.Next();

			MyDictionary.Add(Key, Value);
		}

		var MyRecords = new ObservableSortedList<int, int>(MyDictionary);

		//****************************************

		for (var Index = 0; Index < 512; Index++)
		{
			var InnerIndex = MyRandom.Next(MyRecords.Count);

			var Key = MyRecords.Keys[InnerIndex];

			Assert.That(MyRecords.Remove(Key), Is.True);
			Assert.That(MyDictionary.Remove(Key), Is.True);
		}

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(512), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyDictionary.SequenceEquivalent(MyRecords), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		foreach (var MyPair in MyDictionary)
		{
			Assert.That(MyRecords.TryGetValue(MyPair.Key, out var Value), Is.True);
			Assert.That(Value, Is.EqualTo(MyPair.Value));
		}

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void RemoveAt()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);

		var MyDictionary = new SortedList<int, int>(1024);
		//****************************************

		for (var Index = 0; Index < 1024; Index++)
		{
			int Key, Value;

			do
			{
				Key = MyRandom.Next();
			} while (MyDictionary.ContainsKey(Key));

			Value = MyRandom.Next();

			MyDictionary.Add(Key, Value);
		}

		var MyRecords = new ObservableSortedList<int, int>(MyDictionary);

		//****************************************

		for (var Index = 0; Index < 512; Index++)
		{
			var InnerIndex = MyRandom.Next(MyRecords.Count);

			var Key = MyRecords.Keys[InnerIndex];

			MyRecords.RemoveAt(InnerIndex);
			Assert.That(MyDictionary.Remove(Key), Is.True);
		}

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(512), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyDictionary.SequenceEquivalent(MyRecords), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		foreach (var MyPair in MyDictionary)
		{
			Assert.That(MyRecords.TryGetValue(MyPair.Key, out var Value), Is.True);
			Assert.That(Value, Is.EqualTo(MyPair.Value));
		}

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void RemoveAll()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);

		var MyDictionary = new SortedList<int, int>(1024);
		//****************************************

		for (var Index = 0; Index < 1024; Index++)
		{
			int Key, Value;

			do
			{
				Key = MyRandom.Next();
			} while (MyDictionary.ContainsKey(Key));

			Value = MyRandom.Next();

			MyDictionary.Add(Key, Value);
		}

		var MyRecords = new ObservableSortedList<int, int>(MyDictionary);

		//****************************************

		MyRecords.RemoveAll((pair) => { if (MyRandom.Next() > int.MaxValue / 2) return MyDictionary.Remove(pair.Key); return false; });

		//****************************************

		Assert.That(MyDictionary.SequenceEquivalent(MyRecords), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		foreach (var MyPair in MyDictionary)
		{
			Assert.That(MyRecords.TryGetValue(MyPair.Key, out var Value), Is.True);
			Assert.That(Value, Is.EqualTo(MyPair.Value));
		}

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void RemoveRange()
	{ //****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);

		var MyDictionary = new SortedList<int, int>(1024);
		//****************************************

		for (var Index = 0; Index < 1024; Index++)
		{
			int Key, Value;

			do
			{
				Key = MyRandom.Next();
			} while (MyDictionary.ContainsKey(Key));

			Value = MyRandom.Next();

			MyDictionary.Add(Key, Value);
		}

		var MyRecords = new ObservableSortedList<int, int>(MyDictionary);

		//****************************************

		foreach (var MyResult in MyRecords.Skip(256).Take(256))
			MyDictionary.Remove(MyResult.Key);

		MyRecords.RemoveRange(256, 256);

		//****************************************

		Assert.That(MyDictionary.SequenceEquivalent(MyRecords), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		foreach (var MyPair in MyDictionary)
		{
			Assert.That(MyRecords.TryGetValue(MyPair.Key, out var Value), Is.True);
			Assert.That(Value, Is.EqualTo(MyPair.Value));
		}

		Thread.Sleep(1);
	}

	[Test()]
	public void Replace()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		//****************************************

		MyRecords.Add(9, 1);
		MyRecords.Add(12, 2);
		MyRecords.Add(10, 3);
		MyRecords.Add(11, 4);
		MyRecords[10] = 84;

		//****************************************

		Assert.That(MyRecords[10], Is.EqualTo(84));
	}

	[Test()]
	public void ReplaceCollide()
	{	//****************************************
		var MyRecords = new ObservableSortedList<CollideStruct, int>();
		var MyKey = new CollideStruct(10);
		//****************************************

		MyRecords.Add(new CollideStruct(9), 1);
		MyRecords.Add(new CollideStruct(12), 2);
		MyRecords.Add(new CollideStruct(10), 3);
		MyRecords.Add(new CollideStruct(11), 4);

		MyRecords[MyKey] = 84;

		//****************************************

		Assert.That(MyRecords[MyKey], Is.EqualTo(84));
	}

	[Test()]
	public void SetKey()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		//****************************************

		MyRecords[9] = 1;
		MyRecords[12] = 2;
		MyRecords[10] = 3;
		MyRecords[11] = 4;

		MyRecords[10] = 84;

		//****************************************

		Assert.That(MyRecords[10], Is.EqualTo(84));
	}

	[Test()]
	public void SetKeyCollide()
	{	//****************************************
		var MyRecords = new ObservableSortedList<CollideStruct, int>();
		var MyKey = new CollideStruct(10);
		//****************************************

		MyRecords[new CollideStruct(9)] = 1;
		MyRecords[new CollideStruct(12)] = 2;
		MyRecords[new CollideStruct(10)] = 3;
		MyRecords[new CollideStruct(11)] = 4;

		MyRecords[MyKey] = 84;

		//****************************************

		Assert.That(MyRecords[MyKey], Is.EqualTo(84));
	}

	//****************************************

	[Test(), MaxTime(1000)]
	public void EventAdd()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableSortedList<int, int>(1);
		NotifyCollectionChangedEventArgs? MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
		//****************************************

		MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
		MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
		MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

		var Pair = new KeyValuePair<int, int>(MyRandom.Next(), MyRandom.Next());

		MyRecords.Add(Pair);

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(1), "Item count does not match");

		Assert.That(MyEventArgs, Is.Not.Null, "No Event Raised");
		Assert.That(MyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
		Assert.That(MyEventArgs.NewItems, Is.Not.Null, "No New Items");
		Assert.That(MyEventArgs.NewStartingIndex, Is.Zero, "Starting Index incorrect");
		Assert.That(MyEventArgs.NewItems, Has.Count.EqualTo(1), "New Items Count incorrect");
		Assert.That(MyEventArgs.NewItems[0], Is.EqualTo(Pair), "New Items Value incorrect");

		Assert.That(MyKeyEventArgs, Is.Not.Null, "No Key Event Raised");
		Assert.That(MyKeyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
		Assert.That(MyKeyEventArgs.NewItems, Is.Not.Null, "No Key New Items");
		Assert.That(MyKeyEventArgs.NewStartingIndex, Is.Zero, "Key Starting Index incorrect");
		Assert.That(MyKeyEventArgs.NewItems, Has.Count.EqualTo(1), "New Key Items Count incorrect");
		Assert.That(MyKeyEventArgs.NewItems[0], Is.EqualTo(Pair.Key), "New Key Items Value incorrect");

		Assert.That(MyValueEventArgs, Is.Not.Null, "No Value Event Raised");
		Assert.That(MyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
		Assert.That(MyValueEventArgs.NewItems, Is.Not.Null, "No Value New Items");
		Assert.That(MyValueEventArgs.NewStartingIndex, Is.Zero, "Value Starting Index incorrect");
		Assert.That(MyValueEventArgs.NewItems, Has.Count.EqualTo(1), "New Value Items Count incorrect");
		Assert.That(MyValueEventArgs.NewItems[0], Is.EqualTo(Pair.Value), "New Value Items Value incorrect");
	}

	[Test(), MaxTime(1000)]
	public void EventAddUpdate()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableSortedList<int, int>(1);
		NotifyCollectionChangedEventArgs? MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
		//****************************************

		MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
		MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
		MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

		var Pair = new KeyValuePair<int, int>(MyRandom.Next(), MyRandom.Next());

		MyRecords.BeginUpdate();

		MyRecords.Add(Pair);

		//****************************************

		Assert.That(MyEventArgs, Is.Null, "Event Raised");
		Assert.That(MyKeyEventArgs, Is.Null, "Event Raised");
		Assert.That(MyValueEventArgs, Is.Null, "Event Raised");

		//****************************************

		MyRecords.EndUpdate();

		Assert.That(MyRecords, Has.Count.EqualTo(1), "Item count does not match");

		Assert.That(MyEventArgs, Is.Not.Null, "No Event Raised");
		Assert.That(MyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Reset));

		Assert.That(MyKeyEventArgs, Is.Not.Null, "No Key Event Raised");
		Assert.That(MyKeyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Reset));

		Assert.That(MyValueEventArgs, Is.Not.Null, "No Value Event Raised");
		Assert.That(MyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Reset));
	}

	[Test(), MaxTime(1000)]
	public void EventAddMany()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableSortedList<int, int>(1024);
		var EventCount = 0;
		//****************************************

		MyRecords.CollectionChanged += (sender, e) => { if (e.Action == NotifyCollectionChangedAction.Add) EventCount++; };

		for (var Index = 0; Index < 1024; Index++)
			MyRecords.Add(MyRandom.Next(), MyRandom.Next());

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(1024), "Item count does not match");
		Assert.That(EventCount, Is.EqualTo(1024), "Event Count does not match");
	}

	[Test()]
	public void EventAddRangeEmpty()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		NotifyCollectionChangedEventArgs? MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
		//****************************************

		MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
		MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
		MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

		MyRecords.AddRange(Enumerable.Empty<KeyValuePair<int, int>>());

		//****************************************

		Assert.That(MyEventArgs, Is.Null, "Event Raised");
		Assert.That(MyKeyEventArgs, Is.Null, "Event Raised");
		Assert.That(MyValueEventArgs, Is.Null, "Event Raised");
	}

	[Test()]
	public void EventClear()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		NotifyCollectionChangedEventArgs? MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
		//****************************************

		MyRecords[10] = 42;

		MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
		MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
		MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

		MyRecords.Clear();

		//****************************************

		Assert.That(MyRecords.Count, Is.Zero, "Items not cleared");

		Assert.That(MyEventArgs, Is.Not.Null, "No Event Raised");
		Assert.That(MyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Reset));

		Assert.That(MyKeyEventArgs, Is.Not.Null, "No Key Event Raised");
		Assert.That(MyKeyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Reset));

		Assert.That(MyValueEventArgs, Is.Not.Null, "No Value Event Raised");
		Assert.That(MyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Reset));
	}

	[Test()]
	public void EventClearEmpty()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		NotifyCollectionChangedEventArgs? MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
		//****************************************

		MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
		MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
		MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

		MyRecords.Clear();

		//****************************************

		Assert.That(MyRecords.Count, Is.Zero, "Items not cleared");

		Assert.That(MyEventArgs, Is.Null, "Event Raised");
		Assert.That(MyKeyEventArgs, Is.Null, "Event Raised");
		Assert.That(MyValueEventArgs, Is.Null, "Event Raised");
	}

	[Test(), MaxTime(1000)]
	public void EventReplace()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableSortedList<int, int>(1);
		NotifyCollectionChangedEventArgs? MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
		//****************************************

		MyRecords.Add(42, 10);

		MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
		MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
		MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

		MyRecords[42] = 84;

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(1), "Item count does not match");
		Assert.That(MyEventArgs, Is.Not.Null, "No Event Raised");
		Assert.That(MyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Replace));

		Assert.That(MyEventArgs.OldItems, Is.Not.Null, "No Old Items");
		Assert.That(MyEventArgs.OldStartingIndex, Is.Zero, "Starting Index incorrect");
		Assert.That(MyEventArgs.OldItems, Has.Count.EqualTo(1), "Old Items Count incorrect");
		Assert.That(MyEventArgs.OldItems[0], Is.EqualTo(new KeyValuePair<int, int>(42, 10)), "Old Items Value incorrect");

		Assert.That(MyEventArgs.NewItems, Is.Not.Null, "No New Items");
		Assert.That(MyEventArgs.NewStartingIndex, Is.Zero, "Starting Index incorrect");
		Assert.That(MyEventArgs.NewItems, Has.Count.EqualTo(1), "New Items Count incorrect");
		Assert.That(MyEventArgs.NewItems[0], Is.EqualTo(new KeyValuePair<int, int>(42, 84)), "New Items Value incorrect");
	}

	[Test()]
	public void EventReplaceUnchanged()
	{	//****************************************
		var MyRecords = new ObservableSortedList<int, int>();
		NotifyCollectionChangedEventArgs? MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
		//****************************************

		MyRecords[9] = 1;
		MyRecords[12] = 2;
		MyRecords[10] = 3;
		MyRecords[11] = 4;

		MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
		MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
		MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

		MyRecords[10] = 3;

		//****************************************

		Assert.That(MyRecords[10], Is.EqualTo(3));

		Assert.That(MyEventArgs, Is.Null, "Event Raised");
		Assert.That(MyKeyEventArgs, Is.Null, "Event Raised");
		Assert.That(MyValueEventArgs, Is.Null, "Event Raised");
	}

	[Test(), MaxTime(1000)]
	public void EventRemove()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableSortedList<int, int>(1);
		NotifyCollectionChangedEventArgs? MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
		//****************************************

		MyRecords.Add(42, 84);

		MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
		MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
		MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

		MyRecords.RemoveAt(0);

		//****************************************

		Assert.That(MyRecords.Count, Is.Zero, "Item count does not match");
		Assert.That(MyEventArgs, Is.Not.Null, "No Event Raised");
		Assert.That(MyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
		Assert.That(MyEventArgs.OldItems, Is.Not.Null, "No Old Items");
		Assert.That(MyEventArgs.OldStartingIndex, Is.Zero, "Starting Index incorrect");
		Assert.That(MyEventArgs.OldItems, Has.Count.EqualTo(1), "Old Items Count incorrect");
		Assert.That(MyEventArgs.OldItems[0], Is.EqualTo(new KeyValuePair<int, int>(42, 84)), "Old Items Value incorrect");

		Assert.That(MyKeyEventArgs, Is.Not.Null, "No Key Event Raised");
		Assert.That(MyKeyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
		Assert.That(MyKeyEventArgs.OldItems, Is.Not.Null, "No Key Old Items");
		Assert.That(MyKeyEventArgs.OldStartingIndex, Is.Zero, "Key Starting Index incorrect");
		Assert.That(MyKeyEventArgs.OldItems, Has.Count.EqualTo(1), "Old Key Items Count incorrect");
		Assert.That(MyKeyEventArgs.OldItems[0], Is.EqualTo(42), "Old Key Items Value incorrect");

		Assert.That(MyValueEventArgs, Is.Not.Null, "No Value Event Raised");
		Assert.That(MyEventArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
		Assert.That(MyValueEventArgs.OldItems, Is.Not.Null, "No Value Old Items");
		Assert.That(MyValueEventArgs.OldStartingIndex, Is.Zero, "Value Starting Index incorrect");
		Assert.That(MyValueEventArgs.OldItems, Has.Count.EqualTo(1), "Old Value Items Count incorrect");
		Assert.That(MyValueEventArgs.OldItems[0], Is.EqualTo(84), "Old Value Items Value incorrect");
	}

	[Test(), MaxTime(1000)]
	public void EventRemoveMany()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableSortedList<int, int>(1024);
		var EventCount = 0;
		//****************************************

		for (var Index = 0; Index < 1024; Index++)
			MyRecords.Add(MyRandom.Next(), MyRandom.Next());

		MyRecords.CollectionChanged += (sender, e) => { if (e.Action == NotifyCollectionChangedAction.Remove) EventCount++; };

		while (MyRecords.Count > 0)
			MyRecords.RemoveAt(MyRecords.Count - 1);

		//****************************************

		Assert.That(MyRecords.Count, Is.Zero, "Item count does not match");
		Assert.That(EventCount, Is.EqualTo(1024), "Event Count does not match");
	}

	//****************************************

	private struct CollideStruct : IComparable<CollideStruct>
	{
		public CollideStruct(int value)
		{
			Value = value;
		}

		//****************************************

		public int CompareTo(CollideStruct other)
		{
			return Value - other.Value;
		}

		//****************************************

		public int Value { get; }
	}
}
