using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

#pragma warning disable IDE0028 // Simplify collection initialization

namespace Proximity.Collections.Tests
{
	[TestFixture]
	public sealed class BiDictionaryTests
	{
		[Test]
		public void Add()
		{
			var Random = Initialise();
			var Records = new BiDictionary<int, int>();

			foreach (var (Left, Right) in YieldRandom(Random, 1024))
				Records.Add(Left, Right);

			Validate(Records);
		}

		[Test]
		public void AddCapacity()
		{
			var Random = Initialise();
			var Records = new BiDictionary<int, int>(1024);

			foreach (var (Left, Right) in YieldRandom(Random, 1024))
				Records.Add(Left, Right);

			Validate(Records);
		}

		[Test]
		public void AddCollide()
		{
			var Random = Initialise();
			var Records = new BiDictionary<Collider, Collider>();

			foreach (var (Left, Right) in YieldRandom(Random, 64))
				Records.Add(Left, Right);

			Validate(Records);
		}

		[Test()]
		public void AddExistsLeft()
		{
			var MyRecords = new BiDictionary<int, int>();

			MyRecords.Add(10, 84);


			try
			{
				MyRecords.Add(10, 92);

				Assert.Fail("Add succeeded");
			}
			catch (ArgumentException)
			{
			}
		}

		[Test()]
		public void AddExistsRight()
		{
			var MyRecords = new BiDictionary<int, int>();

			MyRecords.Add(10, 84);

			try
			{
				MyRecords.Add(12, 84);

				Assert.Fail("Add succeeded");
			}
			catch (ArgumentException)
			{
			}
		}

		[Test]
		public void AddRange()
		{
			var Random = Initialise();
			var Records = new BiDictionary<int, int>();

			Records.AddRange(YieldRandom(Random, 1024).Select(ToIntPair));

			Validate(Records);
		}

		[Test]
		public void AddRangeCollide()
		{
			var Random = Initialise();
			var Records = new BiDictionary<Collider, Collider>();

			Records.AddRange(YieldRandom(Random, 64).Select(ToColliderPair));

			Validate(Records);
		}

		[Test()]
		public void AddRangeDuplicateInRange()
		{
			var Records = new BiDictionary<int, int>(64);

			try
			{
				Records.AddRange(new[] { new KeyValuePair<int, int>(1, 1), new KeyValuePair<int, int>(2, 2), new KeyValuePair<int, int>(3, 3), new KeyValuePair<int, int>(1, 4) });

				Assert.Fail("Range succeeded");
			}
			catch (ArgumentException)
			{
			}

			Assert.AreEqual(0, Records.Count, "Items were added");
		}

		[Test()]
		public void AddRangeDuplicateInDictionary()
		{
			var Records = new BiDictionary<int, int>(64);

			Records[9] = 1;
			Records[10] = 2;
			Records[11] = 3;
			Records[12] = 4;

			try
			{
				Records.AddRange(new[] { new KeyValuePair<int, int>(1, 5), new KeyValuePair<int, int>(2, 6), new KeyValuePair<int, int>(3, 7), new KeyValuePair<int, int>(9, 8) });

				Assert.Fail("Range succeeded");
			}
			catch (ArgumentException)
			{
			}

			Assert.AreEqual(4, Records.Count, "Items were added");
		}

		[Test]
		public void AddRangePrePopulated()
		{
			var Random = Initialise();
			var Records = new BiDictionary<int, int>();

			var Input = YieldRandom(Random, 1024).Select(ToIntPair).ToArray();

			for (var Index = 0; Index < 512; Index++)
				Records.Add(Input[Index]);

			Records.AddRange(Input.Skip(512));

			Validate(Records);
		}

		[Test]
		public void AddRangePrePopulatedCollide()
		{
			var Random = Initialise();
			var Records = new BiDictionary<Collider, Collider>();

			var Input = YieldRandom(Random, 1024).Select(ToColliderPair).ToArray();

			for (var Index = 0; Index < 512; Index++)
				Records.Add(Input[Index]);

			Records.AddRange(Input.Skip(512));

			Validate(Records);
		}

