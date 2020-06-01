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

			var Inputs = YieldRandom(Random, 512).ToQueue();

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

			var Inputs = YieldRandom(Random, 512).ToQueue();

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
