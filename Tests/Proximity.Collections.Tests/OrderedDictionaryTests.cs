using System;
using System.Collections.Observable;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using NUnit.Framework;
//****************************************

#pragma warning disable IDE0028 // Simplify collection initialization

namespace Proximity.Collections.Tests
{
	[TestFixture]
	public class OrderedDictionaryTests
	{
		[Test()]//, Repeat(2)]
		public void Add()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new OrderedDictionary<int, int>();

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

			Assert.AreEqual(1024, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			Assert.IsTrue(MyDictionary.SequenceEquivalent(MyRecords), "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test(), Repeat(2)]
		public void AddCapacity()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new OrderedDictionary<int, int>(1024);

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

			Assert.AreEqual(1024, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			Assert.IsTrue(MyDictionary.SequenceEquivalent(MyRecords), "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test(), Repeat(2)]
		public void AddCollide()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new OrderedDictionary<Collider, int>();

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

			Assert.AreEqual(64, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			CollectionAssert.AreEquivalent(MyDictionary, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test()]
		public void AddExists()
		{	//****************************************
			var MyRecords = new OrderedDictionary<int, int>();
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
			var MyRecords = new OrderedDictionary<int, int>(1024);

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

			Assert.AreEqual(1024, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			Assert.IsTrue(MyDictionary.SequenceEquivalent(MyRecords), "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test(), Repeat(2)]
		public void AddRangeCollide()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new OrderedDictionary<Collider, int>(64);

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

			Assert.AreEqual(64, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			CollectionAssert.AreEquivalent(MyDictionary, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test()]
		public void AddRangeDuplicateInRange()
		{	//****************************************
			var MyRecords = new OrderedDictionary<int, int>(64);
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

			Assert.AreEqual(0, MyRecords.Count, "Items were added");
		}

		[Test()]
		public void AddRangeDuplicateInDictionary()
		{	//****************************************
			var MyRecords = new OrderedDictionary<int, int>(64);
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

			Assert.AreEqual(4, MyRecords.Count, "Items were added");
		}

		[Test(), Repeat(2)]
		public void AddRangePrePopulated()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new OrderedDictionary<int, int>(1024);

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

			Assert.AreEqual(1024, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			Assert.IsTrue(MyDictionary.SequenceEquivalent(MyRecords), "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test(), Repeat(2)]
		public void AddRangePrePopulatedCollide()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new OrderedDictionary<Collider, int>(64);

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

			Assert.AreEqual(64, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			Assert.IsTrue(MyDictionary.SequenceEquivalent(MyRecords), "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test()]//, Repeat(2)]
		public void AddRemove()
		{ //****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new OrderedDictionary<int, int>();
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
			var MyRecords = new OrderedDictionary<Collider, int>();
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
			var MyRecords = new OrderedDictionary<int, int>();
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
			var MyRecords = new OrderedDictionary<Collider, int>();
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
			var MyRecords = new OrderedDictionary<int, int>();

			//****************************************

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(), MyRandom.Next());

			MyRecords.Clear();

			Assert.AreEqual(0, MyRecords.Count);
		}

		[Test]
		public void ClearAdd()
		{ //****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new OrderedDictionary<int, int>();

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
			var MyRecords = new OrderedDictionary<int, int>();

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

			Assert.AreEqual(1024, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			var OrderedRecords = new List<KeyValuePair<int, int>>(MyDictionary);

			OrderedRecords.Sort(new ValueComparer<int, int>());

			// Ensure enumeration occurs in value-sorted order
			CollectionAssert.AreEqual(OrderedRecords, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test()]
		public void GetIndex()
		{	//****************************************
			var MyRecords = new OrderedDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(new KeyValuePair<int, int>(10, 42), ((IList<KeyValuePair<int, int>>)MyRecords)[0]);
		}

		[Test()]
		public void GetIndexMulti()
		{ //****************************************
			var MyRecords = new OrderedDictionary<int, int>();
			//****************************************

			MyRecords[8] = 40;
			MyRecords[9] = 41;
			MyRecords[10] = 42;
			MyRecords[11] = 43;
			MyRecords[12] = 44;

			//****************************************

			Assert.AreEqual(new KeyValuePair<int, int>(10, 42), ((IList<KeyValuePair<int, int>>)MyRecords)[2]);
		}

		[Test()]
		public void GetIndexOutOfRange()
		{	//****************************************
			var MyRecords = new OrderedDictionary<int, int>();
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
			var MyRecords = new OrderedDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(42, MyRecords[10]);
		}

		[Test()]
		public void GetKeyMissing()
		{	//****************************************
			var MyRecords = new OrderedDictionary<int, int>();
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
			var MyRecords = new OrderedDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(0, MyRecords.IndexOf(new KeyValuePair<int,int>(10, 42)));
		}

		[Test()]
		public void IndexOfKey()
		{	//****************************************
			var MyRecords = new OrderedDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(0, MyRecords.IndexOfKey(10));
		}

		[Test()]
		public void IndexOfKeyCollideMissing()
		{	//****************************************
			var MyRecords = new OrderedDictionary<Collider, int>();
			//****************************************

			MyRecords[new Collider(10)] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOfKey(new Collider(11)));
		}

		[Test()]
		public void IndexOfKeyCollideMissing2()
		{ //****************************************
			var MyRecords = new OrderedDictionary<Collider, int>();
			//****************************************

			MyRecords[new Collider(10)] = 42;
			MyRecords[new Collider(int.MaxValue)] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOfKey(new Collider(11)));
		}

		[Test()]
		public void IndexOfKeyMissing()
		{	//****************************************
			var MyRecords = new OrderedDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOfKey(11));
		}

		[Test()]
		public void IndexOfMissingKey()
		{	//****************************************
			var MyRecords = new OrderedDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOf(new KeyValuePair<int, int>(11, 12)));
		}

		[Test()]
		public void IndexOfMissingValue()
		{	//****************************************
			var MyRecords = new OrderedDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOf(new KeyValuePair<int, int>(10, 30)));
		}

		[Test()]
		public void IndexOfValue()
		{ //****************************************
			var MyRecords = new OrderedDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(0, MyRecords.IndexOfValue(42));
		}

		[Test()]
		public void IndexOfValueMissing()
		{ //****************************************
			var MyRecords = new OrderedDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOfValue(30));
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

			var MyRecords = new OrderedDictionary<int, int>(MyDictionary);

			//****************************************

			Assert.AreEqual(1024, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			Assert.IsTrue(MyDictionary.SequenceEquivalent(MyRecords), "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
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

			var MyRecords = new OrderedDictionary<int, int>(MyDictionary);

			//****************************************

			for (var Index = 0; Index < 512; Index++)
			{
				var InnerIndex = MyRandom.Next(MyRecords.Count);

				var Key = MyRecords.Keys[InnerIndex];

				Assert.IsTrue(MyRecords.Remove(Key));
				Assert.IsTrue(MyDictionary.Remove(Key));
			}

			//****************************************

			Assert.AreEqual(512, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			Assert.IsTrue(MyDictionary.SequenceEquivalent(MyRecords), "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
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

			var MyRecords = new OrderedDictionary<int, int>(MyDictionary);

			//****************************************

			for (var Index = 0; Index < 512; Index++)
			{
				var InnerIndex = MyRandom.Next(MyRecords.Count);

				var Key = MyRecords.Keys[InnerIndex];

				MyRecords.RemoveAt(InnerIndex);
				Assert.IsTrue(MyDictionary.Remove(Key));
			}

			//****************************************

			Assert.AreEqual(512, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			Assert.IsTrue(MyDictionary.SequenceEquivalent(MyRecords), "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
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

			var MyRecords = new OrderedDictionary<int, int>(MyDictionary);

			//****************************************

			foreach (var MyResult in MyRecords.Skip(256).Take(256))
				MyDictionary.Remove(MyResult.Key);

			MyRecords.RemoveRange(256, 256);

			//****************************************

			Assert.IsTrue(MyDictionary.SequenceEquivalent(MyRecords), "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test()]
		public void Replace()
		{	//****************************************
			var MyRecords = new OrderedDictionary<int, int>();
			//****************************************

			MyRecords.Add(9, 1);
			MyRecords.Add(12, 2);
			MyRecords.Add(10, 3);
			MyRecords.Add(11, 4);
			MyRecords[10] = 84;

			//****************************************

			Assert.AreEqual(84, MyRecords[10]);
		}

		[Test()]
		public void ReplaceCollide()
		{	//****************************************
			var MyRecords = new OrderedDictionary<Collider, int>();
			var MyKey = new Collider(10);
			//****************************************

			MyRecords.Add(new Collider(9), 1);
			MyRecords.Add(new Collider(12), 2);
			MyRecords.Add(new Collider(10), 3);
			MyRecords.Add(new Collider(11), 4);

			MyRecords[MyKey] = 84;

			//****************************************

			Assert.AreEqual(84, MyRecords[MyKey]);
		}

		[Test()]
		public void SetKey()
		{	//****************************************
			var MyRecords = new OrderedDictionary<int, int>();
			//****************************************

			MyRecords[9] = 1;
			MyRecords[12] = 2;
			MyRecords[10] = 3;
			MyRecords[11] = 4;

			MyRecords[10] = 84;

			//****************************************

			Assert.AreEqual(84, MyRecords[10]);
		}

		[Test()]
		public void SetKeyCollide()
		{	//****************************************
			var MyRecords = new OrderedDictionary<Collider, int>();
			var MyKey = new Collider(10);
			//****************************************

			MyRecords[new Collider(9)] = 1;
			MyRecords[new Collider(12)] = 2;
			MyRecords[new Collider(10)] = 3;
			MyRecords[new Collider(11)] = 4;

			MyRecords[MyKey] = 84;

			//****************************************

			Assert.AreEqual(84, MyRecords[MyKey]);
		}


		private sealed class ValueComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>> where TValue : IComparable<TValue>
		{
			public ValueComparer()
			{
			}

			public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) => x.Value.CompareTo(y.Value);
		}

	}
}