		[Test()]//, Repeat(2)]
		public void AddRemove()
		{
			var Random = Initialise();
			var Records = new BiDictionary<int, int>();

			var Inputs = YieldRandom(Random, 1024).ToQueue();

			foreach (var (Left, Right) in YieldRandom(Random, 16))
				Records.Add(Left, Right);

			for (var Index = 0; Index < 1024; Index++)
			{
				if (Index % 10 >= 5 && Records.Count > 0)
				{
					var Key = Records.Get(Random.Next(0, Records.Count)).Key;

					Records.Remove(Key);
				}
				else
				{
					Records.Add(ToIntPair(Inputs.Dequeue()));
				}
			}
		}

		[Test()]//, Repeat(2)]
		public void AddRemoveCollide()
		{
			var Random = Initialise();
			var Records = new BiDictionary<Collider, Collider>();

			foreach (var (Left, Right) in YieldRandom(Random, 16))
				Records.Add(Left, Right);

			var Inputs = YieldRandom(Random, 1024).ToQueue();

			for (var Index = 0; Index < 1024; Index++)
			{
				if (Index % 10 >= 5 && Records.Count > 0)
				{
					var Key = Records.Get(Random.Next(0, Records.Count)).Key;

					Records.Remove(Key);
				}
				else
				{
					Records.Add(ToColliderPair(Inputs.Dequeue()));
				}
			}
		}

		[Test()]//, Repeat(2)]
		public void AddRemoveAllAdd()
		{
			var Random = Initialise();
			var Records = new BiDictionary<int, int>();

			foreach (var (Left, Right) in YieldRandom(Random, 1024))
				Records.Add(Left, Right);

			while (Records.Count > 0)
			{
				var Key = Records.Get(Random.Next(0, Records.Count)).Key;

				Records.Remove(Key);
			}

			foreach (var (Left, Right) in YieldRandom(Random, 1024))
				Records.Add(Left, Right);
		}

		[Test]
		public void Clear()
		{
			var Random = Initialise();
			var Records = new BiDictionary<int, int>();

			foreach (var (Left, Right) in YieldRandom(Random, 1024))
				Records.Add(Left, Right);

			Records.Clear();

			Assert.AreEqual(0, Records.Count);
		}

		[Test]
		public void ClearAdd()
		{
			var Random = Initialise();
			var Records = new BiDictionary<int, int>();

			foreach (var (Left, Right) in YieldRandom(Random, 1024))
				Records.Add(Left, Right);

			Records.Clear();

			foreach (var (Left, Right) in YieldRandom(Random, 1024))
				Records.Add(Left, Right);

			Validate(Records);
		}

		[Test()]
		public void GetIndex()
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(new KeyValuePair<int, int>(10, 42), ((IList<KeyValuePair<int, int>>)MyRecords)[0]);
		}

