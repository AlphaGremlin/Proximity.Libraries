/****************************************\
 NullableDecimalValidator.cs
 Created: 2014-06-11
\****************************************/
#if !MOBILE && !PORTABLE
using System;
using System.Configuration;
using System.Globalization;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// Represents a configuration validator where the decimal value can be null
	/// </summary>
	public class NullableDecimalValidator : ConfigurationValidatorBase
	{	//****************************************
		private decimal _MinValue = decimal.MinValue, _MaxValue = decimal.MaxValue;
		private bool _ExcludeRange;
		//****************************************
		
		/// <summary>
		/// Creates a new nullable decimal validator with a given minimum and maximum
		/// </summary>
		public NullableDecimalValidator(decimal minValue, decimal maxValue) : this(minValue, maxValue, false)
		{
		}
		
		/// <summary>
		/// Creates a new nullable decimal validator with a given minimum and maximum, and exclusion flag
		/// </summary>
		public NullableDecimalValidator(decimal minValue, decimal maxValue, bool excludeRange)
		{
			if (minValue > maxValue)
				throw new ArgumentOutOfRangeException("minValue");
			
			_MinValue = minValue;
			_MaxValue = maxValue;
			_ExcludeRange = excludeRange;
		}
		
		//****************************************
		
		/// <summary>
		/// Checks whether this Validator can validate a type
		/// </summary>
		/// <param name="type">The type to check</param>
		/// <returns>True if the passed type is supported</returns>
		public override bool CanValidate(Type type)
		{
			return type == typeof(decimal?) || type == typeof(decimal);
		}
		
		/// <summary>
		/// Validates the provided value
		/// </summary>
		/// <param name="value">The value to validate</param>
		public override void Validate(object value)
		{
			if (!(value is decimal || value is decimal?))
				throw new ArgumentException("value");
			
			var MyValue = (decimal?) value;
			
			if (MyValue == null)
				return;
			
			if (_ExcludeRange)
			{
				if (MyValue > _MinValue && MyValue < _MaxValue)
					throw new ArgumentOutOfRangeException("value");
			}
			else
			{
				if (MyValue < _MinValue || MyValue > _MaxValue)
					throw new ArgumentOutOfRangeException("value");
			}
		}
	}
}
#endif