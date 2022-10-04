using System;
using System.Collections.Generic;
using System.Collections.Observable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework;
//****************************************

namespace Proximity.Collections.Tests
{
	[TestFixture]
	public class ObservableListViewDynamicTests
	{
		[Test(), MaxTime(1000)]
		public void Populate()
		{ //****************************************
			var MyRecords = new ObservableList<TestObject>(1024);
			//****************************************

			MyRecords.Add(new TestObject(1024));

			//****************************************

			var MyView = new ObservableListViewDynamic<TestObject>(MyRecords, (Func<TestObject, TestObject>)Clone);

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			
			CollectionAssert.AreEqual(MySortedRecords, MyView);
		}

		[Test(), MaxTime(1000)]
		public void Change()
		{ //****************************************
			var MyRecords = new ObservableList<TestObject>(1024);
			//****************************************

			MyRecords.Add(new TestObject(1024));

			//****************************************

			var MyView = new ObservableListViewDynamic<TestObject>(MyRecords, (Func<TestObject, TestObject>)Clone);

			MyRecords[0].Value = 512;

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);

			CollectionAssert.AreEqual(MySortedRecords, MyView);
		}

		[Test(), MaxTime(2000), Repeat(20)]
		public void ChangeMultiple()
		{ //****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<TestObject>(1024);
			//****************************************

			var MyView = new ObservableListViewDynamic<TestObject>(MyRecords, (Func<TestObject, TestObject>)Clone);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(new TestObject(MyRandom.Next(short.MaxValue)));

			for (var Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)].Value = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);

			Assert.IsTrue(MySortedRecords.SequenceEqual(MyView), "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(20)]
		public void ChangeMultipleFilter()
		{ //****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<TestObject>(1024);
			//****************************************

			var MyView = new ObservableListViewDynamic<TestObject>(MyRecords, Clone, (IComparer<TestObject>)null, FilterLessThan512);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(new TestObject(MyRandom.Next(short.MaxValue)));

			for (var Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)].Value = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);

			Assert.IsTrue(MySortedRecords.SequenceEqual(MyView), "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(20)]
		public void ChangeMultipleMaximum([Values(256, 512, 1024)] int maximum)
		{ //****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<TestObject>(1024);
			//****************************************

			var MyView = new ObservableListViewDynamic<TestObject>(MyRecords, Clone, (IComparer<TestObject>)null, null, maximum);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(new TestObject(MyRandom.Next(short.MaxValue)));

			for (var Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)].Value = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			Assert.IsTrue(MySortedRecords.SequenceEqual(MyView), "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		[Test(), MaxTime(2000), Repeat(20)]
		public void ChangeMultipleMaximumFilter([Values(256, 512, 1024)] int maximum)
		{ //****************************************
			var MySeed = Environment.TickCount;
			var MyRandom = new Random(MySeed);
			var MyRecords = new ObservableList<TestObject>(1024);
			//****************************************

			var MyView = new ObservableListViewDynamic<TestObject>(MyRecords, Clone, (IComparer<TestObject>)null, FilterLessThan512, maximum);

			for (var Index = 0; Index < 1024; Index++)
				MyRecords.Add(new TestObject(MyRandom.Next(short.MaxValue)));

			for (var Index = 0; Index < 1024; Index++)
			{
				MyRecords[MyRandom.Next(1024)].Value = MyRandom.Next(short.MaxValue);
			}

			//****************************************

			var MySortedRecords = MyRecords.Where(FilterLessThan512).ToArray();

			Array.Sort(MySortedRecords);
			if (MySortedRecords.Length > maximum)
				Array.Resize(ref MySortedRecords, maximum);

			CollectionAssert.AreEqual(MySortedRecords, MyView, "Bad Seed was {0}", MySeed);

			Thread.Sleep(1);
		}

		//****************************************

		private bool FilterLessThan512(TestObject value)
		{
			return value.Value < 512;
		}

		private bool FilterMoreThan512(TestObject value)
		{
			return value.Value > 512;
		}

		private static TestObject Clone(TestObject source)
		{
			return new TestObject(source.Value);
		}

		//****************************************

		private class TestObject : INotifyPropertyChanged, IComparable<TestObject>, IEquatable<TestObject>
		{ //****************************************
			private int _Value;
			//****************************************

			public TestObject(int value)
			{
				_Value = value;
			}

			//****************************************



			public override string ToString()
			{
				return _Value.ToString();
			}

			//****************************************

			int IComparable<TestObject>.CompareTo(TestObject other)
			{
				return _Value.CompareTo(other._Value);
			}

			bool IEquatable<TestObject>.Equals(TestObject other)
			{
				return _Value == other._Value;
			}

			//****************************************

			/// <summary>
			/// Raises the <see cref="PropertyChanged"/> event
			/// </summary>
			/// <param name="propertyName">The name of the property. If omitted, defaults to the caller member name</param>
			protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
			{
				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}

			//****************************************

			public event PropertyChangedEventHandler PropertyChanged;

			public int Value
			{
				get { return _Value; }
				set { if (_Value == value) return; _Value = value; RaisePropertyChanged(); }
			}
		}
	}
}