		[Test()]
		public void GetIndexMulti()
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
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
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
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
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(42, MyRecords[10]);
			Assert.AreEqual(10, MyRecords.Inverse[42]);
		}

		[Test()]
		public void GetKeyMissing()
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			try
			{
				var Pair = MyRecords[42];

				Assert.Fail("Key found");
			}
			catch (KeyNotFoundException)
			{
			}

			try
			{
				var Pair = MyRecords.Inverse[10];

				Assert.Fail("Key found");
			}
			catch (KeyNotFoundException)
			{
			}
		}

		[Test()]
		public void IndexOf()
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(0, MyRecords.IndexOf(new KeyValuePair<int, int>(10, 42)));
		}

		[Test()]
		public void IndexOfLeft()
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(0, MyRecords.IndexOfLeft(10));
		}

		[Test()]
		public void IndexOfRight()
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(0, MyRecords.IndexOfRight(42));
		}

		[Test()]
		public void IndexOfLeftCollideMissing()
		{ //****************************************
			var MyRecords = new BiDictionary<Collider, Collider>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOfLeft(11));
		}

		[Test()]
		public void IndexOfLeftCollideMissing2()
		{ //****************************************
			var MyRecords = new BiDictionary<Collider, Collider>();
			//****************************************

			MyRecords[10] = 42;
			MyRecords[int.MaxValue] = 43;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOfLeft(11));
		}

		[Test()]
		public void IndexOfLeftMissing()
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOfLeft(11));
		}

		[Test()]
		public void IndexOfRightCollideMissing()
		{ //****************************************
			var MyRecords = new BiDictionary<Collider, Collider>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOfRight(11));
		}

		[Test()]
		public void IndexOfRightCollideMissing2()
		{ //****************************************
			var MyRecords = new BiDictionary<Collider, Collider>();
			//****************************************

			MyRecords[10] = 42;
			MyRecords[int.MaxValue] = 43;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOfRight(11));
		}

		[Test()]
		public void IndexOfRightMissing()
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOfRight(11));
		}

		[Test()]
		public void IndexOfMissingLeft()
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOf(new KeyValuePair<int, int>(11, 42)));
		}

		[Test()]
		public void IndexOfMissingRight()
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
			//****************************************

			MyRecords[10] = 42;

			//****************************************

			Assert.AreEqual(-1, MyRecords.IndexOf(new KeyValuePair<int, int>(10, 30)));
		}

		[Test(), Repeat(2)]
		public void PrePopulate()
		{ //****************************************
			var Seed = Environment.TickCount;
			var Random = new Random(Seed);
			//****************************************

			var Dictionary = new Dictionary<int, int>(1024);

			foreach (var (Left, Right) in YieldRandom(Random, 1024))
				Dictionary.Add(Left, Right);

			var Records = new BiDictionary<int, int>(Dictionary);

			//****************************************

			Assert.AreEqual(1024, Records.Count, "Count incorrect. Bad Seed was {0}", Seed);

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match. Bad Seed was {0}", Seed);

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetRight(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}
		}

		[Test()]
		public void RemoveLeft()
		{ //****************************************
			var Seed = Environment.TickCount;
			var Random = new Random(Seed);
			//****************************************

			var Dictionary = new Dictionary<int, int>(1024);

			foreach (var (Left, Right) in YieldRandom(Random, 1024))
				Dictionary.Add(Left, Right);

			var Records = new BiDictionary<int, int>(Dictionary);

			//****************************************

			for (var Index = 0; Index < 512; Index++)
			{
				var InnerIndex = Random.Next(Records.Count);

				var Key = Records.Lefts[InnerIndex];

				Assert.IsTrue(Records.Remove(Key));
				Assert.IsTrue(Dictionary.Remove(Key));
			}

			//****************************************

			Assert.AreEqual(512, Records.Count, "Count incorrect. Bad Seed was {0}", Seed);

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match. Bad Seed was {0}", Seed);

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetRight(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}
		}

		[Test()]
		public void RemoveRight()
		{ //****************************************
			var Seed = Environment.TickCount;
			var Random = new Random(Seed);
			//****************************************

			var Dictionary = new Dictionary<int, int>(1024);

			foreach (var (Left, Right) in YieldRandom(Random, 1024))
				Dictionary.Add(Left, Right);

			var Records = BiDictionary.FromInverse(Dictionary);

			//****************************************

			for (var Index = 0; Index < 512; Index++)
			{
				var InnerIndex = Random.Next(Records.Count);

				var Key = Records.Rights[InnerIndex];

				Assert.IsTrue(Records.Inverse.Remove(Key));
				Assert.IsTrue(Dictionary.Remove(Key));
			}

			//****************************************

			Assert.AreEqual(512, Records.Count, "Count incorrect. Bad Seed was {0}", Seed);

			CollectionAssert.AreEquivalent(Dictionary, Records.Inverse, "Collections don't match. Bad Seed was {0}", Seed);

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetLeft(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}
		}

		[Test()]
		public void RemoveAt()
		{ //****************************************
			var Seed = Environment.TickCount;
			var Random = new Random(Seed);
			//****************************************

			var Dictionary = new Dictionary<int, int>(1024);

			foreach (var (Left, Right) in YieldRandom(Random, 1024))
				Dictionary.Add(Left, Right);

			var Records = new BiDictionary<int, int>(Dictionary);

			//****************************************

			for (var Index = 0; Index < 512; Index++)
			{
				var InnerIndex = Random.Next(Records.Count);

				var Key = Records.Lefts[InnerIndex];

				Records.RemoveAt(InnerIndex);
				Assert.IsTrue(Dictionary.Remove(Key));
			}

			//****************************************

			Assert.AreEqual(512, Records.Count, "Count incorrect. Bad Seed was {0}", Seed);

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match. Bad Seed was {0}", Seed);

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetRight(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}
		}

		[Test()]
		public void RemoveAtSingle()
		{ //****************************************
			var Seed = Environment.TickCount;
			var Random = new Random(Seed);
			//****************************************

			var Dictionary = new Dictionary<int, int>(16);

			foreach (var (Left, Right) in YieldRandom(Random, 16))
				Dictionary.Add(Left, Right);

			var Records = new BiDictionary<int, int>(Dictionary);

			//****************************************

			var InnerIndex = Random.Next(Records.Count);

			var Key = Records.Lefts[InnerIndex];

			Records.RemoveAt(InnerIndex);
			Assert.IsTrue(Dictionary.Remove(Key));
			Assert.IsFalse(Records.ContainsLeft(Key));

			//****************************************

			Assert.AreEqual(15, Records.Count, "Count incorrect. Bad Seed was {0}", Seed);

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match. Bad Seed was {0}", Seed);

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetRight(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}
		}

		[Test(), Repeat(2)]
		public void RemoveAll()
		{ //****************************************
			var Seed = Environment.TickCount;
			var Random = new Random(Seed);
			//****************************************

			var Dictionary = new Dictionary<int, int>(1024);

			foreach (var (Left, Right) in YieldRandom(Random, 1024))
				Dictionary.Add(Left, Right);

			var Records = new BiDictionary<int, int>(Dictionary);

			//****************************************

			Records.RemoveAll((pair) => { if (Random.Next() > int.MaxValue / 2) return Dictionary.Remove(pair.Key); return false; });

			//****************************************

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match. Bad Seed was {0}", Seed);

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetRight(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}
		}

		[Test(), Repeat(2)]
		public void RemoveRange()
		{ //****************************************
			var Seed = Environment.TickCount;
			var Random = new Random(Seed);
			//****************************************

			var Dictionary = new Dictionary<int, int>(1024);

			foreach (var (Left, Right) in YieldRandom(Random, 1024))
				Dictionary.Add(Left, Right);

			var Records = new BiDictionary<int, int>(Dictionary);

			//****************************************

			foreach (var MyResult in Records.Skip(256).Take(256))
				Dictionary.Remove(MyResult.Key);

			Records.RemoveRange(256, 256);

			//****************************************

			CollectionAssert.AreEquivalent(Dictionary, Records, "Collections don't match. Bad Seed was {0}", Seed);

			foreach (var MyPair in Dictionary)
			{
				Assert.IsTrue(Records.TryGetRight(MyPair.Key, out var Value));
				Assert.AreEqual(MyPair.Value, Value);
			}
		}

		[Test()]
		public void Replace()
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
			//****************************************

			MyRecords.Add(9, 1);
			MyRecords.Add(12, 2);
			MyRecords.Add(10, 3);
			MyRecords.Add(11, 4);

			//****************************************

			try
			{
				MyRecords[10] = 4;

				Assert.Fail("Did not fail");
			}
			catch (ArgumentException)
			{
			}
		}

		[Test()]
		public void ReplaceCollide()
		{ //****************************************
			var MyRecords = new BiDictionary<Collider, int>();
			//****************************************

			MyRecords.Add(9, 1);
			MyRecords.Add(12, 2);
			MyRecords.Add(10, 3);
			MyRecords.Add(11, 4);

			//****************************************

			try
			{
				MyRecords[10] = 4;

				Assert.Fail("Did not fail");
			}
			catch (ArgumentException)
			{
			}
		}

		[Test()]
		public void SetKeyLeft()
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
			//****************************************

			MyRecords[9] = 1;
			MyRecords[12] = 2;
			MyRecords[10] = 3;
			MyRecords[11] = 4;

			MyRecords[10] = 5;

			//****************************************

			Assert.AreEqual(4, MyRecords.Count);
			Assert.AreEqual(5, MyRecords[10]);
			Assert.AreEqual(10, MyRecords.Inverse[5]);
		}

		[Test()]
		public void SetKeyRight()
		{ //****************************************
			var MyRecords = new BiDictionary<int, int>();
			//****************************************

			MyRecords[9] = 1;
			MyRecords[12] = 2;
			MyRecords[10] = 3;
			MyRecords[11] = 4;

			MyRecords[13] = 4;

			//****************************************

			Assert.AreEqual(4, MyRecords.Count);
			Assert.AreEqual(4, MyRecords[13]);
			Assert.AreEqual(13, MyRecords.Inverse[4]);
		}

		[Test()]
		public void SetKeyCollideLeft()
		{ //****************************************
			var MyRecords = new BiDictionary<Collider, int>();
			//****************************************

			MyRecords[9] = 1;
			MyRecords[12] = 2;
			MyRecords[10] = 3;
			MyRecords[11] = 4;

			MyRecords[10] = 5;

			//****************************************

			Assert.AreEqual(4, MyRecords.Count);
			Assert.AreEqual(5, MyRecords[10]);
			Assert.AreEqual(10, MyRecords.Inverse[5].Value);
		}

		[Test()]
		public void SetKeyCollideRight()
		{ //****************************************
			var MyRecords = new BiDictionary<Collider, int>();
			//****************************************

			MyRecords[9] = 1;
			MyRecords[12] = 2;
			MyRecords[10] = 3;
			MyRecords[11] = 4;

			MyRecords[13] = 4;

			//****************************************

			Assert.AreEqual(4, MyRecords.Count);
			Assert.AreEqual(4, MyRecords[13]);
			Assert.AreEqual(13, MyRecords.Inverse[4].Value);
		}

		//****************************************

		private void Validate(BiDictionary<int, int> dictionary)
		{
			CollectionAssert.AreEquivalent(dictionary.Lefts, dictionary.Rights);

			foreach (var (Left, Right) in dictionary)
			{
				Assert.AreEqual(Left, dictionary.Inverse[Right]);
				Assert.AreEqual(Right, dictionary[Left]);
			}
		}

		private void Validate(BiDictionary<Collider, Collider> dictionary)
		{
			CollectionAssert.AreEquivalent(dictionary.Lefts, dictionary.Rights);

			foreach (var (Left, Right) in dictionary)
			{
				Assert.AreEqual(Left, dictionary.Inverse[Right]);
				Assert.AreEqual(Right, dictionary[Left]);
			}
		}

		private Random Initialise()
		{
			var Seed = Environment.TickCount;
			var NewRandom = new Random(Seed);

			Console.WriteLine("Seed: {0}", Seed);

			return NewRandom;
		}

		private IEnumerable<(int left, int right)> YieldRandom(Random random, int count)
		{
			var Values = new HashSet<int>(count);

			while (Values.Count < count)
				Values.Add(random.Next());

			var List = Values.ToArray();

			for (var Index = 0; Index < count; Index++)
				yield return (List[Index], List[count - Index - 1]);
		}

		private KeyValuePair<int, int> ToIntPair((int left, int right) pair) => new KeyValuePair<int, int>(pair.left, pair.right);

		private KeyValuePair<Collider, Collider> ToColliderPair((int left, int right) pair) => new KeyValuePair<Collider, Collider>(pair.left, pair.right);
	}
}
