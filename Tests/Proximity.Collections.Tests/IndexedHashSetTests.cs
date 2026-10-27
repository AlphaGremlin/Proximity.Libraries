using NUnit.Framework;

#pragma warning disable IDE0028 // Simplify collection initialization

namespace Proximity.Collections.Tests;

[TestFixture]
public class IndexedHashSetTests
{
	[Test()]//, Repeat(2)]
	public void Add()
	{ //****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var TestSet = new IndexedHashSet<int>();

		var ReferenceSet = new HashSet<int>(1024);
		//****************************************

		for (var Index = 0; Index < 1024; Index++)
		{
			int Key;

			do
			{
				Key = MyRandom.Next();
			} while (ReferenceSet.Contains(Key));

			ReferenceSet.Add(Key);
			TestSet.Add(Key);
		}

		//****************************************

		Assert.That(TestSet, Has.Count.EqualTo(1024), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(ReferenceSet.SequenceEquivalent(TestSet), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		ReferenceSet.SymmetricExceptWith(TestSet);

		Assert.That(ReferenceSet, Is.Empty);

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void AddCapacity()
	{ //****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var TestSet = new IndexedHashSet<int>(1024);

		var ReferenceSet = new HashSet<int>(1024);
		//****************************************

		for (var Index = 0; Index < 1024; Index++)
		{
			int Key;

			do
			{
				Key = MyRandom.Next();
			} while (ReferenceSet.Contains(Key));

			ReferenceSet.Add(Key);
			TestSet.Add(Key);
		}

		//****************************************

		Assert.That(TestSet, Has.Count.EqualTo(1024), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(ReferenceSet.SequenceEquivalent(TestSet), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		ReferenceSet.SymmetricExceptWith(TestSet);

		Assert.That(ReferenceSet, Is.Empty);

		Thread.Sleep(1);
	}

	[Test(), Repeat(2)]
	public void AddCollide()
	{ //****************************************
		var MySeed = Environment.TickCount;
		var MyRandom = new Random(MySeed);
		var TestSet = new IndexedHashSet<Collider>();

		var ReferenceSet = new HashSet<Collider>(64);
		//****************************************

		for (var Index = 0; Index < 64; Index++)
		{
			Collider Key;

			do
			{
				Key = new Collider(MyRandom.Next());
			} while (ReferenceSet.Contains(Key));

			ReferenceSet.Add(Key);
			TestSet.Add(Key);
		}

		//****************************************

		Assert.That(TestSet, Has.Count.EqualTo(64), $"Count incorrect. Bad Seed was {MySeed}");

		Assert.That(ReferenceSet.SequenceEquivalent(TestSet), Is.True, $"Collections don't match. Bad Seed was {MySeed}");

		ReferenceSet.SymmetricExceptWith(TestSet);

		Assert.That(ReferenceSet, Is.Empty);

		Thread.Sleep(1);
	}

	[Test()]
	public void AddExists()
	{ //****************************************
		var MyRecords = new IndexedHashSet<int>();
		//****************************************

		MyRecords.Add(10);

		//****************************************

		Assert.That(MyRecords.Add(10), Is.EqualTo(-1));
	}


}
