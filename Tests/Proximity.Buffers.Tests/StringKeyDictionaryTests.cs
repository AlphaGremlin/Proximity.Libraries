using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using NUnit.Framework;
//****************************************

#pragma warning disable IDE0028 // Simplify collection initialization

namespace Proximity.Buffers.Tests
{
	[TestFixture]
	public class StringKeyDictionaryTests
	{
		[Test()]//, Repeat(2)]
		public void Add()
		{ //****************************************
			var Random = Initialise();
			var Records = new StringKeyDictionary<int>();

			var Dictionary = new Dictionary<string, int>(1024);
			//****************************************

			foreach (var Pair in YieldRandom(Random, 1024))
			{
				Dictionary.Add(Pair.Key, Pair.Value);
				Records.Add(Pair.Key, Pair.Value);
			}

			//****************************************

			Assert.AreEqual(1024, Records.Count, "Count incorrect");

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match");

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test(), Repeat(2)]
		public void AddCapacity()
		{ //****************************************
			var Random = Initialise();
			var Records = new StringKeyDictionary<int>();

			var Dictionary = new Dictionary<string, int>(1024);
			//****************************************

			foreach (var Pair in YieldRandom(Random, 1024))
			{
				Dictionary.Add(Pair.Key, Pair.Value);
				Records.Add(Pair.Key, Pair.Value);
			}

			//****************************************

			Assert.AreEqual(1024, Records.Count, "Count incorrect");

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match");

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test()]
		public void AddExists()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>();
			//****************************************

			MyRecords.Add("A", 84);

			//****************************************

			try
			{
				MyRecords.Add("A", 84);

				Assert.Fail("Add succeeded");
			}
			catch (ArgumentException)
			{
			}
		}

		[Test(), Repeat(2)]
		public void AddRange()
		{ //****************************************
			var Random = Initialise();
			var Records = new StringKeyDictionary<int>();

			var Dictionary = new Dictionary<string, int>(1024);
			//****************************************

			foreach (var Pair in YieldRandom(Random, 1024))
			{
				Dictionary.Add(Pair.Key, Pair.Value);
			}

			Records.AddRange(Dictionary);

			//****************************************

			Assert.AreEqual(1024, Records.Count, "Count incorrect");

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match");

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test()]
		public void AddRangeDuplicateInRange()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>(64);
			//****************************************

