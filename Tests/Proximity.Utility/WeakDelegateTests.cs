using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
//****************************************

namespace Proximity.Utility.Tests
{
	[TestFixture]
	public class WeakDelegateTests
	{
		[Test]
		public void DelegateZero()
		{	//****************************************
			var MyTarget = new TestClass();
			//****************************************

			var MyDelegate = WeakDelegate.Create(MyTarget.ActionZero);

			MyDelegate();

			//****************************************

			Assert.AreEqual(1, MyTarget.Raised);
		}

		[Test]
		public void DelegateZeroCleanup()
		{
			var MyDelegate = WeakDelegate.Create(new TestClassFail().ActionZero);

			GC.Collect();

			MyDelegate();
		}

		[Test]
		public void DelegateOne()
		{	//****************************************
			var MyTarget = new TestClass();
			//****************************************

			var MyDelegate = WeakDelegate.Create<int>(MyTarget.ActionOne);

			MyDelegate(1);

			//****************************************

			Assert.AreEqual(1, MyTarget.Raised);
		}

		[Test]
		public void DelegateOneCleanup()
		{
			var MyDelegate = WeakDelegate.Create<int>(new TestClassFail().ActionOne);

			GC.Collect();

			MyDelegate(1);
		}

		//****************************************

		private class TestClass
		{	//****************************************
			private int _Raised;
			//****************************************

			public void ActionZero()
			{
				Interlocked.Increment(ref _Raised);
			}

			public void ActionOne(int first)
			{
				Assert.AreEqual(1, 1, "Parameter was not as expected");

				Interlocked.Increment(ref _Raised);
			}

			//****************************************

			public int Raised
			{
				get { return _Raised; }
			}
		}

		private class TestClassFail
		{
			public void ActionZero()
			{
				Assert.Fail("Should not be raised");
			}

			public void ActionOne(int first)
			{
				Assert.Fail("Should not be raised");
			}
		}
	}
}
