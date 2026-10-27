using NUnit.Framework;
//****************************************

#pragma warning disable IDE0028 // Simplify collection initialization

namespace Proximity.Collections.Tests;

[TestFixture]
public class OrderedSortedDictionaryTests
{
	[Test()]//, Repeat(2)]
	public void Add()
	{	//****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new OrderedSortedDictionary<int, int>();

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
		var MyRecords = new OrderedSortedDictionary<int, int>(1024);

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
		var MyRecords = new OrderedSortedDictionary<Collider, int>();

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

		Assert.That(MyRecords, Is.EquivalentTo(MyDictionary), $"Collections don't match. Bad Seed was {MySeed}");

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
		var MyRecords = new OrderedSortedDictionary<int, int>();
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
		var MyRecords = new OrderedSortedDictionary<int, int>(1024);

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
		var MyRecords = new OrderedSortedDictionary<Collider, int>(64);

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

		Assert.That(MyRecords, Is.EquivalentTo(MyDictionary), $"Collections don't match. Bad Seed was {MySeed}");

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
		var MyRecords = new OrderedSortedDictionary<int, int>(64);
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
		var MyRecords = new OrderedSortedDictionary<int, int>(64);
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
		var MyRecords = new OrderedSortedDictionary<int, int>(1024);

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
		var MyRecords = new OrderedSortedDictionary<Collider, int>(64);

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
		var MyRecords = new OrderedSortedDictionary<int, int>();
		//****************************************

		for (var Index = 0; Index < 16; Index++)
			MyRecords.Add(MyRandom.Next(), MyRandom.Next());

		for (var Index = 0; Index < 1024; Index++)
		{
			if (Index % 10 >= 5 && MyRecords.Count > 0)
			{
				var Key = MyRecords.Get(MyRandom.Next(0, MyRecords.Count)).Key;

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
		var MyRecords = new OrderedSortedDictionary<Collider, int>();
		//****************************************

		for (var Index = 0; Index < 16; Index++)
			MyRecords.Add(new Collider(MyRandom.Next()), MyRandom.Next());

		for (var Index = 0; Index < 1024; Index++)
		{
			if (Index % 10 >= 5 && MyRecords.Count > 0)
			{
				var Key = MyRecords.Get(MyRandom.Next(0, MyRecords.Count)).Key;

				MyRecords.Remove(Key);
			}
			else
			{
				MyRecords[new Collider(MyRandom.Next())] = MyRandom.Next();
			}
		}
	}

	[Test()]//, Repeat(2)]
	public void AddRemoveAllAdd()
	{ //****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new OrderedSortedDictionary<int, int>();
		//****************************************

		for (var Index = 0; Index < 1024; Index++)
			MyRecords[MyRandom.Next(0, 128)] = MyRandom.Next(0, 128);

		while (MyRecords.Count > 0)
		{
			var Key = MyRecords.Get(MyRandom.Next(0, MyRecords.Count)).Key;

			MyRecords.Remove(Key);
		}

		for (var Index = 0; Index < 1024; Index++)
			MyRecords[MyRandom.Next(0, 128)] = MyRandom.Next(0, 128);
	}

	[Test()]//, Repeat(2)]
	public void AddRemoveAllAddCollide()
	{ //****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new OrderedSortedDictionary<Collider, int>();
		//****************************************

		for (var Index = 0; Index < 16; Index++)
			MyRecords[new Collider(MyRandom.Next(0, 128))] = MyRandom.Next(0, 128);

		while (MyRecords.Count > 0)
		{
			var Key = MyRecords.Get(MyRandom.Next(0, MyRecords.Count)).Key;

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
		var MyRecords = new OrderedSortedDictionary<int, int>();

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
		var MyRecords = new OrderedSortedDictionary<int, int>();

		//****************************************

		for (var Index = 0; Index < 1024; Index++)
			MyRecords.Add(MyRandom.Next(), MyRandom.Next());

		MyRecords.Clear();

		for (var Index = 0; Index < 1024; Index++)
			MyRecords.Add(MyRandom.Next(), MyRandom.Next());
	}

	[Test()]//, Repeat(2)]
	public void Enumerate()
	{ //****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var MyRecords = new OrderedSortedDictionary<int, int>();

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

		var OrderedRecords = new List<KeyValuePair<int, int>>(MyDictionary);

		OrderedRecords.Sort(new ValueComparer<int, int>());

		// Ensure enumeration occurs in value-sorted order
		Assert.That(MyRecords, Is.EqualTo(OrderedRecords).AsCollection, $"Collections don't match. Bad Seed was {MySeed}");

		Thread.Sleep(1);
	}

	[Test()]
	public void GetIndex()
	{	//****************************************
		var MyRecords = new OrderedSortedDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(((IList<KeyValuePair<int, int>>)MyRecords)[0], Is.EqualTo(new KeyValuePair<int, int>(10, 42)));
	}

	[Test()]
	public void GetIndexMulti()
	{ //****************************************
		var MyRecords = new OrderedSortedDictionary<int, int>();
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
		var MyRecords = new OrderedSortedDictionary<int, int>();
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
		var MyRecords = new OrderedSortedDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords[10], Is.EqualTo(42));
	}

	[Test()]
	public void GetKeyMissing()
	{	//****************************************
		var MyRecords = new OrderedSortedDictionary<int, int>();
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
		var MyRecords = new OrderedSortedDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOf(new KeyValuePair<int, int>(10, 42)), Is.Zero);
	}

	[Test()]
	public void IndexOfKey()
	{	//****************************************
		var MyRecords = new OrderedSortedDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOfKey(10), Is.Zero);
	}

