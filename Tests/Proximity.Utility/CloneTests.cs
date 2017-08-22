/****************************************\
 EqualityTests.cs
 Created: 2012-07-17
\****************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Proximity.Utility.Reflection;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Tests the functionality of the Cloning class
	/// </summary>
	[TestFixture()]
	public class CloneTests
	{
		[Test()]
		public void BasicObjectClone()
		{
			var Left = new CustomObjectBasic(0, decimal.Zero, null);
			var Right = Cloning.Clone(Left);
			
			Assert.AreEqual(Left, Right, "Are not equal");
		}
		
		[Test()]
		public void BasicObjectCloneWithValues()
		{
			var Left = new CustomObjectBasic(12345678, 12345678.90m, new object());
			var Right = Cloning.Clone(Left);
			
			Assert.AreEqual(Left, Right, "Are not equal");
		}

		[Test()]
		public void ComplexObjectClone()
		{
			var Left = new CustomObjectComplex(new CustomObjectBasic(0, decimal.Zero, new object()), decimal.MaxValue, new object());
			var Right = Cloning.Clone(Left);

			Assert.AreEqual(Left, Right, "Are not equal");
		}

		[Test()]
		public void ReadOnlyObjectClone()
		{
			var Left = new CustomObjectReadOnly(0, decimal.Zero, null);
			var Right = Cloning.Clone(Left);
			
			Assert.AreEqual(Left, Right, "Are not equal");
		}
		
		[Test()]
		public void ReadOnlyObjectCloneWithValues()
		{
			var Left = new CustomObjectReadOnly(12345678, 12345678.90m, new object());
			var Right = Cloning.Clone(Left);
			
			Assert.AreEqual(Left, Right, "Are not equal");
		}

		[Test()]
		public void ReadOnlyObjectInheritedClone()
		{
			var Left = new CustomObjectInheritedReadOnly(0, decimal.Zero, null, 'A');
			var Right = Cloning.Clone(Left);

			Assert.AreEqual(Left, Right, "Are not equal");
		}

		[Test()]
		public void BasicObjectCloneTo()
		{
			var Left = new CustomObjectBasic(12345678, 12345678.90m, new object());
			var Right = new CustomObjectBasic();
			Cloning.CloneTo(Left, Right);
			
			Assert.AreEqual(Left, Right, "Are not equal");
		}
		
		[Test()]
		public void ReadOnlyObjectCloneTo()
		{
			var Left = new CustomObjectReadOnly(12345678, 12345678.90m, new object());
			var Right = new CustomObjectReadOnly(0, decimal.Zero, null);
			Cloning.CloneTo(Left, Right);
			
			Assert.AreEqual(0, Right.First, "Are not equal");
			Assert.AreEqual(decimal.Zero, Right.Second, "Are not equal");
			Assert.AreSame(Left.Third, Right.Third, "Are not same");
		}
		
		[Test()]
		public void ReadOnlyObjectCloneToReadOnly()
		{
			var Left = new CustomObjectReadOnly(12345678, 12345678.90m, new object());
			var Right = new CustomObjectReadOnly(0, decimal.Zero, null);
			Cloning.CloneToWithReadOnly(Left, Right);
			
			Assert.AreEqual(Left, Right, "Are not equal");
		}
		
		[Test()]
		public void InheritedObjectClone()
		{
			var Left = new CustomObjectInherited(0, decimal.Zero, null, '\0');
			var Right = Cloning.Clone(Left);
			
			Assert.AreEqual(Left, Right, "Are not equal");
		}
		
		[Test()]
		public void InheritedObjectCloneWithValues()
		{
			var Left = new CustomObjectInherited(12345678, 12345678.90m, new object(), 'A');
			var Right = Cloning.Clone(Left);
			
			Assert.AreEqual(Left, Right, "Are not equal");
		}
		
		[Test()]
		public void InheritedObjectCloneDynamic()
		{
			var Left = (CustomObjectBasic)new CustomObjectInherited(0, decimal.Zero, null, '\0');
			var Right = Cloning.CloneDynamic(Left);
			
			Assert.IsInstanceOf(typeof(CustomObjectInherited), Right, "Is not expected type");
			Assert.AreEqual(Left, Right, "Are not equal");
		}
		
		//****************************************
		
		private class CustomObjectBasic : IEquatable<CustomObjectBasic>
		{	//****************************************
			private int _First;
			private decimal _Second;
			private object _Third;
			//****************************************
			
			public CustomObjectBasic()
			{
			}
			
			public CustomObjectBasic(int first, decimal second, object third)
			{
				_First = first;
				_Second = second;
				_Third = third;
			}
			
			public override bool Equals(object obj)
			{
				return (obj is CustomObjectBasic) && Equals((CustomObjectBasic)obj);
			}
			
			public bool Equals(CustomObjectBasic other)
			{
				return _First == other._First && _Second == other._Second && _Third == other._Third;
			}
			
			public override int GetHashCode()
			{
				int hashCode = 0;
				unchecked
				{
					hashCode += 1000000007 * _First.GetHashCode();
					hashCode += 1000000009 * _Second.GetHashCode();
					if (_Third != null)
						hashCode += 1000000021 * _Third.GetHashCode();
				}
				return hashCode;
			}
			
			public int First
			{
				get { return _First; }
			}
			
			public decimal Second
			{
				get { return _Second; }
			}
			
			public object Third
			{
				get { return _Third; }
			}
		}

		private class CustomObjectComplex : IEquatable<CustomObjectComplex>
		{ //****************************************
			private readonly CustomObjectBasic _First;
			private decimal? _Second;
			private object _Third;
			//****************************************

			public CustomObjectComplex()
			{
			}

			public CustomObjectComplex(CustomObjectBasic first, decimal? second, object third)
			{
				_First = first;
				_Second = second;
				_Third = third;
			}

			public override bool Equals(object obj)
			{
				return (obj is CustomObjectComplex) && Equals((CustomObjectComplex)obj);
			}

			public bool Equals(CustomObjectComplex other)
			{
				return _First == other._First && _Second == other._Second && _Third == other._Third;
			}

			public override int GetHashCode()
			{
				int hashCode = 0;
				unchecked
				{
					hashCode += 1000000007 * _First.GetHashCode();
					hashCode += 1000000009 * _Second.GetHashCode();
					if (_Third != null)
						hashCode += 1000000021 * _Third.GetHashCode();
				}
				return hashCode;
			}

			public CustomObjectBasic First
			{
				get { return _First; }
			}

			public decimal? Second
			{
				get { return _Second; }
			}

			public object Third
			{
				get { return _Third; }
			}
		}

		private class CustomObjectReadOnly : IEquatable<CustomObjectReadOnly>
		{	//****************************************
			private readonly int _First;
			private readonly decimal _Second;
			private object _Third;
			//****************************************
			
			public CustomObjectReadOnly(int first, decimal second, object third)
			{
				_First = first;
				_Second = second;
				_Third = third;
			}
			
			public override bool Equals(object obj)
			{
				return (obj is CustomObjectReadOnly) && Equals((CustomObjectReadOnly)obj);
			}
			
			public bool Equals(CustomObjectReadOnly other)
			{
				return _First == other._First && _Second == other._Second && _Third == other._Third;
			}
			
			public override int GetHashCode()
			{
				int hashCode = 0;
				unchecked
				{
					hashCode += 1000000007 * _First.GetHashCode();
					hashCode += 1000000009 * _Second.GetHashCode();
					if (_Third != null)
						hashCode += 1000000021 * _Third.GetHashCode();
				}
				return hashCode;
			}
			
			public int First
			{
				get { return _First; }
			}
			
			public decimal Second
			{
				get { return _Second; }
			}
			
			public object Third
			{
				get { return _Third; }
			}
		}
		
		private class CustomObjectInherited : CustomObjectBasic, IEquatable<CustomObjectInherited>
		{	//****************************************
			private char _Fourth;
			//****************************************
			
			public CustomObjectInherited(int first, decimal second, object third, char fourth) : base(first, second, third)
			{
				_Fourth = fourth;
			}
			
			public override bool Equals(object obj)
			{
				return (obj is CustomObjectInherited) && Equals((CustomObjectInherited)obj);
			}
			
			public bool Equals(CustomObjectInherited other)
			{
				return base.Equals(other) && _Fourth == other._Fourth;
			}
			
			public override int GetHashCode()
			{
				int hashCode = base.GetHashCode();
				unchecked
				{
					hashCode += 1000000007 * _Fourth.GetHashCode();
				}
				return hashCode;
			}
			
			public char Fourth
			{
				get { return _Fourth; }
			}
		}

		private class CustomObjectInheritedReadOnly : CustomObjectReadOnly, IEquatable<CustomObjectInheritedReadOnly>
		{ //****************************************
			private char _Fourth;
			//****************************************

			public CustomObjectInheritedReadOnly(int first, decimal second, object third, char fourth) : base(first, second, third)
			{
				_Fourth = fourth;
			}

			public override bool Equals(object obj)
			{
				return (obj is CustomObjectInheritedReadOnly) && Equals((CustomObjectInheritedReadOnly)obj);
			}

			public bool Equals(CustomObjectInheritedReadOnly other)
			{
				return base.Equals(other) && _Fourth == other._Fourth;
			}

			public override int GetHashCode()
			{
				int hashCode = base.GetHashCode();
				unchecked
				{
					hashCode += 1000000007 * _Fourth.GetHashCode();
				}
				return hashCode;
			}

			public char Fourth
			{
				get { return _Fourth; }
			}
		}
	}
}
