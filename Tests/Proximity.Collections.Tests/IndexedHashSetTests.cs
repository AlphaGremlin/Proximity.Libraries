using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

#pragma warning disable IDE0028 // Simplify collection initialization

namespace Proximity.Collections.Tests
{
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
				int Key, Value;

				do
				{
					Key = MyRandom.Next();
				} while (ReferenceSet.Contains(Key));

				ReferenceSet.Add(Key);
				TestSet.Add(Key);
			}

			//****************************************

			Assert.AreEqual(1024, TestSet.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			Assert.IsTrue(ReferenceSet.SequenceEquivalent(TestSet), "Collections don't match. Bad Seed was {0}", MySeed);

			ReferenceSet.SymmetricExceptWith(TestSet);

			CollectionAssert.IsEmpty(ReferenceSet);

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
				int Key, Value;

				do
				{
					Key = MyRandom.Next();
				} while (ReferenceSet.Contains(Key));

				ReferenceSet.Add(Key);
				TestSet.Add(Key);
			}

			//****************************************

			Assert.AreEqual(1024, TestSet.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			Assert.IsTrue(ReferenceSet.SequenceEquivalent(TestSet), "Collections don't match. Bad Seed was {0}", MySeed);

			ReferenceSet.SymmetricExceptWith(TestSet);

			CollectionAssert.IsEmpty(ReferenceSet);

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
				int Value;

				do
				{
					Key = new Collider(MyRandom.Next());
				} while (ReferenceSet.Contains(Key));

				ReferenceSet.Add(Key);
				TestSet.Add(Key);
			}

			//****************************************

			Assert.AreEqual(64, TestSet.Count, "Count incorrect. Bad Seed was {0}", MySeed);

			Assert.IsTrue(ReferenceSet.SequenceEquivalent(TestSet), "Collections don't match. Bad Seed was {0}", MySeed);

			ReferenceSet.SymmetricExceptWith(TestSet);

			CollectionAssert.IsEmpty(ReferenceSet);

			Thread.Sleep(1);
		}

		[Test()]
		public void AddExists()
		{ //****************************************
			var MyRecords = new IndexedHashSet<int>();
			//****************************************

			MyRecords.Add(10);

			//****************************************

			Assert.AreEqual(-1, MyRecords.Add(10));
		}


	}
}
