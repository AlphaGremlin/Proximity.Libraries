/****************************************\
 ObservableSortedListTests.cs
 Created: 2016-05-26
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Utility.Tests
{
	[TestFixture, Timeout(2000)]
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

			Assert.AreEqual(1024, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			CollectionAssert.AreEqual(MySortedList, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);

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

			Assert.AreEqual(1024, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			CollectionAssert.AreEqual(MySortedList, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);

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

			for (int Index = 0; Index < 64; Index++)
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

			Assert.AreEqual(64, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			CollectionAssert.AreEquivalent(MyDictionary, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);

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

			Assert.AreEqual(1024, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			CollectionAssert.AreEqual(MySortedList, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);
			
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

			for (int Index = 0; Index < 64; Index++)
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

			Assert.AreEqual(64, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			CollectionAssert.AreEquivalent(MyDictionary, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);
			
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

			Assert.AreEqual(0, MyRecords.Count, "Items were added");
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

			Assert.AreEqual(4, MyRecords.Count, "Items were added");
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

			Assert.AreEqual(1024, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			CollectionAssert.AreEqual(MySortedList, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);

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

			for (int Index = 0; Index < 32; Index++)
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

			for (int Index = 0; Index < 32; Index++)
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

			Assert.AreEqual(64, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			CollectionAssert.AreEquivalent(MyDictionary, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test()]
		public void GetIndex()
		{	//****************************************
			var MyRecords = new ObservableSortedList<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(new KeyValuePair<int, int>(10, 42), ((IList<KeyValuePair<int, int>>)MyRecords)[0]);
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

			Assert.AreEqual(42, MyRecords[10]);
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

			Assert.AreEqual(0, MyRecords.IndexOf(new KeyValuePair<int,int>(10, 42)));
		}

		[Test()]
		public void IndexOfKey()
		{	//****************************************
			var MyRecords = new ObservableSortedList<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(0, MyRecords.IndexOfKey(10));
		}

		[Test()]
		public void IndexOfKeyCollideMissing()
		{	//****************************************
			var MyRecords = new ObservableSortedList<CollideStruct, int>();
			//****************************************

			MyRecords[new CollideStruct(10)] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOfKey(new CollideStruct(11)));
		}

		[Test()]
		public void IndexOfKeyCollideMissing2()
		{	//****************************************
			var MyRecords = new ObservableSortedList<CollideStruct, int>();
			//****************************************

			MyRecords[new CollideStruct(10)] = 42;
			MyRecords[new CollideStruct(int.MaxValue)] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOfKey(new CollideStruct(11)));
		}

		[Test()]
		public void IndexOfKeyMissing()
		{	//****************************************
			var MyRecords = new ObservableSortedList<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOfKey(11));
		}

		[Test()]
		public void IndexOfMissingKey()
		{	//****************************************
			var MyRecords = new ObservableSortedList<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOf(new KeyValuePair<int, int>(11, 12)));
		}

		[Test()]
		public void IndexOfMissingValue()
		{	//****************************************
			var MyRecords = new ObservableSortedList<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOf(new KeyValuePair<int, int>(10, 30)));
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

			Assert.AreEqual(1024, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			CollectionAssert.AreEqual(MySortedList, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);
			
			Thread.Sleep(1);
		}

		[Test(), Repeat(2)]
		public void Remove()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);

			var MyDictionary = new SortedList<int, int>(1024);
			//****************************************

			for (int Index = 0; Index < 1024; Index++)
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

			for (int Index = 0; Index < 512; Index++)
			{
				int InnerIndex = MyRandom.Next(MyRecords.Count);

				var Key = MyRecords.Keys[InnerIndex];

				Assert.IsTrue(MyRecords.Remove(Key));
				Assert.IsTrue(MyDictionary.Remove(Key));
			}

			//****************************************

			Assert.AreEqual(512, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			CollectionAssert.AreEquivalent(MyDictionary, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				int Value;

				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out Value));
				Assert.AreEqual(MyPair.Value, Value);
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

			for (int Index = 0; Index < 1024; Index++)
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

			for (int Index = 0; Index < 512; Index++)
			{
				int InnerIndex = MyRandom.Next(MyRecords.Count);

				var Key = MyRecords.Keys[InnerIndex];

				MyRecords.RemoveAt(InnerIndex);
				Assert.IsTrue(MyDictionary.Remove(Key));
			}

			//****************************************

			Assert.AreEqual(512, MyRecords.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			CollectionAssert.AreEquivalent(MyDictionary, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				int Value;

				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out Value));
				Assert.AreEqual(MyPair.Value, Value);
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

			for (int Index = 0; Index < 1024; Index++)
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

			CollectionAssert.AreEquivalent(MyDictionary, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				int Value;

				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out Value));
				Assert.AreEqual(MyPair.Value, Value);
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

			for (int Index = 0; Index < 1024; Index++)
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

			CollectionAssert.AreEquivalent(MyDictionary, MyRecords, "Collections don't match. Bad Seed was {0}", MySeed);

			foreach (var MyPair in MyDictionary)
			{
				int Value;

				Assert.IsTrue(MyRecords.TryGetValue(MyPair.Key, out Value));
				Assert.AreEqual(MyPair.Value, Value);
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

			Assert.AreEqual(84, MyRecords[10]);
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

			Assert.AreEqual(84, MyRecords[MyKey]);
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

			Assert.AreEqual(84, MyRecords[10]);
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

			Assert.AreEqual(84, MyRecords[MyKey]);
		}

		//****************************************

		[Test(), Timeout(1000)]
		public void EventAdd()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableSortedList<int, int>(1);
			NotifyCollectionChangedEventArgs MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
			//****************************************

			MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
			MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
			MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

			var Pair = new KeyValuePair<int, int>(MyRandom.Next(), MyRandom.Next());

			MyRecords.Add(Pair);

			//****************************************

			Assert.AreEqual(1, MyRecords.Count, "Item count does not match");

			Assert.IsNotNull(MyEventArgs, "No Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Add, MyEventArgs.Action);
			Assert.IsNotNull(MyEventArgs.NewItems, "No New Items");
			Assert.AreEqual(0, MyEventArgs.NewStartingIndex, "Starting Index incorrect");
			Assert.AreEqual(1, MyEventArgs.NewItems.Count, "New Items Count incorrect");
			Assert.AreEqual(Pair, MyEventArgs.NewItems[0], "New Items Value incorrect");

			Assert.IsNotNull(MyKeyEventArgs, "No Key Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Add, MyKeyEventArgs.Action);
			Assert.IsNotNull(MyKeyEventArgs.NewItems, "No Key New Items");
			Assert.AreEqual(0, MyKeyEventArgs.NewStartingIndex, "Key Starting Index incorrect");
			Assert.AreEqual(1, MyKeyEventArgs.NewItems.Count, "New Key Items Count incorrect");
			Assert.AreEqual(Pair.Key, MyKeyEventArgs.NewItems[0], "New Key Items Value incorrect");

			Assert.IsNotNull(MyValueEventArgs, "No Value Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Add, MyEventArgs.Action);
			Assert.IsNotNull(MyValueEventArgs.NewItems, "No Value New Items");
			Assert.AreEqual(0, MyValueEventArgs.NewStartingIndex, "Value Starting Index incorrect");
			Assert.AreEqual(1, MyValueEventArgs.NewItems.Count, "New Value Items Count incorrect");
			Assert.AreEqual(Pair.Value, MyValueEventArgs.NewItems[0], "New Value Items Value incorrect");
		}

		[Test(), Timeout(1000)]
		public void EventAddUpdate()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableSortedList<int, int>(1);
			NotifyCollectionChangedEventArgs MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
			//****************************************

			MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
			MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
			MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

			var Pair = new KeyValuePair<int, int>(MyRandom.Next(), MyRandom.Next());

			MyRecords.BeginUpdate();

			MyRecords.Add(Pair);

			//****************************************

			Assert.IsNull(MyEventArgs, "Event Raised");
			Assert.IsNull(MyKeyEventArgs, "Event Raised");
			Assert.IsNull(MyValueEventArgs, "Event Raised");

			//****************************************

			MyRecords.EndUpdate();

			Assert.AreEqual(1, MyRecords.Count, "Item count does not match");

			Assert.IsNotNull(MyEventArgs, "No Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Reset, MyEventArgs.Action);

			Assert.IsNotNull(MyKeyEventArgs, "No Key Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Reset, MyKeyEventArgs.Action);

			Assert.IsNotNull(MyValueEventArgs, "No Value Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Reset, MyEventArgs.Action);
		}

		[Test(), Timeout(1000)]
		public void EventAddMany()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableSortedList<int, int>(1024);
			int EventCount = 0;
			//****************************************

			MyRecords.CollectionChanged += (sender, e) => { if (e.Action == NotifyCollectionChangedAction.Add) EventCount++; };

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(), MyRandom.Next());

			//****************************************

			Assert.AreEqual(1024, MyRecords.Count, "Item count does not match");
			Assert.AreEqual(1024, EventCount, "Event Count does not match");
		}

		[Test()]
		public void EventAddRangeEmpty()
		{	//****************************************
			var MyRecords = new ObservableSortedList<int, int>();
			NotifyCollectionChangedEventArgs MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
			//****************************************

			MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
			MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
			MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

			MyRecords.AddRange(Enumerable.Empty<KeyValuePair<int, int>>());

			//****************************************

			Assert.IsNull(MyEventArgs, "Event Raised");
			Assert.IsNull(MyKeyEventArgs, "Event Raised");
			Assert.IsNull(MyValueEventArgs, "Event Raised");
		}

		[Test()]
		public void EventClear()
		{	//****************************************
			var MyRecords = new ObservableSortedList<int, int>();
			NotifyCollectionChangedEventArgs MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
			//****************************************

			MyRecords[10] = 42;

			MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
			MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
			MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

			MyRecords.Clear();

			//****************************************

			Assert.AreEqual(0, MyRecords.Count, "Items not cleared");

			Assert.IsNotNull(MyEventArgs, "No Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Reset, MyEventArgs.Action);

			Assert.IsNotNull(MyKeyEventArgs, "No Key Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Reset, MyKeyEventArgs.Action);

			Assert.IsNotNull(MyValueEventArgs, "No Value Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Reset, MyEventArgs.Action);
		}

		[Test()]
		public void EventClearEmpty()
		{	//****************************************
			var MyRecords = new ObservableSortedList<int, int>();
			NotifyCollectionChangedEventArgs MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
			//****************************************

			MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
			MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
			MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

			MyRecords.Clear();

			//****************************************

			Assert.AreEqual(0, MyRecords.Count, "Items not cleared");

			Assert.IsNull(MyEventArgs, "Event Raised");
			Assert.IsNull(MyKeyEventArgs, "Event Raised");
			Assert.IsNull(MyValueEventArgs, "Event Raised");
		}

		[Test(), Timeout(1000)]
		public void EventReplace()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableSortedList<int, int>(1);
			NotifyCollectionChangedEventArgs MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
			//****************************************

			MyRecords.Add(42, 10);

			MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
			MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
			MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

			MyRecords[42] = 84;

			//****************************************

			Assert.AreEqual(1, MyRecords.Count, "Item count does not match");
			Assert.IsNotNull(MyEventArgs, "No Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Replace, MyEventArgs.Action);

			Assert.IsNotNull(MyEventArgs.OldItems, "No Old Items");
			Assert.AreEqual(0, MyEventArgs.OldStartingIndex, "Starting Index incorrect");
			Assert.AreEqual(1, MyEventArgs.OldItems.Count, "Old Items Count incorrect");
			Assert.AreEqual(new KeyValuePair<int, int>(42, 10), MyEventArgs.OldItems[0], "Old Items Value incorrect");

			Assert.IsNotNull(MyEventArgs.NewItems, "No New Items");
			Assert.AreEqual(0, MyEventArgs.NewStartingIndex, "Starting Index incorrect");
			Assert.AreEqual(1, MyEventArgs.NewItems.Count, "New Items Count incorrect");
			Assert.AreEqual(new KeyValuePair<int, int>(42, 84), MyEventArgs.NewItems[0], "New Items Value incorrect");
		}

		[Test()]
		public void EventReplaceUnchanged()
		{	//****************************************
			var MyRecords = new ObservableSortedList<int, int>();
			NotifyCollectionChangedEventArgs MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
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

			Assert.AreEqual(3, MyRecords[10]);

			Assert.IsNull(MyEventArgs, "Event Raised");
			Assert.IsNull(MyKeyEventArgs, "Event Raised");
			Assert.IsNull(MyValueEventArgs, "Event Raised");
		}

		[Test(), Timeout(1000)]
		public void EventRemove()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableSortedList<int, int>(1);
			NotifyCollectionChangedEventArgs MyEventArgs = null, MyKeyEventArgs = null, MyValueEventArgs = null;
			//****************************************

			MyRecords.Add(42, 84);

			MyRecords.CollectionChanged += (sender, e) => MyEventArgs = e;
			MyRecords.Keys.CollectionChanged += (sender, e) => MyKeyEventArgs = e;
			MyRecords.Values.CollectionChanged += (sender, e) => MyValueEventArgs = e;

			MyRecords.RemoveAt(0);

			//****************************************

			Assert.AreEqual(0, MyRecords.Count, "Item count does not match");
			Assert.IsNotNull(MyEventArgs, "No Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Remove, MyEventArgs.Action);
			Assert.IsNotNull(MyEventArgs.OldItems, "No Old Items");
			Assert.AreEqual(0, MyEventArgs.OldStartingIndex, "Starting Index incorrect");
			Assert.AreEqual(1, MyEventArgs.OldItems.Count, "Old Items Count incorrect");
			Assert.AreEqual(new KeyValuePair<int, int>(42, 84), MyEventArgs.OldItems[0], "Old Items Value incorrect");

			Assert.IsNotNull(MyKeyEventArgs, "No Key Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Remove, MyKeyEventArgs.Action);
			Assert.IsNotNull(MyKeyEventArgs.OldItems, "No Key Old Items");
			Assert.AreEqual(0, MyKeyEventArgs.OldStartingIndex, "Key Starting Index incorrect");
			Assert.AreEqual(1, MyKeyEventArgs.OldItems.Count, "Old Key Items Count incorrect");
			Assert.AreEqual(42, MyKeyEventArgs.OldItems[0], "Old Key Items Value incorrect");

			Assert.IsNotNull(MyValueEventArgs, "No Value Event Raised");
			Assert.AreEqual(NotifyCollectionChangedAction.Remove, MyEventArgs.Action);
			Assert.IsNotNull(MyValueEventArgs.OldItems, "No Value Old Items");
			Assert.AreEqual(0, MyValueEventArgs.OldStartingIndex, "Value Starting Index incorrect");
			Assert.AreEqual(1, MyValueEventArgs.OldItems.Count, "Old Value Items Count incorrect");
			Assert.AreEqual(84, MyValueEventArgs.OldItems[0], "Old Value Items Value incorrect");
		}

		[Test(), Timeout(1000)]
		public void EventRemoveMany()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableSortedList<int, int>(1024);
			int EventCount = 0;
			//****************************************

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(), MyRandom.Next());

			MyRecords.CollectionChanged += (sender, e) => { if (e.Action == NotifyCollectionChangedAction.Remove) EventCount++; };

			while (MyRecords.Count > 0)
				MyRecords.RemoveAt(MyRecords.Count - 1);

			//****************************************

			Assert.AreEqual(0, MyRecords.Count, "Item count does not match");
			Assert.AreEqual(1024, EventCount, "Event Count does not match");
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
}
