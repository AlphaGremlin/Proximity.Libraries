/****************************************\
 ListExtensionTests.cs
 Created: 2012-07-17
\****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Description of MyClass.
	/// </summary>
	[TestFixture()]
	public class ListExtensionTests
	{	//****************************************
		private List<DateTime> _SortedRecords;
		//****************************************
		
		public ListExtensionTests()
		{
		}
		
		//****************************************
		
		[OneTimeSetUp()]
		public void Setup()
		{
			_SortedRecords = new List<DateTime>();
			
			#region Records Data
			_SortedRecords.Add(new DateTime(2012, 7, 11));
			_SortedRecords.Add(new DateTime(2012, 7, 10));
			_SortedRecords.Add(new DateTime(2012, 7, 9));
			_SortedRecords.Add(new DateTime(2012, 7, 6));
			_SortedRecords.Add(new DateTime(2012, 7, 5));
			_SortedRecords.Add(new DateTime(2012, 7, 4));
			_SortedRecords.Add(new DateTime(2012, 7, 3));
			_SortedRecords.Add(new DateTime(2012, 7, 2));
			_SortedRecords.Add(new DateTime(2012, 6, 29));
			_SortedRecords.Add(new DateTime(2012, 6, 28));
			_SortedRecords.Add(new DateTime(2012, 6, 27));
			_SortedRecords.Add(new DateTime(2012, 6, 26));
			_SortedRecords.Add(new DateTime(2012, 6, 25));
			_SortedRecords.Add(new DateTime(2012, 6, 22));
			_SortedRecords.Add(new DateTime(2012, 6, 21));
			_SortedRecords.Add(new DateTime(2012, 6, 20));
			_SortedRecords.Add(new DateTime(2012, 6, 19));
			_SortedRecords.Add(new DateTime(2012, 6, 18));
			_SortedRecords.Add(new DateTime(2012, 6, 15));
			_SortedRecords.Add(new DateTime(2012, 6, 14));
			_SortedRecords.Add(new DateTime(2012, 6, 13));
			_SortedRecords.Add(new DateTime(2012, 6, 12));
			_SortedRecords.Add(new DateTime(2012, 6, 8));
			_SortedRecords.Add(new DateTime(2012, 6, 7));
			_SortedRecords.Add(new DateTime(2012, 6, 6));
			_SortedRecords.Add(new DateTime(2012, 6, 5));
			_SortedRecords.Add(new DateTime(2012, 6, 4));
			_SortedRecords.Add(new DateTime(2012, 6, 1));
			_SortedRecords.Add(new DateTime(2012, 5, 31));
			_SortedRecords.Add(new DateTime(2012, 5, 30));
			_SortedRecords.Add(new DateTime(2012, 5, 29));
			_SortedRecords.Add(new DateTime(2012, 5, 28));
			_SortedRecords.Add(new DateTime(2012, 5, 25));
			_SortedRecords.Add(new DateTime(2012, 5, 24));
			_SortedRecords.Add(new DateTime(2012, 5, 23));
			_SortedRecords.Add(new DateTime(2012, 5, 22));
			_SortedRecords.Add(new DateTime(2012, 5, 21));
			_SortedRecords.Add(new DateTime(2012, 5, 18));
			_SortedRecords.Add(new DateTime(2012, 5, 17));
			_SortedRecords.Add(new DateTime(2012, 5, 16));
			_SortedRecords.Add(new DateTime(2012, 5, 15));
			_SortedRecords.Add(new DateTime(2012, 5, 14));
			_SortedRecords.Add(new DateTime(2012, 5, 11));
			_SortedRecords.Add(new DateTime(2012, 5, 10));
			_SortedRecords.Add(new DateTime(2012, 5, 9));
			_SortedRecords.Add(new DateTime(2012, 5, 8));
			_SortedRecords.Add(new DateTime(2012, 5, 7));
			_SortedRecords.Add(new DateTime(2012, 5, 4));
			_SortedRecords.Add(new DateTime(2012, 5, 3));
			_SortedRecords.Add(new DateTime(2012, 5, 2));
			_SortedRecords.Add(new DateTime(2012, 5, 1));
			_SortedRecords.Add(new DateTime(2012, 4, 30));
			_SortedRecords.Add(new DateTime(2012, 4, 27));
			_SortedRecords.Add(new DateTime(2012, 4, 26));
			_SortedRecords.Add(new DateTime(2012, 4, 24));
			_SortedRecords.Add(new DateTime(2012, 4, 23));
			_SortedRecords.Add(new DateTime(2012, 4, 20));
			_SortedRecords.Add(new DateTime(2012, 4, 19));
			_SortedRecords.Add(new DateTime(2012, 4, 18));
			_SortedRecords.Add(new DateTime(2012, 4, 17));
			_SortedRecords.Add(new DateTime(2012, 4, 16));
			_SortedRecords.Add(new DateTime(2012, 4, 13));
			_SortedRecords.Add(new DateTime(2012, 4, 12));
			_SortedRecords.Add(new DateTime(2012, 4, 11));
			_SortedRecords.Add(new DateTime(2012, 4, 10));
			_SortedRecords.Add(new DateTime(2012, 4, 5));
			_SortedRecords.Add(new DateTime(2012, 4, 4));
			_SortedRecords.Add(new DateTime(2012, 4, 3));
			_SortedRecords.Add(new DateTime(2012, 4, 2));
			_SortedRecords.Add(new DateTime(2012, 3, 30));
			_SortedRecords.Add(new DateTime(2012, 3, 29));
			_SortedRecords.Add(new DateTime(2012, 3, 28));
			_SortedRecords.Add(new DateTime(2012, 3, 27));
			_SortedRecords.Add(new DateTime(2012, 3, 26));
			_SortedRecords.Add(new DateTime(2012, 3, 23));
			_SortedRecords.Add(new DateTime(2012, 3, 22));
			_SortedRecords.Add(new DateTime(2012, 3, 21));
			_SortedRecords.Add(new DateTime(2012, 3, 20));
			_SortedRecords.Add(new DateTime(2012, 3, 19));
			_SortedRecords.Add(new DateTime(2012, 3, 16));
			_SortedRecords.Add(new DateTime(2012, 3, 15));
			_SortedRecords.Add(new DateTime(2012, 3, 14));
			_SortedRecords.Add(new DateTime(2012, 3, 13));
			_SortedRecords.Add(new DateTime(2012, 3, 12));
			_SortedRecords.Add(new DateTime(2012, 3, 9));
			_SortedRecords.Add(new DateTime(2012, 3, 8));
			_SortedRecords.Add(new DateTime(2012, 3, 7));
			_SortedRecords.Add(new DateTime(2012, 3, 6));
			_SortedRecords.Add(new DateTime(2012, 3, 5));
			_SortedRecords.Add(new DateTime(2012, 3, 2));
			_SortedRecords.Add(new DateTime(2012, 3, 1));
			_SortedRecords.Add(new DateTime(2012, 2, 29));
			_SortedRecords.Add(new DateTime(2012, 2, 28));
			_SortedRecords.Add(new DateTime(2012, 2, 27));
			_SortedRecords.Add(new DateTime(2012, 2, 24));
			_SortedRecords.Add(new DateTime(2012, 2, 23));
			_SortedRecords.Add(new DateTime(2012, 2, 22));
			_SortedRecords.Add(new DateTime(2012, 2, 21));
			_SortedRecords.Add(new DateTime(2012, 2, 20));
			_SortedRecords.Add(new DateTime(2012, 2, 17));
			_SortedRecords.Add(new DateTime(2012, 2, 16));
			_SortedRecords.Add(new DateTime(2012, 2, 15));
			_SortedRecords.Add(new DateTime(2012, 2, 14));
			_SortedRecords.Add(new DateTime(2012, 2, 13));
			_SortedRecords.Add(new DateTime(2012, 2, 10));
			_SortedRecords.Add(new DateTime(2012, 2, 9));
			_SortedRecords.Add(new DateTime(2012, 2, 8));
			_SortedRecords.Add(new DateTime(2012, 2, 7));
			_SortedRecords.Add(new DateTime(2012, 2, 6));
			_SortedRecords.Add(new DateTime(2012, 2, 3));
			_SortedRecords.Add(new DateTime(2012, 2, 2));
			_SortedRecords.Add(new DateTime(2012, 2, 1));
			_SortedRecords.Add(new DateTime(2012, 1, 31));
			_SortedRecords.Add(new DateTime(2012, 1, 30));
			_SortedRecords.Add(new DateTime(2012, 1, 27));
			_SortedRecords.Add(new DateTime(2012, 1, 25));
			_SortedRecords.Add(new DateTime(2012, 1, 24));
			_SortedRecords.Add(new DateTime(2012, 1, 23));
			_SortedRecords.Add(new DateTime(2012, 1, 20));
			_SortedRecords.Add(new DateTime(2012, 1, 19));
			_SortedRecords.Add(new DateTime(2012, 1, 18));
			_SortedRecords.Add(new DateTime(2012, 1, 17));
			_SortedRecords.Add(new DateTime(2012, 1, 16));
			_SortedRecords.Add(new DateTime(2012, 1, 13));
			_SortedRecords.Add(new DateTime(2012, 1, 12));
			_SortedRecords.Add(new DateTime(2012, 1, 11));
			_SortedRecords.Add(new DateTime(2012, 1, 10));
			_SortedRecords.Add(new DateTime(2012, 1, 9));
			_SortedRecords.Add(new DateTime(2012, 1, 6));
			_SortedRecords.Add(new DateTime(2012, 1, 5));
			_SortedRecords.Add(new DateTime(2012, 1, 4));
			_SortedRecords.Add(new DateTime(2012, 1, 3));
			_SortedRecords.Add(new DateTime(2011, 12, 30));
			_SortedRecords.Add(new DateTime(2011, 12, 29));
			_SortedRecords.Add(new DateTime(2011, 12, 28));
			_SortedRecords.Add(new DateTime(2011, 12, 23));
			_SortedRecords.Add(new DateTime(2011, 12, 22));
			_SortedRecords.Add(new DateTime(2011, 12, 21));
			_SortedRecords.Add(new DateTime(2011, 12, 20));
			_SortedRecords.Add(new DateTime(2011, 12, 19));
			_SortedRecords.Add(new DateTime(2011, 12, 16));
			_SortedRecords.Add(new DateTime(2011, 12, 15));
			_SortedRecords.Add(new DateTime(2011, 12, 14));
			_SortedRecords.Add(new DateTime(2011, 12, 13));
			_SortedRecords.Add(new DateTime(2011, 12, 12));
			_SortedRecords.Add(new DateTime(2011, 12, 9));
			_SortedRecords.Add(new DateTime(2011, 12, 8));
			_SortedRecords.Add(new DateTime(2011, 12, 7));
			_SortedRecords.Add(new DateTime(2011, 12, 6));
			_SortedRecords.Add(new DateTime(2011, 12, 5));
			_SortedRecords.Add(new DateTime(2011, 12, 2));
			_SortedRecords.Add(new DateTime(2011, 12, 1));
			_SortedRecords.Add(new DateTime(2011, 11, 30));
			_SortedRecords.Add(new DateTime(2011, 11, 29));
			_SortedRecords.Add(new DateTime(2011, 11, 28));
			_SortedRecords.Add(new DateTime(2011, 11, 25));
			_SortedRecords.Add(new DateTime(2011, 11, 24));
			_SortedRecords.Add(new DateTime(2011, 11, 23));
			_SortedRecords.Add(new DateTime(2011, 11, 22));
			_SortedRecords.Add(new DateTime(2011, 11, 21));
			_SortedRecords.Add(new DateTime(2011, 11, 18));
			_SortedRecords.Add(new DateTime(2011, 11, 17));
			_SortedRecords.Add(new DateTime(2011, 11, 16));
			_SortedRecords.Add(new DateTime(2011, 11, 15));
			_SortedRecords.Add(new DateTime(2011, 11, 14));
			_SortedRecords.Add(new DateTime(2011, 11, 11));
			_SortedRecords.Add(new DateTime(2011, 11, 10));
			_SortedRecords.Add(new DateTime(2011, 11, 9));
			_SortedRecords.Add(new DateTime(2011, 11, 8));
			_SortedRecords.Add(new DateTime(2011, 11, 7));
			_SortedRecords.Add(new DateTime(2011, 11, 4));
			_SortedRecords.Add(new DateTime(2011, 11, 3));
			_SortedRecords.Add(new DateTime(2011, 11, 2));
			_SortedRecords.Add(new DateTime(2011, 11, 1));
			_SortedRecords.Add(new DateTime(2011, 10, 31));
			_SortedRecords.Add(new DateTime(2011, 10, 28));
			_SortedRecords.Add(new DateTime(2011, 10, 27));
			_SortedRecords.Add(new DateTime(2011, 10, 26));
			_SortedRecords.Add(new DateTime(2011, 10, 25));
			_SortedRecords.Add(new DateTime(2011, 10, 24));
			_SortedRecords.Add(new DateTime(2011, 10, 21));
			_SortedRecords.Add(new DateTime(2011, 10, 20));
			_SortedRecords.Add(new DateTime(2011, 10, 19));
			_SortedRecords.Add(new DateTime(2011, 10, 18));
			_SortedRecords.Add(new DateTime(2011, 10, 17));
			_SortedRecords.Add(new DateTime(2011, 10, 14));
			_SortedRecords.Add(new DateTime(2011, 10, 13));
			_SortedRecords.Add(new DateTime(2011, 10, 12));
			_SortedRecords.Add(new DateTime(2011, 10, 11));
			_SortedRecords.Add(new DateTime(2011, 10, 10));
			_SortedRecords.Add(new DateTime(2011, 10, 7));
			_SortedRecords.Add(new DateTime(2011, 10, 6));
			_SortedRecords.Add(new DateTime(2011, 10, 5));
			_SortedRecords.Add(new DateTime(2011, 10, 4));
			_SortedRecords.Add(new DateTime(2011, 10, 3));
			_SortedRecords.Add(new DateTime(2011, 9, 30));
			_SortedRecords.Add(new DateTime(2011, 9, 29));
			_SortedRecords.Add(new DateTime(2011, 9, 28));
			_SortedRecords.Add(new DateTime(2011, 9, 27));
			#endregion
			
			_SortedRecords.Sort(ReverseComparer<DateTime>.Default);
		}
		
		//****************************************
		
		[Test()]
		public void SeekBefore()
		{
			Assert.AreEqual(0, _SortedRecords.NearestBelowDescending(new DateTime(2012, 7, 15)));
		}
		
		[Test()]
		public void SeekAtStart()
		{
			Assert.AreEqual(0, _SortedRecords.NearestBelowDescending(new DateTime(2012, 7, 11)));
		}
		
		[Test()]
		public void SeekWeekend1()
		{
			var Index = _SortedRecords.IndexOf(new DateTime(2012, 7, 9));
			
			Assert.AreNotEqual(-1, Index, "Missing Index");
			
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelowDescending(new DateTime(2012, 7, 9)));
			Assert.AreEqual(Index + 1, _SortedRecords.NearestBelowDescending(new DateTime(2012, 7, 8)));
			Assert.AreEqual(Index + 1, _SortedRecords.NearestBelowDescending(new DateTime(2012, 7, 7)));
			Assert.AreEqual(Index + 1, _SortedRecords.NearestBelowDescending(new DateTime(2012, 7, 6)));
		}
		
		[Test()]
		public void SeekWeekend2()
		{
			var Index = _SortedRecords.IndexOf(new DateTime(2011, 12, 5));
			
			Assert.AreNotEqual(-1, Index, "Missing Index");
			
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelowDescending(new DateTime(2011, 12, 5)));
			Assert.AreEqual(Index + 1, _SortedRecords.NearestBelowDescending(new DateTime(2011, 12, 4)));
			Assert.AreEqual(Index + 1, _SortedRecords.NearestBelowDescending(new DateTime(2011, 12, 3)));
			Assert.AreEqual(Index + 1, _SortedRecords.NearestBelowDescending(new DateTime(2011, 12, 2)));
		}
		
		[Test()]
		public void SeekWeek1()
		{
			var Index = _SortedRecords.IndexOf(new DateTime(2012, 6, 22));
			
			Assert.AreNotEqual(-1, Index, "Missing Index");
			
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelowDescending(new DateTime(2012, 6, 22)));
			Assert.AreEqual(Index + 1, _SortedRecords.NearestBelowDescending(new DateTime(2012, 6, 21)));
			Assert.AreEqual(Index + 2, _SortedRecords.NearestBelowDescending(new DateTime(2012, 6, 20)));
			Assert.AreEqual(Index + 3, _SortedRecords.NearestBelowDescending(new DateTime(2012, 6, 19)));
			Assert.AreEqual(Index + 4, _SortedRecords.NearestBelowDescending(new DateTime(2012, 6, 18)));
		}
		
		[Test()]
		public void SeekWeek2()
		{
			var Index = _SortedRecords.IndexOf(new DateTime(2012, 6, 22));
			
			Assert.AreNotEqual(-1, Index, "Missing Index");
			
			Assert.AreEqual(Index - 1, _SortedRecords.NearestBelowDescending(new DateTime(2012, 6, 25)));
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelowDescending(new DateTime(2012, 6, 22)));
			Assert.AreEqual(Index + 1, _SortedRecords.NearestBelowDescending(new DateTime(2012, 6, 21)));
			Assert.AreEqual(Index + 2, _SortedRecords.NearestBelowDescending(new DateTime(2012, 6, 20)));
			Assert.AreEqual(Index + 3, _SortedRecords.NearestBelowDescending(new DateTime(2012, 6, 19)));
			Assert.AreEqual(Index + 4, _SortedRecords.NearestBelowDescending(new DateTime(2012, 6, 18)));
			Assert.AreEqual(Index + 5, _SortedRecords.NearestBelowDescending(new DateTime(2012, 6, 15)));
		}
		
		[Test()]
		public void SeekWeek3()
		{
			var Index = _SortedRecords.IndexOf(new DateTime(2011, 10, 28));
			
			Assert.AreNotEqual(-1, Index, "Missing Index");
			
			Assert.AreEqual(Index - 1, _SortedRecords.NearestBelowDescending(new DateTime(2011, 10, 31)));
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelowDescending(new DateTime(2011, 10, 30)));
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelowDescending(new DateTime(2011, 10, 29)));
			Assert.AreEqual(Index + 0, _SortedRecords.NearestBelowDescending(new DateTime(2011, 10, 28)));
			Assert.AreEqual(Index + 1, _SortedRecords.NearestBelowDescending(new DateTime(2011, 10, 27)));
			Assert.AreEqual(Index + 2, _SortedRecords.NearestBelowDescending(new DateTime(2011, 10, 26)));
			Assert.AreEqual(Index + 3, _SortedRecords.NearestBelowDescending(new DateTime(2011, 10, 25)));
			Assert.AreEqual(Index + 4, _SortedRecords.NearestBelowDescending(new DateTime(2011, 10, 24)));
			Assert.AreEqual(Index + 5, _SortedRecords.NearestBelowDescending(new DateTime(2011, 10, 23)));
			Assert.AreEqual(Index + 5, _SortedRecords.NearestBelowDescending(new DateTime(2011, 10, 22)));
			Assert.AreEqual(Index + 5, _SortedRecords.NearestBelowDescending(new DateTime(2011, 10, 21)));
		}
		
		[Test()]
		public void SeekAtEnd()
		{
			Assert.AreEqual(_SortedRecords.Count - 1, _SortedRecords.NearestBelowDescending(new DateTime(2011, 9, 27)));
		}
		
		[Test()]
		public void SeekAfter()
		{
			Assert.AreEqual(-1, _SortedRecords.NearestBelowDescending(new DateTime(2011, 9, 20)));
		}

		//****************************************

		[Test(), MaxTime(2000), Repeat(20)]
		public void PartialSort([Values(0, 1, 10, 255, 256, 512, 999, 1000)] int index)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new List<int>(1024);
			//****************************************

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			var MySortedRecords = MyRecords.ToArray();

			//****************************************

			MyRecords.PartialSort(index, 24, Comparer<int>.Default);

			//****************************************

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords.Skip(index).Take(24), MyRecords.Skip(index).Take(24), "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}
		
		[Test(), MaxTime(2000)]
		public void PartialSort([Values(0, 1, 10, 255, 256, 512, 999, 1000)] int index, [Values(192796144)] int seed)
		{	//****************************************
			var MyRandom = new Random(seed);
			var MyRecords = new List<int>(1024);
			//****************************************

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			var MySortedRecords = MyRecords.ToArray();

			//****************************************

			MyRecords.PartialSort(index, 24, Comparer<int>.Default);

			//****************************************

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords.Skip(index).Take(24), MyRecords.Skip(index).Take(24));
		}
	}
}

