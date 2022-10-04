using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
//****************************************

namespace Proximity.Collections.Tests
{
	/// <summary>
	/// Tests the functionality of the Binary Search Extensions class
	/// </summary>
	[TestFixture()]
	public class BinarySearchExtensionTests
	{	//****************************************
		private IList<DateTime> _SortedRecords;
		//****************************************
		
		public BinarySearchExtensionTests()
		{
		}
		
		//****************************************
		
		[OneTimeSetUp()]
		public void Setup()
		{
			var SortedRecords = new List<DateTime>();

			_SortedRecords = SortedRecords;
			
			#region Records Data
			SortedRecords.Add(new DateTime(2012, 7, 11));
			SortedRecords.Add(new DateTime(2012, 7, 10));
			SortedRecords.Add(new DateTime(2012, 7, 9));
			SortedRecords.Add(new DateTime(2012, 7, 6));
			SortedRecords.Add(new DateTime(2012, 7, 5));
			SortedRecords.Add(new DateTime(2012, 7, 4));
			SortedRecords.Add(new DateTime(2012, 7, 3));
			SortedRecords.Add(new DateTime(2012, 7, 2));
			SortedRecords.Add(new DateTime(2012, 6, 29));
			SortedRecords.Add(new DateTime(2012, 6, 28));
			SortedRecords.Add(new DateTime(2012, 6, 27));
			SortedRecords.Add(new DateTime(2012, 6, 26));
			SortedRecords.Add(new DateTime(2012, 6, 25));
			SortedRecords.Add(new DateTime(2012, 6, 22));
			SortedRecords.Add(new DateTime(2012, 6, 21));
			SortedRecords.Add(new DateTime(2012, 6, 20));
			SortedRecords.Add(new DateTime(2012, 6, 19));
			SortedRecords.Add(new DateTime(2012, 6, 18));
			SortedRecords.Add(new DateTime(2012, 6, 15));
			SortedRecords.Add(new DateTime(2012, 6, 14));
			SortedRecords.Add(new DateTime(2012, 6, 13));
			SortedRecords.Add(new DateTime(2012, 6, 12));
			SortedRecords.Add(new DateTime(2012, 6, 8));
			SortedRecords.Add(new DateTime(2012, 6, 7));
			SortedRecords.Add(new DateTime(2012, 6, 6));
			SortedRecords.Add(new DateTime(2012, 6, 5));
			SortedRecords.Add(new DateTime(2012, 6, 4));
			SortedRecords.Add(new DateTime(2012, 6, 1));
			SortedRecords.Add(new DateTime(2012, 5, 31));
			SortedRecords.Add(new DateTime(2012, 5, 30));
			SortedRecords.Add(new DateTime(2012, 5, 29));
			SortedRecords.Add(new DateTime(2012, 5, 28));
			SortedRecords.Add(new DateTime(2012, 5, 25));
			SortedRecords.Add(new DateTime(2012, 5, 24));
			SortedRecords.Add(new DateTime(2012, 5, 23));
			SortedRecords.Add(new DateTime(2012, 5, 22));
			SortedRecords.Add(new DateTime(2012, 5, 21));
			SortedRecords.Add(new DateTime(2012, 5, 18));
			SortedRecords.Add(new DateTime(2012, 5, 17));
			SortedRecords.Add(new DateTime(2012, 5, 16));
			SortedRecords.Add(new DateTime(2012, 5, 15));
			SortedRecords.Add(new DateTime(2012, 5, 14));
			SortedRecords.Add(new DateTime(2012, 5, 11));
			SortedRecords.Add(new DateTime(2012, 5, 10));
			SortedRecords.Add(new DateTime(2012, 5, 9));
			SortedRecords.Add(new DateTime(2012, 5, 8));
			SortedRecords.Add(new DateTime(2012, 5, 7));
			SortedRecords.Add(new DateTime(2012, 5, 4));
			SortedRecords.Add(new DateTime(2012, 5, 3));
			SortedRecords.Add(new DateTime(2012, 5, 2));
			SortedRecords.Add(new DateTime(2012, 5, 1));
			SortedRecords.Add(new DateTime(2012, 4, 30));
			SortedRecords.Add(new DateTime(2012, 4, 27));
			SortedRecords.Add(new DateTime(2012, 4, 26));
			SortedRecords.Add(new DateTime(2012, 4, 24));
			SortedRecords.Add(new DateTime(2012, 4, 23));
			SortedRecords.Add(new DateTime(2012, 4, 20));
			SortedRecords.Add(new DateTime(2012, 4, 19));
			SortedRecords.Add(new DateTime(2012, 4, 18));
			SortedRecords.Add(new DateTime(2012, 4, 17));
			SortedRecords.Add(new DateTime(2012, 4, 16));
			SortedRecords.Add(new DateTime(2012, 4, 13));
			SortedRecords.Add(new DateTime(2012, 4, 12));
			SortedRecords.Add(new DateTime(2012, 4, 11));
			SortedRecords.Add(new DateTime(2012, 4, 10));
			SortedRecords.Add(new DateTime(2012, 4, 5));
			SortedRecords.Add(new DateTime(2012, 4, 4));
			SortedRecords.Add(new DateTime(2012, 4, 3));
			SortedRecords.Add(new DateTime(2012, 4, 2));
			SortedRecords.Add(new DateTime(2012, 3, 30));
			SortedRecords.Add(new DateTime(2012, 3, 29));
			SortedRecords.Add(new DateTime(2012, 3, 28));
			SortedRecords.Add(new DateTime(2012, 3, 27));
			SortedRecords.Add(new DateTime(2012, 3, 26));
			SortedRecords.Add(new DateTime(2012, 3, 23));
			SortedRecords.Add(new DateTime(2012, 3, 22));
			SortedRecords.Add(new DateTime(2012, 3, 21));
			SortedRecords.Add(new DateTime(2012, 3, 20));
			SortedRecords.Add(new DateTime(2012, 3, 19));
			SortedRecords.Add(new DateTime(2012, 3, 16));
			SortedRecords.Add(new DateTime(2012, 3, 15));
			SortedRecords.Add(new DateTime(2012, 3, 14));
			SortedRecords.Add(new DateTime(2012, 3, 13));
			SortedRecords.Add(new DateTime(2012, 3, 12));
			SortedRecords.Add(new DateTime(2012, 3, 9));
			SortedRecords.Add(new DateTime(2012, 3, 8));
			SortedRecords.Add(new DateTime(2012, 3, 7));
			SortedRecords.Add(new DateTime(2012, 3, 6));
			SortedRecords.Add(new DateTime(2012, 3, 5));
			SortedRecords.Add(new DateTime(2012, 3, 2));
			SortedRecords.Add(new DateTime(2012, 3, 1));
			SortedRecords.Add(new DateTime(2012, 2, 29));
			SortedRecords.Add(new DateTime(2012, 2, 28));
			SortedRecords.Add(new DateTime(2012, 2, 27));
			SortedRecords.Add(new DateTime(2012, 2, 24));
			SortedRecords.Add(new DateTime(2012, 2, 23));
			SortedRecords.Add(new DateTime(2012, 2, 22));
			SortedRecords.Add(new DateTime(2012, 2, 21));
			SortedRecords.Add(new DateTime(2012, 2, 20));
			SortedRecords.Add(new DateTime(2012, 2, 17));
			SortedRecords.Add(new DateTime(2012, 2, 16));
			SortedRecords.Add(new DateTime(2012, 2, 15));
			SortedRecords.Add(new DateTime(2012, 2, 14));
			SortedRecords.Add(new DateTime(2012, 2, 13));
			SortedRecords.Add(new DateTime(2012, 2, 10));
			SortedRecords.Add(new DateTime(2012, 2, 9));
			SortedRecords.Add(new DateTime(2012, 2, 8));
			SortedRecords.Add(new DateTime(2012, 2, 7));
			SortedRecords.Add(new DateTime(2012, 2, 6));
			SortedRecords.Add(new DateTime(2012, 2, 3));
			SortedRecords.Add(new DateTime(2012, 2, 2));
			SortedRecords.Add(new DateTime(2012, 2, 1));
			SortedRecords.Add(new DateTime(2012, 1, 31));
			SortedRecords.Add(new DateTime(2012, 1, 30));
			SortedRecords.Add(new DateTime(2012, 1, 27));
			SortedRecords.Add(new DateTime(2012, 1, 25));
			SortedRecords.Add(new DateTime(2012, 1, 24));
			SortedRecords.Add(new DateTime(2012, 1, 23));
			SortedRecords.Add(new DateTime(2012, 1, 20));
			SortedRecords.Add(new DateTime(2012, 1, 19));
			SortedRecords.Add(new DateTime(2012, 1, 18));
			SortedRecords.Add(new DateTime(2012, 1, 17));
			SortedRecords.Add(new DateTime(2012, 1, 16));
			SortedRecords.Add(new DateTime(2012, 1, 13));
			SortedRecords.Add(new DateTime(2012, 1, 12));
			SortedRecords.Add(new DateTime(2012, 1, 11));
			SortedRecords.Add(new DateTime(2012, 1, 10));
			SortedRecords.Add(new DateTime(2012, 1, 9));
			SortedRecords.Add(new DateTime(2012, 1, 6));
			SortedRecords.Add(new DateTime(2012, 1, 5));
			SortedRecords.Add(new DateTime(2012, 1, 4));
			SortedRecords.Add(new DateTime(2012, 1, 3));
			SortedRecords.Add(new DateTime(2011, 12, 30));
			SortedRecords.Add(new DateTime(2011, 12, 29));
			SortedRecords.Add(new DateTime(2011, 12, 28));
			SortedRecords.Add(new DateTime(2011, 12, 23));
			SortedRecords.Add(new DateTime(2011, 12, 22));
			SortedRecords.Add(new DateTime(2011, 12, 21));
			SortedRecords.Add(new DateTime(2011, 12, 20));
			SortedRecords.Add(new DateTime(2011, 12, 19));
			SortedRecords.Add(new DateTime(2011, 12, 16));
			SortedRecords.Add(new DateTime(2011, 12, 15));
			SortedRecords.Add(new DateTime(2011, 12, 14));
			SortedRecords.Add(new DateTime(2011, 12, 13));
			SortedRecords.Add(new DateTime(2011, 12, 12));
			SortedRecords.Add(new DateTime(2011, 12, 9));
			SortedRecords.Add(new DateTime(2011, 12, 8));
			SortedRecords.Add(new DateTime(2011, 12, 7));
			SortedRecords.Add(new DateTime(2011, 12, 6));
			SortedRecords.Add(new DateTime(2011, 12, 5));
			SortedRecords.Add(new DateTime(2011, 12, 2));
			SortedRecords.Add(new DateTime(2011, 12, 1));
			SortedRecords.Add(new DateTime(2011, 11, 30));
			SortedRecords.Add(new DateTime(2011, 11, 29));
			SortedRecords.Add(new DateTime(2011, 11, 28));
			SortedRecords.Add(new DateTime(2011, 11, 25));
			SortedRecords.Add(new DateTime(2011, 11, 24));
			SortedRecords.Add(new DateTime(2011, 11, 23));
			SortedRecords.Add(new DateTime(2011, 11, 22));
			SortedRecords.Add(new DateTime(2011, 11, 21));
			SortedRecords.Add(new DateTime(2011, 11, 18));
			SortedRecords.Add(new DateTime(2011, 11, 17));
			SortedRecords.Add(new DateTime(2011, 11, 16));
			SortedRecords.Add(new DateTime(2011, 11, 15));
			SortedRecords.Add(new DateTime(2011, 11, 14));
			SortedRecords.Add(new DateTime(2011, 11, 11));
			SortedRecords.Add(new DateTime(2011, 11, 10));
			SortedRecords.Add(new DateTime(2011, 11, 9));
			SortedRecords.Add(new DateTime(2011, 11, 8));
			SortedRecords.Add(new DateTime(2011, 11, 7));
			SortedRecords.Add(new DateTime(2011, 11, 4));
			SortedRecords.Add(new DateTime(2011, 11, 3));
			SortedRecords.Add(new DateTime(2011, 11, 2));
			SortedRecords.Add(new DateTime(2011, 11, 1));
			SortedRecords.Add(new DateTime(2011, 10, 31));
			SortedRecords.Add(new DateTime(2011, 10, 28));
			SortedRecords.Add(new DateTime(2011, 10, 27));
			SortedRecords.Add(new DateTime(2011, 10, 26));
			SortedRecords.Add(new DateTime(2011, 10, 25));
			SortedRecords.Add(new DateTime(2011, 10, 24));
			SortedRecords.Add(new DateTime(2011, 10, 21));
			SortedRecords.Add(new DateTime(2011, 10, 20));
			SortedRecords.Add(new DateTime(2011, 10, 19));
			SortedRecords.Add(new DateTime(2011, 10, 18));
			SortedRecords.Add(new DateTime(2011, 10, 17));
			SortedRecords.Add(new DateTime(2011, 10, 14));
			SortedRecords.Add(new DateTime(2011, 10, 13));
			SortedRecords.Add(new DateTime(2011, 10, 12));
			SortedRecords.Add(new DateTime(2011, 10, 11));
			SortedRecords.Add(new DateTime(2011, 10, 10));
			SortedRecords.Add(new DateTime(2011, 10, 7));
			SortedRecords.Add(new DateTime(2011, 10, 6));
			SortedRecords.Add(new DateTime(2011, 10, 5));
			SortedRecords.Add(new DateTime(2011, 10, 4));
			SortedRecords.Add(new DateTime(2011, 10, 3));
			SortedRecords.Add(new DateTime(2011, 9, 30));
			SortedRecords.Add(new DateTime(2011, 9, 29));
			SortedRecords.Add(new DateTime(2011, 9, 28));
			SortedRecords.Add(new DateTime(2011, 9, 27));
			#endregion

			SortedRecords.Sort(ReverseComparer<DateTime>.Default);
		}
		