	[Test()]
	public void IndexOfKeyCollideMissing()
	{	//****************************************
		var MyRecords = new OrderedSortedDictionary<Collider, int>();
		//****************************************

		MyRecords[new Collider(10)] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOfKey(new Collider(11)), Is.EqualTo(-1));
	}

	[Test()]
	public void IndexOfKeyCollideMissing2()
	{ //****************************************
		var MyRecords = new OrderedSortedDictionary<Collider, int>();
		//****************************************

		MyRecords[new Collider(10)] = 42;
		MyRecords[new Collider(int.MaxValue)] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOfKey(new Collider(11)), Is.EqualTo(-1));
	}

	[Test()]
	public void IndexOfKeyMissing()
	{	//****************************************
		var MyRecords = new OrderedSortedDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOfKey(11), Is.EqualTo(-1));
	}

	[Test()]
	public void IndexOfMissingKey()
	{	//****************************************
		var MyRecords = new OrderedSortedDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOf(new KeyValuePair<int, int>(11, 12)), Is.EqualTo(-1));
	}

	[Test()]
	public void IndexOfMissingValue()
	{	//****************************************
		var MyRecords = new OrderedSortedDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOf(new KeyValuePair<int, int>(10, 30)), Is.EqualTo(-1));
	}

	[Test()]
	public void IndexOfValue()
	{ //****************************************
		var MyRecords = new OrderedSortedDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOfValue(42), Is.Zero);
	}

	[Test()]
	public void IndexOfValueMissing()
	{ //****************************************
		var MyRecords = new OrderedSortedDictionary<int, int>();
		//****************************************

		MyRecords[10] = 42;

		//****************************************

		Assert.That(MyRecords.IndexOfValue(30), Is.EqualTo(-1));
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

		var MyRecords = new OrderedSortedDictionary<int, int>(MyDictionary);

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

		var MyRecords = new OrderedSortedDictionary<int, int>(MyDictionary);

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

		var MyRecords = new OrderedSortedDictionary<int, int>(MyDictionary);

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

		var MyRecords = new OrderedSortedDictionary<int, int>(MyDictionary);

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
		var MyRecords = new OrderedSortedDictionary<int, int>();
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
		var MyRecords = new OrderedSortedDictionary<Collider, int>();
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
		var MyRecords = new OrderedSortedDictionary<int, int>();
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
		var MyRecords = new OrderedSortedDictionary<Collider, int>();
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


	private sealed class ValueComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>> where TValue : IComparable<TValue>
	{
		public ValueComparer()
		{
		}

		public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) => x.Value.CompareTo(y.Value);
	}

}
