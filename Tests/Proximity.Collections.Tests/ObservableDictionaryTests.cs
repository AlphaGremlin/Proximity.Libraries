using System.Collections.Observable;
using System.Collections.Specialized;
using NUnit.Framework;
//****************************************

#pragma warning disable IDE0028 // Simplify collection initialization

namespace Proximity.Collections.Tests;

[TestFixture]
public class ObservableDictionaryTests
{
	[Test()]//, Repeat(2)]
	public void Add()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<int, int>();

		var MyDictionary = new Dictionary<int, int>(1024);
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
			MyRecords.Add(Key, Value);
		}

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(1024), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyDictionary.SequenceEquivalent(MyRecords), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		foreach (var MyPair in MyDictionary)
		{
			Assert.That(MyRecords.TryGetValue(MyPair.Key, out var Value), Is.True);
			Assert.That(Value, Is.EqualTo(MyPair.Value));
		}

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void AddCapacity()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<int, int>(1024);

		var MyDictionary = new Dictionary<int, int>(1024);
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
			MyRecords.Add(Key, Value);
		}

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(1024), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyDictionary.SequenceEquivalent(MyRecords), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		foreach (var MyPair in MyDictionary)
		{
			Assert.That(MyRecords.TryGetValue(MyPair.Key, out var Value), Is.True);
			Assert.That(Value, Is.EqualTo(MyPair.Value));
		}

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void AddCollide()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<Collider, int>();

		var MyDictionary = new Dictionary<Collider, int>(64);
		//****************************************

		for (var Index = 0; Index < 64; Index++)
		{
			Collider Key;
			int Value;

			do
			{
				Key = new Collider(MyRandom.Next());
			} while (MyDictionary.ContainsKey(Key));

			Value = MyRandom.Next();

			MyDictionary.Add(Key, Value);
			MyRecords.Add(Key, Value);
		}

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(64), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyDictionary.SequenceEquivalent(MyRecords), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		foreach (var MyPair in MyDictionary)
		{
			Assert.That(MyRecords.TryGetValue(MyPair.Key, out var Value), Is.True);
			Assert.That(Value, Is.EqualTo(MyPair.Value));
		}

		Thread.Sleep(1);
	}

	[Test()]
	public void AddExists()
	{	//****************************************
		var MyRecords = new ObservableDictionary<int, int>();
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
		var MyRecords = new ObservableDictionary<int, int>(1024);

		var MyDictionary = new Dictionary<int, int>(1024);
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

		MyRecords.AddRange(MyDictionary);

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(1024), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyDictionary.SequenceEquivalent(MyRecords), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		foreach (var MyPair in MyDictionary)
		{
			Assert.That(MyRecords.TryGetValue(MyPair.Key, out var Value), Is.True);
			Assert.That(Value, Is.EqualTo(MyPair.Value));
		}

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void AddRangeCollide()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<Collider, int>(64);

		var MyDictionary = new Dictionary<Collider, int>(64);
		//****************************************

		for (var Index = 0; Index < 64; Index++)
		{
			Collider Key;
			int Value;

			do
			{
				Key = new Collider(MyRandom.Next());
			} while (MyDictionary.ContainsKey(Key));

			Value = MyRandom.Next();

			MyDictionary.Add(Key, Value);
		}

		MyRecords.AddRange(MyDictionary);

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(64), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyDictionary.SequenceEquivalent(MyRecords), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		foreach (var MyPair in MyDictionary)
		{
			Assert.That(MyRecords.TryGetValue(MyPair.Key, out var Value), Is.True);
			Assert.That(Value, Is.EqualTo(MyPair.Value));
		}

		Thread.Sleep(1);
	}

	[Test()]
	public void AddRangeDuplicateInRange()
	{	//****************************************
		var MyRecords = new ObservableDictionary<int, int>(64);
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
		var MyRecords = new ObservableDictionary<int, int>(64);
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
		var MyRecords = new ObservableDictionary<int, int>(1024);

		var MyDictionary = new Dictionary<int, int>(1024);
		var MySecondSet = new List<KeyValuePair<int, int>>(512);
		//****************************************

		for (var Index = 0; Index < 512; Index++)
		{
			int Key, Value;

			do
			{
				Key = MyRandom.Next();
			} while (MyDictionary.ContainsKey(Key));

			Value = MyRandom.Next();

			MyDictionary.Add(Key, Value);
			MyRecords.Add(Key, Value);
		}

		for (var Index = 0; Index < 512; Index++)
		{
			int Key, Value;

			do
			{
				Key = MyRandom.Next();
			} while (MyDictionary.ContainsKey(Key));

			Value = MyRandom.Next();

			MyDictionary.Add(Key, Value);
			MySecondSet.Add(new KeyValuePair<int, int>(Key, Value));
		}

		MyRecords.AddRange(MySecondSet);

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(1024), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyDictionary.SequenceEquivalent(MyRecords), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		foreach (var MyPair in MyDictionary)
		{
			Assert.That(MyRecords.TryGetValue(MyPair.Key, out var Value), Is.True);
			Assert.That(Value, Is.EqualTo(MyPair.Value));
		}

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void AddRangePrePopulatedCollide()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<Collider, int>(64);

		var MyDictionary = new Dictionary<Collider, int>(64);
		var MySecondSet = new List<KeyValuePair<Collider, int>>(32);
		//****************************************

		for (var Index = 0; Index < 32; Index++)
		{
			Collider Key;
			int Value;

			do
			{
				Key = new Collider(MyRandom.Next());
			} while (MyDictionary.ContainsKey(Key));

			Value = MyRandom.Next();

			MyDictionary.Add(Key, Value);
			MyRecords.Add(Key, Value);
		}

		for (var Index = 0; Index < 32; Index++)
		{
			Collider Key;
			int Value;

			do
			{
				Key = new Collider(MyRandom.Next());
			} while (MyDictionary.ContainsKey(Key));

			Value = MyRandom.Next();

			MyDictionary.Add(Key, Value);
			MySecondSet.Add(new KeyValuePair<Collider, int>(Key, Value));
		}

		MyRecords.AddRange(MySecondSet);

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(64), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyDictionary.SequenceEquivalent(MyRecords), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		foreach (var MyPair in MyDictionary)
		{
			Assert.That(MyRecords.TryGetValue(MyPair.Key, out var Value), Is.True);
			Assert.That(Value, Is.EqualTo(MyPair.Value));
		}

		Thread.Sleep(1);
	}

	[Test()]//, Repeat(2)]
	public void AddRemove()
	{ //****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<int, int>();
		//****************************************

		for (var Index = 0; Index < 16; Index++)
			MyRecords.Add(MyRandom.Next(), MyRandom.Next());

		for (var Index = 0; Index < 1024; Index++)
		{
			if (Index % 10 >= 5 && MyRecords.Count > 0)
			{
				var Key = ((IList<KeyValuePair<int, int>>)MyRecords)[MyRandom.Next(0, MyRecords.Count)].Key;

				MyRecords.Remove(Key);
			}
			else
			{
				MyRecords[MyRandom.Next()] = MyRandom.Next();
			}
		}
	}

	[Test()]//, Repeat(2)]
	public void AddRemoveCollide()
	{ //****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<Collider, int>();
		//****************************************

		for (var Index = 0; Index < 16; Index++)
			MyRecords.Add(new Collider(MyRandom.Next()), MyRandom.Next());

		for (var Index = 0; Index < 1024; Index++)
		{
			if (Index % 10 >= 5 && MyRecords.Count > 0)
			{
				var Key = ((IList<KeyValuePair<Collider, int>>)MyRecords)[MyRandom.Next(0, MyRecords.Count)].Key;

				MyRecords.Remove(Key);
			}
			else
			{
				MyRecords[new Collider(MyRandom.Next())] = MyRandom.Next();
			}
		}
	}

	[Test()]//, Repeat(2)]
	public void AddRemoveAll()
	{ //****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<int, int>();
		//****************************************

		for (var Index = 0; Index < 1024; Index++)
			MyRecords[MyRandom.Next(0, 128)] = MyRandom.Next(0, 128);

		while (MyRecords.Count > 0)
		{
			var Key = ((IList<KeyValuePair<int, int>>)MyRecords)[MyRandom.Next(0, MyRecords.Count)].Key;

			MyRecords.Remove(Key);
		}

		for (var Index = 0; Index < 1024; Index++)
			MyRecords[MyRandom.Next(0, 128)] = MyRandom.Next(0, 128);
	}

	[Test()]//, Repeat(2)]
	public void AddRemoveAllCollide()
	{ //****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<Collider, int>();
		//****************************************

		for (var Index = 0; Index < 16; Index++)
			MyRecords[new Collider(MyRandom.Next(0, 128))] = MyRandom.Next(0, 128);

		while (MyRecords.Count > 0)
		{
			var Key = ((IList<KeyValuePair<Collider, int>>)MyRecords)[MyRandom.Next(0, MyRecords.Count)].Key;

			MyRecords.Remove(Key);
		}

		for (var Index = 0; Index < 16; Index++)
			MyRecords[new Collider(MyRandom.Next(0, 128))] = MyRandom.Next(0, 128);
	}

	[Test]
	public void Clear()
	{ //****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<int, int>();

		//****************************************

		for (var Index = 0; Index < 1024; Index++)
			MyRecords.Add(MyRandom.Next(), MyRandom.Next());

		MyRecords.Clear();

		Assert.That(MyRecords.Count, Is.Zero);
	}

	[Test]
	public void ClearAdd()
	{ //****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<int, int>();

		//****************************************

		for (var Index = 0; Index < 1024; Index++)
			MyRecords.Add(MyRandom.Next(), MyRandom.Next());

		MyRecords.Clear();

		for (var Index = 0; Index < 1024; Index++)
			MyRecords.Add(MyRandom.Next(), MyRandom.Next());
	}

	[Test()]
	public void GetIndex()
	{	//****************************************
		var MyRecords = new ObservableDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(((IList<KeyValuePair<int, int>>)MyRecords)[0], Is.EqualTo(new KeyValuePair<int, int>(10, 42)));
	}

	[Test()]
	public void GetIndexMulti()
	{ //****************************************
		var MyRecords = new ObservableDictionary<int, int>();
		//****************************************

		MyRecords[8] = 40;
		MyRecords[9] = 41;
		MyRecords[10] = 42;
		MyRecords[11] = 43;
		MyRecords[12] = 44;

		//****************************************

		Assert.That(((IList<KeyValuePair<int, int>>)MyRecords)[2], Is.EqualTo(new KeyValuePair<int, int>(10, 42)));
	}

	[Test()]
	public void GetIndexOutOfRange()
	{	//****************************************
		var MyRecords = new ObservableDictionary<int, int>();
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
		var MyRecords = new ObservableDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords[10], Is.EqualTo(42));
	}

	[Test()]
	public void GetKeyMissing()
	{	//****************************************
		var MyRecords = new ObservableDictionary<int, int>();
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
	public void EnumerateDuringRemove()
	{
		var MyRecords = new ObservableDictionary<int, int>(3);

		MyRecords[10] = 42;
		MyRecords[11] = 43;
		MyRecords[12] = 44;

		KeyValuePair<int, int>[]? Snapshot = null;

		MyRecords.CollectionChanged += (sender, e) =>
		{
			if (e.Action == NotifyCollectionChangedAction.Remove)
				Snapshot = MyRecords.ToArray();
		};

		MyRecords.Remove(11);

		Assert.That(Snapshot, Is.Not.Null);

		Assert.That(Snapshot, Is.EquivalentTo(MyRecords));
	}

	[Test()]
	public void IndexOf()
	{	//****************************************
		var MyRecords = new ObservableDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOf(new KeyValuePair<int, int>(10, 42)), Is.Zero);
	}

	[Test()]
	public void IndexOfKey()
	{	//****************************************
		var MyRecords = new ObservableDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOfKey(10), Is.Zero);
	}

	[Test()]
	public void IndexOfKeyCollideMissing()
	{	//****************************************
		var MyRecords = new ObservableDictionary<Collider, int>();
		//****************************************

		MyRecords[new Collider(10)] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOfKey(new Collider(11)), Is.EqualTo(-1));
	}

	[Test()]
	public void IndexOfKeyCollideMissing2()
	{ //****************************************
		var MyRecords = new ObservableDictionary<Collider, int>();
		//****************************************

		MyRecords[new Collider(10)] = 42;
		MyRecords[new Collider(int.MaxValue)] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOfKey(new Collider(11)), Is.EqualTo(-1));
	}

	[Test()]
	public void IndexOfKeyMissing()
	{	//****************************************
		var MyRecords = new ObservableDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOfKey(11), Is.EqualTo(-1));
	}

	[Test()]
	public void IndexOfMissingKey()
	{	//****************************************
		var MyRecords = new ObservableDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOf(new KeyValuePair<int, int>(11, 12)), Is.EqualTo(-1));
	}

	[Test()]
	public void IndexOfMissingValue()
	{	//****************************************
		var MyRecords = new ObservableDictionary<int, int>();
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

		var MyRecords = new ObservableDictionary<int, int>(MyDictionary);

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(1024), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(MyDictionary.SequenceEquivalent(MyRecords), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		foreach (var MyPair in MyDictionary)
		{
			Assert.That(MyRecords.TryGetValue(MyPair.Key, out var Value), Is.True);
			Assert.That(Value, Is.EqualTo(MyPair.Value));
		}

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void Remove()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);

		var MyDictionary = new Dictionary<int, int>(1024);
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

		var MyRecords = new ObservableDictionary<int, int>(MyDictionary);

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

		var MyDictionary = new Dictionary<int, int>(1024);
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

		var MyRecords = new ObservableDictionary<int, int>(MyDictionary);

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

		var MyDictionary = new Dictionary<int, int>(1024);
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

		var MyRecords = new ObservableDictionary<int, int>(MyDictionary);

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

		var MyDictionary = new Dictionary<int, int>(1024);
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

		var MyRecords = new ObservableDictionary<int, int>(MyDictionary);

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
		var MyRecords = new ObservableDictionary<int, int>();
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
		var MyRecords = new ObservableDictionary<Collider, int>();
		var MyKey = new Collider(10);
		//****************************************

		MyRecords.Add(new Collider(9), 1);
		MyRecords.Add(new Collider(12), 2);
		MyRecords.Add(new Collider(10), 3);
		MyRecords.Add(new Collider(11), 4);

		MyRecords[MyKey] = 84;

		//****************************************

		Assert.That(MyRecords[MyKey], Is.EqualTo(84));
	}

	[Test()]
	public void SetKey()
	{	//****************************************
		var MyRecords = new ObservableDictionary<int, int>();
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
		var MyRecords = new ObservableDictionary<Collider, int>();
		var MyKey = new Collider(10);
		//****************************************

		MyRecords[new Collider(9)] = 1;
		MyRecords[new Collider(12)] = 2;
		MyRecords[new Collider(10)] = 3;
		MyRecords[new Collider(11)] = 4;

		MyRecords[MyKey] = 84;

		//****************************************

		Assert.That(MyRecords[MyKey], Is.EqualTo(84));
	}

	//****************************************

	[Test()]
	public void EventAdd()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<int, int>(1);
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

	[Test()]
	public void EventAddUpdate()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<int, int>(1);
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

	[Test()]
	public void EventAddMany()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<int, int>(1024);
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
		var MyRecords = new ObservableDictionary<int, int>();
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
		var MyRecords = new ObservableDictionary<int, int>();
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
		var MyRecords = new ObservableDictionary<int, int>();
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

	[Test()]
	public void EventReplace()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<int, int>(1);
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
		var MyRecords = new ObservableDictionary<int, int>();
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

	[Test()]
	public void EventRemove()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<int, int>(1);
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

	[Test()]
	public void EventRemoveLast()
	{ //****************************************
		var MyRecords = new ObservableDictionary<int, int>(10);
		var MyEventArgs = new List<NotifyCollectionChangedEventArgs>();
		//****************************************

		for (var Index = 0; Index < 10; Index++)
			MyRecords.Add(42 + Index, 84 + Index);

		MyRecords.CollectionChanged += (sender, e) => MyEventArgs.Add(e);

		MyRecords.RemoveAt(9);

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(9), "Item count does not match");
		Assert.That(MyEventArgs, Has.Count.EqualTo(1), "Incorrect number of events raised");

		var FirstEvent = MyEventArgs[0];

		Assert.That(((KeyValuePair<int, int>)FirstEvent.OldItems![0]!).Key, Is.EqualTo(42 + 9));

		Assert.That(MyRecords.Get(8).Key, Is.EqualTo(42 + 8));
	}

	[Test()]
	public void EventRemoveSecondLast()
	{ //****************************************
		var MyRecords = new ObservableDictionary<int, int>(10);
		var MyEventArgs = new List<NotifyCollectionChangedEventArgs>();
		//****************************************

		for (var Index = 0; Index < 10; Index++)
			MyRecords.Add(42 + Index, 84 + Index);

		MyRecords.CollectionChanged += (sender, e) => MyEventArgs.Add(e);

		MyRecords.RemoveAt(8);

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(9), "Item count does not match");
		Assert.That(MyEventArgs, Has.Count.EqualTo(1), "Incorrect number of events raised");

		var FirstEvent = MyEventArgs[0];

		Assert.That(((KeyValuePair<int, int>)FirstEvent.OldItems![0]!).Key, Is.EqualTo(42 + 8));

		Assert.That(MyRecords.Get(8).Key, Is.EqualTo(42 + 9));
	}

	[Test()]
	public void EventRemoveThirdLast()
	{ //****************************************
		var MyRecords = new ObservableDictionary<int, int>(10);
		var MyEventArgs = new List<(NotifyCollectionChangedEventArgs args, KeyValuePair<int, int>[] copy)>();
		//****************************************

		for (var Index = 0; Index < 10; Index++)
			MyRecords.Add(42 + Index, 84 + Index);

		MyRecords.CollectionChanged += (sender, e) => MyEventArgs.Add((e, MyRecords.ToArray()));

		MyRecords.RemoveAt(7);

		//****************************************

		Assert.That(MyRecords, Has.Count.EqualTo(9), "Item count does not match");
		Assert.That(MyEventArgs, Has.Count.EqualTo(2), "Incorrect number of events raised");

		var (FirstEvent, FirstVersion) = MyEventArgs[0];

		Assert.That(FirstEvent.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove), "Incorrect Collection Action");
		Assert.That(((KeyValuePair<int, int>)FirstEvent.OldItems![0]!).Key, Is.EqualTo(42 + 7));
		Assert.That(FirstVersion[8].Key, Is.EqualTo(42 + 9));

		var (SecondEvent, SecondVersion) = MyEventArgs[1];

		Assert.That(SecondEvent.Action, Is.EqualTo(NotifyCollectionChangedAction.Move), "Incorrect Collection Action");
		Assert.That(SecondEvent.OldStartingIndex, Is.EqualTo(8));
		Assert.That(SecondEvent.NewStartingIndex, Is.EqualTo(7));

		Assert.That(SecondVersion[7].Key, Is.EqualTo(42 + 9));
		Assert.That(SecondVersion[8].Key, Is.EqualTo(42 + 8));
	}

	[Test()]
	public void EventRemoveMany()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<int, int>(1024);
		var EventCount = 0;
		//****************************************

		while (MyRecords.Count < 1024)
			MyRecords[MyRandom.Next()] = MyRandom.Next();

		MyRecords.CollectionChanged += (sender, e) => { if (e.Action == NotifyCollectionChangedAction.Remove) EventCount++; };

		while (MyRecords.Count > 0)
			MyRecords.RemoveAt(MyRecords.Count - 1);

		//****************************************

		Assert.That(MyRecords.Count, Is.Zero, "Item count does not match");
		Assert.That(EventCount, Is.EqualTo(1024), "Event Count does not match");
	}

	[Test(), Repeat(2)]
	public void EventExercise()
	{ //****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new ObservableDictionary<int, int>(1024);
		var Monitor = new ObservableMonitor<KeyValuePair<int, int>>(MyRecords);
		//****************************************

		while (MyRecords.Count < 1024)
			MyRecords[MyRandom.Next()] = MyRandom.Next();

		for (var Index = 0; Index < 1024; Index++)
		{
			if (Index % 2 == 0)
				MyRecords.RemoveAt(MyRandom.Next(MyRecords.Count));
			else
				MyRecords[MyRandom.Next()] = MyRandom.Next();
		}

		//****************************************

		Assert.That(Monitor.ToArray(), Is.EqualTo(MyRecords).AsCollection);
	}
}
