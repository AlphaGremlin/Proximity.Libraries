/****************************************\
 ReverseComparer.cs
 Created: 2012-05-22
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using Proximity.Utility.Collections.Reverse;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents an IComparer that sorts in reversed order to default
	/// </summary>
	/// <remarks>Note that the reversal also includes null values, which will be sorted first</remarks>
	public abstract class ReverseComparer<TValue> : IComparer<TValue>, IComparer
	{	//****************************************
		private static ReverseComparer<TValue> _DefaultComparer;
		//****************************************
		
		/// <summary>
		/// Initialises a new instance of the ReverseComparer
		/// </summary>
		protected ReverseComparer()
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Compares two objects of the same type and returns a value indicating whether one is less than, equal to, or greater than the other
		/// </summary>
		/// <param name="x">The first object to compare</param>
		/// <param name="y">The second object to compare</param>
		/// <returns>A signed integer indicating the relative values of x and y</returns>
		public abstract int Compare(TValue x, TValue y);
		
		//****************************************
		
		int IComparer.Compare(object x, object y)
		{
			if (x == null)
				return (y == null) ? 0 : 1;
			else if (y == null)
				return -1;
			
			if (!(x is TValue) || !(y is TValue))
				throw new ArgumentException("Argument is not valid for this comparer");
			
			return Compare((TValue)x, (TValue)y);
		}
		
		//****************************************
		
		/// <summary>
		/// Gets a reversed sort order comparer for the type specified by the generic argument
		/// </summary>
		public static ReverseComparer<TValue> Default
		{
			get
			{
				if (_DefaultComparer == null)
					_DefaultComparer = CreateComparer();
				
				return _DefaultComparer;
			}
		}
		
		/// <summary>
		/// Creates a comparer wrapping an existing comparer, reversing its sorting order
		/// </summary>
		/// <param name="comparer">The IComparer implementation to reverse</param>
		/// <returns>A reversed comparer wrapping the given comparer</returns>
		public static ReverseComparer<TValue> Wrapped(IComparer<TValue> comparer)
		{
			return new WrappedComparer<TValue>(comparer);
		}
		
		//****************************************
		
#if NETSTANDARD1_3
		private static ReverseComparer<TValue> CreateComparer()
		{
			return new WrappedComparer<TValue>(Comparer<TValue>.Default);
		}
#else
		private static ReverseComparer<TValue> CreateComparer()
		{	//****************************************
			var MyType = typeof(TValue);
			//****************************************
			
			// Does TValue implement the generic IComparable?
			if (typeof(IComparable<TValue>).IsAssignableFrom(MyType))
				return (ReverseComparer<TValue>)Activator.CreateInstance(typeof(GenericComparer<>).MakeGenericType(MyType));
			
			// Is TValue a nullable value?
			if (MyType.IsGenericType && MyType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				var MySubType = MyType.GetGenericArguments()[0];
				
				// Does the child type implement the generic IComparable?
				if (typeof(IComparable<>).MakeGenericType(MySubType).IsAssignableFrom(MySubType))
					return (ReverseComparer<TValue>)Activator.CreateInstance(typeof(NullableComparer<>).MakeGenericType(MySubType));
			}
			
			// Nope, just use the default object comparer then
			return new ObjectComparer<TValue>();
		}
#endif
	}
}