			try
			{
				MyRecords.AddRange(new[] { new KeyValuePair<string, int>("A", 1), new KeyValuePair<string, int>("B", 2), new KeyValuePair<string, int>("C", 3), new KeyValuePair<string, int>("A", 4) });

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
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>(64);
			//****************************************

			MyRecords["E"] = 1;
			MyRecords["F"] = 2;
			MyRecords["G"] = 3;
			MyRecords["H"] = 4;

			try
			{
				MyRecords.AddRange(new[] { new KeyValuePair<string, int>("A", 1), new KeyValuePair<string, int>("B", 2), new KeyValuePair<string, int>("C", 3), new KeyValuePair<string, int>("G", 4) });

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
		{ //****************************************
			var Random = Initialise();
			var Records = new StringKeyDictionary<int>();

			var Dictionary = new Dictionary<string, int>(1024);
			var SecondSet = new List<KeyValuePair<string, int>>(512);
			//****************************************

			foreach (var Pair in YieldRandom(Random, 1024))
			{
				Dictionary.Add(Pair.Key, Pair.Value);

				if (Records.Count < 512)
					Records.Add(Pair.Key, Pair.Value);
				else
					SecondSet.Add(Pair);
			}

			//****************************************

			Records.AddRange(SecondSet);

			//****************************************

			Assert.AreEqual(1024, Records.Count, "Count incorrect");

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match");

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test()]//, Repeat(2)]
		public void AddRemove()
		{ //****************************************
			var Random = Initialise();
			var Records = new StringKeyDictionary<int>();
			//****************************************

			foreach (var Pair in YieldRandom(Random, 16))
				Records.Add(Pair.Key, Pair.Value);

			var Inputs = new Queue<KeyValuePair<string, int>>(YieldRandom(Random, 1024));

			//****************************************

			for (var Index = 0; Index < 1024; Index++)
			{
				if (Index % 10 >= 5 && Records.Count > 0)
				{
					var Key = ((IList<KeyValuePair<string, int>>)Records)[Random.Next(0, Records.Count)].Key;

					Records.Remove(Key);
				}
				else
				{
					var Pair = Inputs.Dequeue();

					Records[Pair.Key] = Pair.Value;
				}
			}
		}

		[Test()]//, Repeat(2)]
		public void AddRemoveAll()
		{ //****************************************
			var Random = Initialise();
			var Records = new StringKeyDictionary<int>(1024);
			//****************************************

			foreach (var Pair in YieldRandom(Random, 1024))
				Records.Add(Pair.Key, Pair.Value);

			//****************************************

			while (Records.Count > 0)
			{
				var Key = ((IList<KeyValuePair<string, int>>)Records)[Random.Next(0, Records.Count)].Key;

				Records.Remove(Key);
			}

			foreach (var Pair in YieldRandom(Random, 1024))
				Records.Add(Pair.Key, Pair.Value);
		}

		[Test]
		public void Clear()
		{ //****************************************
			var Random = Initialise();
			var Records = new StringKeyDictionary<int>(1024);
			//****************************************

			foreach (var Pair in YieldRandom(Random, 1024))
				Records.Add(Pair.Key, Pair.Value);

			//****************************************

			Records.Clear();

			Assert.AreEqual(0, Records.Count);
		}

		[Test]
		public void ClearAdd()
		{ //****************************************
			var Random = Initialise();
			var Records = new StringKeyDictionary<int>(1024);
			//****************************************

			foreach (var Pair in YieldRandom(Random, 1024))
				Records.Add(Pair.Key, Pair.Value);

			//****************************************

			Records.Clear();

			foreach (var Pair in YieldRandom(Random, 1024))
				Records.Add(Pair.Key, Pair.Value);
		}

		[Test()]//, Repeat(2)]
		public void EnumerateKeys()
		{ //****************************************
			var Random = Initialise();
			var Records = new StringKeyDictionary<int>();

			var Dictionary = new Dictionary<string, int>(1024);
			//****************************************

			foreach (var Pair in YieldRandom(Random, 1024))
			{
				Dictionary.Add(Pair.Key, Pair.Value);
				Records.Add(Pair.Key, Pair.Value);
			}

			//****************************************

			Assert.AreEqual(1024, Records.Count, "Count incorrect");

			CollectionAssert.AreEquivalent(Dictionary.Keys, Records.Keys, "Collections don't match");

			Thread.Sleep(1);
		}

		[Test()]//, Repeat(2)]
		public void EnumerateValues()
		{ //****************************************
			var Random = Initialise();
			var Records = new StringKeyDictionary<int>();

			var Dictionary = new Dictionary<string, int>(1024);
			//****************************************

			foreach (var Pair in YieldRandom(Random, 1024))
			{
				Dictionary.Add(Pair.Key, Pair.Value);
				Records.Add(Pair.Key, Pair.Value);
			}

			//****************************************

			Assert.AreEqual(1024, Records.Count, "Count incorrect");

			CollectionAssert.AreEquivalent(Dictionary.Values, Records.Values, "Collections don't match");

			Thread.Sleep(1);
		}

		[Test()]
		public void GetIndex()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>();
			//****************************************

			MyRecords["A"] = 42;

			//****************************************

			Assert.AreEqual(new KeyValuePair<string, int>("A", 42), ((IList<KeyValuePair<string, int>>)MyRecords)[0]);
		}

		[Test()]
		public void GetIndexMulti()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>();
			//****************************************

			MyRecords["A"] = 40;
			MyRecords["B"] = 41;
			MyRecords["C"] = 42;
			MyRecords["D"] = 43;
			MyRecords["E"] = 44;

			//****************************************

			Assert.AreEqual(new KeyValuePair<string, int>("C", 42), ((IList<KeyValuePair<string, int>>)MyRecords)[2]);
		}

