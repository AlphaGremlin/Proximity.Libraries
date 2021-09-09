using System;
using System.Configuration;
using System.Globalization;
//****************************************

namespace Proximity.Configuration
{
	/// <summary>
	/// Represents a configuration validator attribute where the decimal value can be null
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
	public sealed class NullableDecimalValidatorAttribute : ConfigurationValidatorAttribute
	{	//****************************************
		// Attributes must contain serialisable types, so we have to use an array
		private int[] _Min = new int[] { -1, -1, -1, -2147483648 }, _Max = new int[] { -1, -1, -1, 0 };
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
		public override ConfigurationValidatorBase ValidatorInstance => new NullableDecimalValidator(MinValue, MaxValue, ExcludeRange);

		/// <summary>
		/// Gets/Sets the minimum value
		/// </summary>
		public decimal MinValue
		{
			get => new(_Min);
			set
			{
				if (MaxValue < value)
					throw new ArgumentOutOfRangeException(nameof(value));

				_Min = decimal.GetBits(value);
			}
		}

		/// <summary>
		/// Gets/Sets the maximum value
		/// </summary>
		public decimal MaxValue
		{
			get => new(_Max);
			set
			{
				if (MinValue > value)
					throw new ArgumentOutOfRangeException(nameof(value));

				_Max = decimal.GetBits(value);
			}
		}

		/// <summary>
		/// Gets/Sets whether the range between Min and Max should be treated as exclusive, rather than inclusive
		/// </summary>
		public bool ExcludeRange { get; set; }
	}
}