		//****************************************
		
		[Test()]
		public void SeekBefore()
		{
			Assert.AreEqual(-1, _SortedRecords.NearestBelow(new DateTime(2012, 7, 15), ReverseComparer<DateTime>.Default));
		}
		
		[Test()]
		public void SeekAtStart()
		{
			Assert.AreEqual(0, _SortedRecords.NearestBelow(new DateTime(2012, 7, 11), ReverseComparer<DateTime>.Default));
		}
		
		[Test()]
		public void SeekWeekend1()
		{
			var Index = _SortedRecords.IndexOf(new DateTime(2012, 7, 9));
			
			Assert.AreNotEqual(-1, Index, "Missing Index");
			
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelow(new DateTime(2012, 7, 9), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelow(new DateTime(2012, 7, 8), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelow(new DateTime(2012, 7, 7), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 1, _SortedRecords.NearestBelow(new DateTime(2012, 7, 6), ReverseComparer<DateTime>.Default));
		}
		
		[Test()]
		public void SeekWeekend2()
		{
			var Index = _SortedRecords.IndexOf(new DateTime(2011, 12, 5));
			
			Assert.AreNotEqual(-1, Index, "Missing Index");
			
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelow(new DateTime(2011, 12, 5), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelow(new DateTime(2011, 12, 4), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelow(new DateTime(2011, 12, 3), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 1, _SortedRecords.NearestBelow(new DateTime(2011, 12, 2), ReverseComparer<DateTime>.Default));
		}
		
		[Test()]
		public void SeekWeek1()
		{
			var Index = _SortedRecords.IndexOf(new DateTime(2012, 6, 22));
			
			Assert.AreNotEqual(-1, Index, "Missing Index");
			
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelow(new DateTime(2012, 6, 22), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 1, _SortedRecords.NearestBelow(new DateTime(2012, 6, 21), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 2, _SortedRecords.NearestBelow(new DateTime(2012, 6, 20), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 3, _SortedRecords.NearestBelow(new DateTime(2012, 6, 19), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 4, _SortedRecords.NearestBelow(new DateTime(2012, 6, 18), ReverseComparer<DateTime>.Default));
		}
		
		[Test()]
		public void SeekWeek2()
		{
			var Index = _SortedRecords.IndexOf(new DateTime(2012, 6, 22));
			
			Assert.AreNotEqual(-1, Index, "Missing Index");
			
			Assert.AreEqual(Index - 1, _SortedRecords.NearestBelow(new DateTime(2012, 6, 25), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelow(new DateTime(2012, 6, 22), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 1, _SortedRecords.NearestBelow(new DateTime(2012, 6, 21), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 2, _SortedRecords.NearestBelow(new DateTime(2012, 6, 20), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 3, _SortedRecords.NearestBelow(new DateTime(2012, 6, 19), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 4, _SortedRecords.NearestBelow(new DateTime(2012, 6, 18), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 5, _SortedRecords.NearestBelow(new DateTime(2012, 6, 15), ReverseComparer<DateTime>.Default));
		}
		
		[Test()]
		public void SeekWeek3()
		{
			var Index = _SortedRecords.IndexOf(new DateTime(2011, 10, 28));
			
			Assert.AreNotEqual(-1, Index, "Missing Index");
			
			Assert.AreEqual(Index - 1, _SortedRecords.NearestBelow(new DateTime(2011, 10, 31), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index - 1, _SortedRecords.NearestBelow(new DateTime(2011, 10, 30), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index - 1, _SortedRecords.NearestBelow(new DateTime(2011, 10, 29), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelow(new DateTime(2011, 10, 28), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 1, _SortedRecords.NearestBelow(new DateTime(2011, 10, 27), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 2, _SortedRecords.NearestBelow(new DateTime(2011, 10, 26), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 3, _SortedRecords.NearestBelow(new DateTime(2011, 10, 25), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 4, _SortedRecords.NearestBelow(new DateTime(2011, 10, 24), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 4, _SortedRecords.NearestBelow(new DateTime(2011, 10, 23), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 4, _SortedRecords.NearestBelow(new DateTime(2011, 10, 22), ReverseComparer<DateTime>.Default));
			Assert.AreEqual(Index + 5, _SortedRecords.NearestBelow(new DateTime(2011, 10, 21), ReverseComparer<DateTime>.Default));
		}
		
		[Test()]
		public void SeekAtEnd()
		{
			Assert.AreEqual(_SortedRecords.Count - 1, _SortedRecords.NearestBelow(new DateTime(2011, 9, 27), ReverseComparer<DateTime>.Default));
		}
		
		[Test()]
		public void SeekAfter()
		{
			Assert.AreEqual(198, _SortedRecords.NearestBelow(new DateTime(2011, 9, 20), ReverseComparer<DateTime>.Default));
		}

		//****************************************

		[Test(), MaxTime(2000), Repeat(20)]
		public void PartialSort([Values(0, 1, 10, 255, 256, 512, 999, 1000)] int index)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new List<int>(1024);
			//****************************************

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			var MySortedRecords = MyRecords.ToArray();

			//****************************************

			MyRecords.PartialSort(index, 24, Comparer<int>.Default);

			//****************************************

			Array.Sort(MySortedRecords);

			Assert.IsTrue(MySortedRecords.Skip(index).Take(24).SequenceEquivalent(MyRecords.Skip(index).Take(24)), "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}
		
		[Test(), MaxTime(2000)]
		public void PartialSort([Values(0, 1, 10, 255, 256, 512, 999, 1000)] int index, [Values(192796144)] int seed)
		{	//****************************************
			var MyRandom = new Random(seed);
			var MyRecords = new List<int>(1024);
			//****************************************

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			var MySortedRecords = MyRecords.ToArray();

			//****************************************

			MyRecords.PartialSort(index, 24, Comparer<int>.Default);

			//****************************************

			Array.Sort(MySortedRecords);

			Assert.IsTrue(MySortedRecords.Skip(index).Take(24).SequenceEquivalent(MyRecords.Skip(index).Take(24)));
		}
	}
}