		[Test()]
		public void GetIndexOutOfRange()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>();
			//****************************************

			MyRecords["A"] = 42;

			//****************************************

			try
			{
				var Pair = ((IList<KeyValuePair<string, int>>)MyRecords)[1];

				Assert.Fail("Key found");
			}
			catch (ArgumentOutOfRangeException)
			{
			}

			try
			{
				var Pair = ((IList<KeyValuePair<string, int>>)MyRecords)[-1];

				Assert.Fail("Key found");
			}
			catch (ArgumentOutOfRangeException)
			{
			}
		}

		[Test()]
		public void GetKey()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>();
			//****************************************

			MyRecords["A"] = 42;

			//****************************************

			Assert.AreEqual(42, MyRecords["A"]);
		}

		[Test()]
		public void GetKeySpan()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>();
			//****************************************

			MyRecords["A"] = 42;

			//****************************************

			Assert.AreEqual(42, MyRecords["A".AsSpan()]);
		}

		[Test()]
		public void GetKeyMissing()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>();
			//****************************************

			MyRecords["A"] = 42;

			//****************************************

			try
			{
				var Pair = MyRecords["B"];

				Assert.Fail("Key found");
			}
			catch (KeyNotFoundException)
			{
			}
		}

		[Test()]
		public void IndexOf()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>();
			//****************************************

			MyRecords["A"] = 42;

			//****************************************

			Assert.AreEqual(0, MyRecords.IndexOf(new KeyValuePair<string, int>("A", 42)));
		}

		[Test()]
		public void IndexOfKey()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>();
			//****************************************

			MyRecords["A"] = 42;

			//****************************************

			Assert.AreEqual(0, MyRecords.IndexOfKey("A"));
		}

		[Test()]
		public void IndexOfKeyMissing()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>();
			//****************************************

			MyRecords["A"] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOfKey("B"));
		}

		[Test()]
		public void IndexOfMissingKey()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>();
			//****************************************

			MyRecords["A"] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOf(new KeyValuePair<string, int>("B", 12)));
		}

		[Test()]
		public void IndexOfMissingValue()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>();
			//****************************************

			MyRecords["A"] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOf(new KeyValuePair<string, int>("A", 30)));
		}

		[Test(), Repeat(2)]
		public void PrePopulate()
		{ //****************************************
			var Random = Initialise();

			var Dictionary = new Dictionary<string, int>(1024);
			//****************************************

			foreach (var Pair in YieldRandom(Random, 1024))
			{
				Dictionary.Add(Pair.Key, Pair.Value);
			}

			//****************************************

			var Records = new StringKeyDictionary<int>(Dictionary);

			//****************************************

			Assert.AreEqual(1024, Records.Count, "Count incorrect");

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match");

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test(), Repeat(2)]
		public void Remove()
		{ //****************************************
			var Random = Initialise();
			var Records = new StringKeyDictionary<int>();

			var Dictionary = new Dictionary<string, int>(1024);
			//****************************************

			foreach (var Pair in YieldRandom(Random, 1024))
			{
				Dictionary.Add(Pair.Key, Pair.Value);
				Records.Add(Pair.Key, Pair.Value);
			}

			//****************************************

			for (var Index = 0; Index < 512; Index++)
			{
				var InnerIndex = Random.Next(Records.Count);

				var Key = Records.Keys[InnerIndex];

				Assert.IsTrue(Records.Remove(Key));
				Assert.IsTrue(Dictionary.Remove(Key));
			}

			//****************************************

			Assert.AreEqual(512, Records.Count, "Count incorrect");

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match");

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test(), Repeat(2)]
		public void RemoveAt()
		{ //****************************************
			var Random = Initialise();
			var Records = new StringKeyDictionary<int>();

			var Dictionary = new Dictionary<string, int>(1024);
			//****************************************

			foreach (var Pair in YieldRandom(Random, 1024))
			{
				Dictionary.Add(Pair.Key, Pair.Value);
				Records.Add(Pair.Key, Pair.Value);
			}

			//****************************************

			for (var Index = 0; Index < 512; Index++)
			{
				var InnerIndex = Random.Next(Records.Count);

				var Key = Records.Keys[InnerIndex];

				Records.RemoveAt(InnerIndex);
				Assert.IsTrue(Dictionary.Remove(Key));
			}

			//****************************************

			Assert.AreEqual(512, Records.Count, "Count incorrect");

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match");

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test(), Repeat(2)]
		public void RemoveAll()
		{ //****************************************
			var Random = Initialise();
			var Records = new StringKeyDictionary<int>();

			var Dictionary = new Dictionary<string, int>(1024);
			//****************************************

			foreach (var Pair in YieldRandom(Random, 1024))
			{
				Dictionary.Add(Pair.Key, Pair.Value);
				Records.Add(Pair.Key, Pair.Value);
			}

			//****************************************

			Records.RemoveAll((pair) => { if (Random.Next() > int.MaxValue / 2) return Dictionary.Remove(pair.Key); return false; });

			//****************************************

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match");

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test(), Repeat(2)]
		public void RemoveRange()
		{ //****************************************
			var Random = Initialise();
			var Records = new StringKeyDictionary<int>();

			var Dictionary = new Dictionary<string, int>(1024);
			//****************************************

			foreach (var Pair in YieldRandom(Random, 1024))
			{
				Dictionary.Add(Pair.Key, Pair.Value);
				Records.Add(Pair.Key, Pair.Value);
			}

			//****************************************

			foreach (var Key in Enumerable.Skip<string>(Records.Keys, 256).Take(256))
				Dictionary.Remove(Key);

			Records.RemoveRange(256, 256);

			//****************************************

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match");

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetValue(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}

			Thread.Sleep(1);
		}

		[Test()]
		public void Replace()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>();
			//****************************************

			MyRecords.Add("A", 1);
			MyRecords.Add("B", 2);
			MyRecords.Add("C", 3);
			MyRecords.Add("D", 4);
			MyRecords["B"] = 84;

			//****************************************

			Assert.AreEqual(84, MyRecords["B"]);
		}

		[Test()]
		public void SetKey()
		{ //****************************************
			var MyRecords = new StringKeyDictionary<int>();
			//****************************************

			MyRecords["A"] = 1;
			MyRecords["B"] = 2;
			MyRecords["C"] = 3;
			MyRecords["D"] = 4;

			MyRecords["B"] = 84;

			//****************************************

			Assert.AreEqual(84, MyRecords["B"]);
		}

		//****************************************

		private Random Initialise()
		{
			var Seed = Environment.TickCount;
			var NewRandom = new Random(Seed);

			Console.WriteLine("Seed: {0}", Seed);

			return NewRandom;
		}

		private IEnumerable<KeyValuePair<string, int>> YieldRandom(Random random, int count)
		{
			var Keys = new HashSet<string>(count);
			var Values = new HashSet<int>(count);

			var Key = new byte[8];

			while (Keys.Count < count)
			{
				random.NextBytes(Key);

				Keys.Add(Convert.ToBase64String(Key));
			}

			while (Values.Count < count)
				Values.Add(random.Next());

			var KeyList = Keys.ToArray();
			var ValueList = Values.ToArray();

			for (var Index = 0; Index < count; Index++)
				yield return new KeyValuePair<string, int>(KeyList[Index], ValueList[count - Index - 1]);
		}

		private KeyValuePair<string, int> ToIntPair((string left, int right) pair) => new KeyValuePair<string, int>(pair.left, pair.right);
	}
}
