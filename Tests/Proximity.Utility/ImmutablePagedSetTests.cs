/****************************************\
 ImmutablePagedSetTests.cs
 Created: 2015-06-25
\****************************************/
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Utility.Tests
{
	[TestFixture]
	public class ImmutablePagedSetTests
	{
		[Test]
		public void SinglePage([Values(8, 16, 32, 48)] int count)
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			var MyContents = GenerateSequential(new DateTime(2000, 1, 1), count).ToArray();
			//****************************************

			MySet = MySet.AddRange(MyContents, true, true);

			//****************************************

			Assert.AreEqual(count, MySet.TotalItems, "Total items are not as expected");
			Assert.IsTrue(MySet.Pages.First().IsStart, "Has no start");
			Assert.IsTrue(MySet.Pages.First().IsFinish, "Has no finish");
			Assert.AreEqual(new DateTime(2000, 1, 1), MySet.Min.Min, "Min out of range");
			Assert.AreEqual(new DateTime(2000, 1, 1), MySet.Min.MinKey, "Min Key out of range");
			Assert.AreEqual(new DateTime(2000, 1, 1).AddDays(count - 1), MySet.Min.Max, "Max out of range");
			Assert.AreEqual(new DateTime(2000, 1, 1).AddDays(count - 1), MySet.Min.MaxKey, "Max Key out of range");

			CollectionAssert.AreEqual(MyContents.OrderBy(item => item.Item1), MySet.Pages.First().Items, "Set does not contain the expected items in the right order");
		}

		[Test]
		public void SinglePageMin([Values(8, 16, 32, 48)] int count)
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			var MyContents = GenerateSequential(new DateTime(2000, 1, 1), count).ToArray();
			//****************************************

			MySet = MySet.AddRangeMin(MyContents, new DateTime(1999, 12, 1), true, true);

			//****************************************

			Assert.AreEqual(count, MySet.TotalItems, "Total items are not as expected");
			Assert.IsTrue(MySet.Pages.First().IsStart, "Has no start");
			Assert.IsTrue(MySet.Pages.First().IsFinish, "Has no finish");

			Assert.AreEqual(new DateTime(1999, 12, 1), MySet.Min.Min, "Min out of range");
			Assert.AreEqual(new DateTime(2000, 1, 1), MySet.Min.MinKey, "Min Key out of range");
			Assert.AreEqual(new DateTime(2000, 1, 1).AddDays(count - 1), MySet.Min.Max, "Max out of range");
			Assert.AreEqual(new DateTime(2000, 1, 1).AddDays(count - 1), MySet.Min.MaxKey, "Max Key out of range");

			CollectionAssert.AreEqual(MyContents.OrderBy(item => item.Item1), MySet.Pages.First().Items, "Set does not contain the expected items in the right order");
		}

		[Test]
		public void SinglePageMax([Values(8, 16, 32, 48)] int count)
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			var MyContents = GenerateSequential(new DateTime(2000, 1, 1), count).ToArray();
			//****************************************

			MySet = MySet.AddRangeMax(MyContents, new DateTime(2000, 3, 1), true, true);

			//****************************************

			Assert.AreEqual(count, MySet.TotalItems, "Total items are not as expected");
			Assert.IsTrue(MySet.Pages.First().IsStart, "Has no start");
			Assert.IsTrue(MySet.Pages.First().IsFinish, "Has no finish");

			Assert.AreEqual(new DateTime(2000, 1, 1), MySet.Min.Min, "Min out of range");
			Assert.AreEqual(new DateTime(2000, 1, 1), MySet.Min.MinKey, "Min Key out of range");
			Assert.AreEqual(new DateTime(2000, 3, 1), MySet.Min.Max, "Max out of range");
			Assert.AreEqual(new DateTime(2000, 1, 1).AddDays(count - 1), MySet.Min.MaxKey, "Max Key out of range");

			CollectionAssert.AreEqual(MyContents.OrderBy(item => item.Item1), MySet.Pages.First().Items, "Set does not contain the expected items in the right order");
		}

		[Test]
		public void SinglePageEmpty()
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			//****************************************
			
			var NewSet = MySet.AddRange(Enumerable.Empty<Tuple<DateTime, int>>(), true, true);

			//****************************************

			Assert.AreEqual(0, NewSet.TotalItems, "Total items are not as expected");
			Assert.AreNotSame(MySet, NewSet, "Set has not been modified");
			Assert.AreEqual(1, NewSet.Pages.Count, "Page was not created");
		}
		
		[Test]
		public void SinglePageAddEmpty()
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			var MyContents = Generate(8, 8).ToArray();
			//****************************************
			
			MySet = MySet.AddRange(MyContents, true, true);
			
			var NewSet = MySet.AddRange(Enumerable.Empty<Tuple<DateTime, int>>(), true, true);

			//****************************************

			Assert.AreEqual(8, NewSet.TotalItems, "Total items are not as expected");
			Assert.AreSame(MySet, NewSet, "Set has been modified");
		}
		
		[Test]
		public void SinglePageNoFinish()
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			//****************************************

			MySet = MySet.AddRange(new[] { Tuple.Create(DateTime.MinValue, 0) }, true, false);

			//****************************************

			Assert.AreEqual(1, MySet.TotalItems, "Total items are not as expected");

			Assert.IsFalse(MySet.Pages.First().IsFinish, "Has finish");
		}

		[Test]
		public void SinglePageNoStart()
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			//****************************************

			MySet = MySet.AddRange(new[] { Tuple.Create(DateTime.MinValue, 0) }, false, true);

			//****************************************

			Assert.AreEqual(1, MySet.TotalItems, "Total items are not as expected");

			Assert.IsFalse(MySet.Pages.First().IsStart, "Has start");
		}

		[Test]
		public void SinglePageAppendFinish([Values(8, 16)] int count)
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			var MyContents = Generate(count, count).OrderBy(item => item.Item1).ToList();
			//****************************************

			MySet = MySet.AddRange(MyContents, true, true);

			var NewItem = Tuple.Create(MyContents[count - 1].Item1.AddDays(1), 0);

			var MyNewSet = MySet.AppendToLatest(NewItem);

			MyContents.Add(NewItem);

			//****************************************

			Assert.AreEqual(count, MySet.TotalItems, "Total items are not as expected");

			Assert.AreEqual(count + 1, MyNewSet.TotalItems, "New total items are not as expected");

			CollectionAssert.AreEqual(MyContents, MyNewSet.Pages.First().Items, "Set does not contain the expected items in the right order");
		}

		[Test]
		public void SinglePageAfter([Values(8, 16, 32, 48)] int count, [Values(2, 3)] int divisor)
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			var MyContents = Generate(count, count).OrderBy(item => item.Item1).ToArray();
			var Index = (count / divisor);
			//****************************************

			MySet = MySet.AddRange(MyContents, true, true);

			var MyResults = MySet.TryReadAfter(MyContents[Index].Item1, null).Results.ToArray();

			//****************************************

			Assert.AreEqual(count - (count / divisor), MyResults.Length, "Length is not as expected");

			CollectionAssert.AreEqual(MyContents.Skip(count / divisor), MyResults, "Set does not contain the expected items in the right order");
		}

		[Test]
		public void SinglePageBefore([Values(8, 16, 32, 48)] int count, [Values(2, 3)] int divisor)
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			var MyContents = Generate(count, count).OrderBy(item => item.Item1).ToArray();
			var Index = (count / divisor) - 1;
			//****************************************

			MySet = MySet.AddRange(MyContents, true, true);

			var MyResults = MySet.TryReadBefore(MyContents[Index].Item1, null).Results.ToArray();

			//****************************************

			Assert.AreEqual(count / divisor, MyResults.Length, "Length is not as expected");

			CollectionAssert.AreEqual(MyContents.Take(count / divisor), MyResults, "Set does not contain the expected items in the right order");
		}

		[Test]
		public void SinglePageOverlap()
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			var MyFirstContents = GenerateSequential(new DateTime(2000, 1, 1), 8).ToArray();
			var MySecondContents = GenerateSequential(new DateTime(2000, 1, 7), 8).ToArray();
			//****************************************
			
			MySet = MySet.AddRange(MyFirstContents, true, true);
			
			MySet = MySet.AddRange(MySecondContents, false, true);

			//****************************************

			Assert.AreEqual(14, MySet.TotalItems, "Total items are not as expected");

			Assert.AreEqual(new DateTime(2000, 1, 1), MySet.Min.Min, "Min out of range");
			Assert.AreEqual(new DateTime(2000, 1, 1), MySet.Min.MinKey, "Min Key out of range");
			Assert.AreEqual(new DateTime(2000, 1, 14), MySet.Min.Max, "Max out of range");
			Assert.AreEqual(new DateTime(2000, 1, 14), MySet.Min.MaxKey, "Max Key out of range");
			
			CollectionAssert.AreEqual(MyFirstContents.Concat(MySecondContents.Skip(2)), MySet.Pages.First().Items, "Set does not contain the expected items");
		}

		[Test]
		public void SinglePageOverlapMin()
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			var MyFirstContents = GenerateSequential(new DateTime(2000, 1, 1), 8).ToArray();
			var MySecondContents = GenerateSequential(new DateTime(2000, 1, 7), 8).ToArray();
			//****************************************

			MySet = MySet.AddRangeMin(MyFirstContents, new DateTime(1999, 12, 1), true, true);

			MySet = MySet.AddRange(MySecondContents, false, true);

			//****************************************

			Assert.AreEqual(14, MySet.TotalItems, "Total items are not as expected");

			Assert.AreEqual(new DateTime(1999, 12, 1), MySet.Min.Min, "Min out of range");
			Assert.AreEqual(new DateTime(2000, 1, 1), MySet.Min.MinKey, "Min Key out of range");
			Assert.AreEqual(new DateTime(2000, 1, 14), MySet.Min.Max, "Max out of range");
			Assert.AreEqual(new DateTime(2000, 1, 14), MySet.Min.MaxKey, "Max Key out of range");
			
			CollectionAssert.AreEqual(MyFirstContents.Concat(MySecondContents.Skip(2)), MySet.Pages.First().Items, "Set does not contain the expected items");
		}

		[Test]
		public void SinglePageOverlapMin2()
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			var MyFirstContents = GenerateSequential(new DateTime(2000, 1, 1), 8).ToArray();
			var MySecondContents = GenerateSequential(new DateTime(2000, 1, 7), 8).ToArray();
			//****************************************

			MySet = MySet.AddRange(MyFirstContents, true, true);

			MySet = MySet.AddRangeMin(MySecondContents, new DateTime(1999, 12, 1), false, true);

			//****************************************

			Assert.AreEqual(14, MySet.TotalItems, "Total items are not as expected");

			Assert.AreEqual(new DateTime(1999, 12, 1), MySet.Min.Min, "Min out of range");
			Assert.AreEqual(new DateTime(2000, 1, 1), MySet.Min.MinKey, "Min Key out of range");
			Assert.AreEqual(new DateTime(2000, 1, 14), MySet.Min.Max, "Max out of range");
			Assert.AreEqual(new DateTime(2000, 1, 14), MySet.Min.MaxKey, "Max Key out of range");

			CollectionAssert.AreEqual(MyFirstContents.Concat(MySecondContents.Skip(2)), MySet.Pages.First().Items, "Set does not contain the expected items");
		}

		[Test]
		public void SinglePageOverlapMax()
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			var MyFirstContents = GenerateSequential(new DateTime(2000, 1, 1), 8).ToArray();
			var MySecondContents = GenerateSequential(new DateTime(2000, 1, 7), 8).ToArray();
			//****************************************

			MySet = MySet.AddRangeMax(MyFirstContents, new DateTime(2000, 1, 31), true, true);

			MySet = MySet.AddRange(MySecondContents, false, true);

			//****************************************

			Assert.AreEqual(14, MySet.TotalItems, "Total items are not as expected");

			Assert.AreEqual(new DateTime(2000, 1, 1), MySet.Min.Min, "Min out of range");
			Assert.AreEqual(new DateTime(2000, 1, 1), MySet.Min.MinKey, "Min Key out of range");
			Assert.AreEqual(new DateTime(2000, 1, 31), MySet.Min.Max, "Max out of range");
			Assert.AreEqual(new DateTime(2000, 1, 14), MySet.Min.MaxKey, "Max Key out of range");

			CollectionAssert.AreEqual(MyFirstContents.Concat(MySecondContents.Skip(2)), MySet.Pages.First().Items, "Set does not contain the expected items");
		}
		
		[Test]
		public void SinglePageOverlapMax2()
		{	//****************************************
			ImmutablePagedSet<DateTime, Tuple<DateTime, int>> MySet = new TestSet();
			var MyFirstContents = GenerateSequential(new DateTime(2000, 1, 1), 8).ToArray();
			var MySecondContents = GenerateSequential(new DateTime(2000, 1, 7), 8).ToArray();
			//****************************************

			MySet = MySet.AddRange(MyFirstContents, true, true);

			MySet = MySet.AddRangeMax(MySecondContents, new DateTime(2000, 1, 31), false, true);

			//****************************************

			Assert.AreEqual(14, MySet.TotalItems, "Total items are not as expected");

			Assert.AreEqual(new DateTime(2000, 1, 1), MySet.Min.Min, "Min out of range");
			Assert.AreEqual(new DateTime(2000, 1, 1), MySet.Min.MinKey, "Min Key out of range");
			Assert.AreEqual(new DateTime(2000, 1, 31), MySet.Min.Max, "Max out of range");
			Assert.AreEqual(new DateTime(2000, 1, 14), MySet.Min.MaxKey, "Max Key out of range");

			CollectionAssert.AreEqual(MyFirstContents.Concat(MySecondContents.Skip(2)), MySet.Pages.First().Items, "Set does not contain the expected items");
		}
		
		//****************************************

		private IEnumerable<Tuple<DateTime, int>> Generate(int seed, int count)
		{
			var MyRandom = new Random(seed);

			while (count-- > 0)
			{
				yield return Tuple.Create(new DateTime((long)(MyRandom.NextDouble() * DateTime.MaxValue.Ticks)), MyRandom.Next());
			}
		}

		private IEnumerable<Tuple<DateTime, int>> GenerateSequential(DateTime startRange, int count)
		{
			var MyRandom = new Random(count);

			while (count-- > 0)
			{
				yield return Tuple.Create(startRange, MyRandom.Next());
				
				startRange = startRange.AddDays(1.0);
			}
		}
		
		//****************************************

		private sealed class TestSet : ImmutablePagedSet<DateTime, Tuple<DateTime, int>>
		{
			public TestSet()
			{
			}

			private TestSet(ImmutableSortedSet<ImmutablePagedSetPage<DateTime, Tuple<DateTime, int>>> pages) : base(pages)
			{
			}

			//****************************************

			protected override ImmutablePagedSet<DateTime, Tuple<DateTime, int>> Create(ImmutableSortedSet<ImmutablePagedSetPage<DateTime, Tuple<DateTime, int>>> pages)
			{
				return new TestSet(pages);
			}

			protected override DateTime GetKey(Tuple<DateTime, int> item)
			{
				return item.Item1;
			}
		}
	}
}
