/****************************************\
 ObservableListViewTests.cs
 Created: 2016-05-26
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
	[TestFixture]
	public class ObservableListViewTests
	{
		[Test(), Timeout(2000), Repeat(20)]
		public void Comparer()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, ReverseComparer<int>.Default);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords, ReverseComparer<int>.Default);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(20)]
		public void PrePopulate()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(20)]
		public void PrePopulateFilter()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512);

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(5)]
		public void PrePopulateMaximum([Values(512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, maximum);

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(20)]
		public void Populate()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(20)]
		public void PopulateFilter()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(5)]
		public void PopulateMaximum([Values(512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, maximum);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(5)]
		public void PopulateMaximumFilter([Values(512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512, maximum);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(20)]
		public void PopulateBeginEnd()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			MyRecords.BeginUpdate();

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			MyRecords.EndUpdate();

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(20)]
		public void PopulateBeginEndFilter()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512);

			MyRecords.BeginUpdate();

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			MyRecords.EndUpdate();

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(5)]
		public void PopulateBeginEndMaximum([Values(512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, maximum);

			MyRecords.BeginUpdate();

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			MyRecords.EndUpdate();

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(20)]
		public void Replace()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (int Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(20)]
		public void ReplaceFilter()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (int Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(5)]
		public void ReplaceMaximum([Values(512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, maximum);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (int Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(10)]
		public void ReplaceMaximumFilter([Values(512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512, maximum);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (int Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(10)]
		public void ReplaceMaximumFilter2([Values(512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterMoreThan512, maximum);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (int Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)] = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterMoreThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(20)]
		public void Remove()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (int Index = 0; Index < 512; Index++)
			{
				MyRecords.RemoveAt(MyRandom.Next(MyRecords.Count));
			}

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(20)]
		public void RemoveFilter()
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (int Index = 0; Index < 512; Index++)
			{
				MyRecords.RemoveAt(MyRandom.Next(MyRecords.Count));
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(5)]
		public void RemoveMaximum([Values(512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, maximum);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (int Index = 0; Index < 512; Index++)
			{
				MyRecords.RemoveAt(MyRandom.Next(MyRecords.Count));
			}

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(5)]
		public void RemoveMaximumFilter([Values(512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterLessThan512, maximum);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (int Index = 0; Index < 512; Index++)
			{
				MyRecords.RemoveAt(MyRandom.Next(MyRecords.Count));
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), Timeout(2000), Repeat(5)]
		public void RemoveMaximumFilter2([Values(512, 1024, 2048)] int maximum)
		{	//****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<int>(1024);
			//****************************************

			var MyView = new ObservableListView<int>(MyRecords, FilterMoreThan512, maximum);

			for (int Index = 0; Index < 1024; Index++)
				MyRecords.Add(MyRandom.Next(short.MaxValue));

			for (int Index = 0; Index < 512; Index++)
			{
				MyRecords.RemoveAt(MyRandom.Next(MyRecords.Count));
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterMoreThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		//****************************************

		private bool FilterLessThan512(int value)
		{
			return value < 512;
		}

		private bool FilterMoreThan512(int value)
		{
			return value > 512;
		}
	}
}
