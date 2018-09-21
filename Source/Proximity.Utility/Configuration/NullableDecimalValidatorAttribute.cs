/****************************************\
 NullableDecimalValidatorAttribute.cs
 Created: 2014-06-11
\****************************************/
#if !NETSTANDARD1_3 && !NETSTANDARD2_0
using System;
using System.Configuration;
using System.Globalization;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// Represents a configuration validator attribute where the decimal value can be null
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
	public sealed class NullableDecimalValidatorAttribute : ConfigurationValidatorAttribute
	{	//****************************************
		private int[] _Min = new int[] { -1, -1, -1, -2147483648 }, _Max = new int[] { -1, -1, -1, 0 };
		private bool _ExcludeRange;
		//****************************************
		
		/// <summary>
		/// Creates a new, blank nullable decimal validator
		/// </summary>
		public NullableDecimalValidatorAttribute()
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Gets a validator instance from this attribute
		/// </summary>
		public override ConfigurationValidatorBase ValidatorInstance
		{
			get
			{
				var Min = new Decimal(_Min);
				var Max = new Decimal(_Max);
				
				if (Min > Max)
					throw new InvalidOperationException("Min cannot be greater than Max");
				
				return new NullableDecimalValidator(Min, Max, _ExcludeRange);
			}
		}
		
		/// <summary>
		/// Gets/Sets the minimum value
		/// </summary>
		public decimal MinValue
		{
			get { return new Decimal(_Min); }
			set
			{
				_Min = decimal.GetBits(value);
			}
		}
		
		/// <summary>
		/// Gets/Sets the maximum value
		/// </summary>
		public decimal MaxValue
		{
			get { return new Decimal(_Max); }
			set
			{
				_Max = decimal.GetBits(value);
			}
		}
		
		/// <summary>
		/// Gets/Sets whether the range between Min and Max should be treated as exclusive, rather than inclusive
		/// </summary>
		public bool ExcludeRange
		{
			get { return _ExcludeRange; }
			set { _ExcludeRange = value; }
		}
	}
}
#endif