using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
//****************************************

namespace System.Collections.Generic
{
	/// <summary>
	/// Provides a comparer that sorts in reversed order to default
	/// </summary>
	/// <remarks>Note that the reversal also includes null values, which will also be sorted in reverse of normal</remarks>
	public abstract class ReverseComparer
	{
		/// <summary>
		/// Creates a comparer wrapping an existing comparer, reversing its sorting order
		/// </summary>
		/// <param name="comparer">The <see cref="IComparer{T}"/> implementation to reverse</param>
		/// <returns>A reversed comparer wrapping the given comparer</returns>
		public static ReverseComparer<T> Reverse<T>(IComparer<T> comparer) => new WrappedComparer<T>(comparer);

		/// <summary>
		/// Provides a generic reversed comparer for nullable value types implementing IComparable
		/// </summary>
		private protected sealed class NullableComparer<T> : ReverseComparer<T?> where T : struct, IComparable<T>
		{
			public override int Compare(T? x, T? y)
			{
				if (x == null)
					return (y == null) ? 0 : 1;
				else if (y == null)
					return -1;

				return y.Value.CompareTo(x.Value);
			}
		}

		/// <summary>
		/// Provides a generic reversed comparer for types implementing IComparable
		/// </summary>
		private protected sealed class GenericComparer<T> : ReverseComparer<T> where T : IComparable<T>
		{
			public override int Compare(T x, T y)
			{
				if (x == null)
					return (y == null) ? 0 : 1;
				else if (y == null)
					return -1;

				return y.CompareTo(x);
			}
		}

		/// <summary>
		/// Provides a reversed comparer for types outsourced to <see cref="Comparer{T}.Default"/>
		/// </summary>
		private protected sealed class ObjectComparer<T> : ReverseComparer<T>
		{
			public override int Compare(T x, T y) => Comparer<T>.Default.Compare(y, x);
		}

		/// <summary>
		/// Provides a reversed comparer wrapping an existing comparer
		/// </summary>
		private protected sealed class WrappedComparer<T> : ReverseComparer<T>
		{ //****************************************
			private readonly IComparer<T> _Comparer;
			//****************************************

			internal WrappedComparer(IComparer<T> comparer) => _Comparer = comparer;

			//****************************************

			public override int Compare(T x, T y) => _Comparer.Compare(y, x);
		}
	}

	/// <summary>
	/// Provides a comparer that sorts in reversed order to default
	/// </summary>
	/// <remarks>Note that the reversal also includes null values, which will also be sorted in reverse of normal</remarks>
	public abstract class ReverseComparer<T> : ReverseComparer, IComparer<T>, IComparer
	{	//****************************************
		private static ReverseComparer<T>? _DefaultComparer;
		//****************************************

		/// <summary>
		/// Compares two objects of the same type and returns a value indicating whether one is less than, equal to, or greater than the other
		/// </summary>
		/// <param name="x">The first object to compare</param>
		/// <param name="y">The second object to compare</param>
		/// <returns>A signed integer indicating the relative values of x and y</returns>
		public abstract int Compare(T x, T y);

		//****************************************

		int IComparer.Compare(object x, object y)
		{
			if (x is null)
				return (y is null) ? 0 : 1;
			else if (y is null)
				return -1;

			if (x is T X && y is T Y)
				return Compare(X, Y);

			throw new ArgumentException("Argument is not valid for this comparer");
		}

		//****************************************

		/// <summary>
		/// Gets a reversed sort order comparer for the type specified by the generic argument
		/// </summary>
		public static ReverseComparer<T> Default
		{
			get
			{
				if (_DefaultComparer == null)
					Interlocked.CompareExchange(ref _DefaultComparer, CreateComparer(), null);
				
				return _DefaultComparer;
			}
		}

		//****************************************

		private static ReverseComparer<T> CreateComparer()
		{	//****************************************
			var MyType = typeof(T);
			//****************************************
			
			// Does TValue implement the generic IComparable?
			if (typeof(IComparable<T>).IsAssignableFrom(MyType))
				return (ReverseComparer<T>)Activator.CreateInstance(typeof(GenericComparer<>).MakeGenericType(MyType));
			
			// Is TValue a nullable value?
			if (MyType.IsGenericType && MyType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				var MySubType = MyType.GetGenericArguments()[0];
				
				// Does the child type implement the generic IComparable?
				if (typeof(IComparable<>).MakeGenericType(MySubType).IsAssignableFrom(MySubType))
					return (ReverseComparer<T>)Activator.CreateInstance(typeof(NullableComparer<>).MakeGenericType(MySubType));
			}
			
			// Nope, just use the default object comparer then
			return new ObjectComparer<T>();
		}
	}
}
