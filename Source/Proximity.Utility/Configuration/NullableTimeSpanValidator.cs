/****************************************\
 NullableTimeSpanValidator.cs
 Created: 2013-05-21
\****************************************/
#if !MOBILE && !PORTABLE
using System;
using System.Configuration;
using System.Globalization;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// Represents a configuration validator where the integer value can be null
	/// </summary>
	public class NullableTimeSpanValidator : ConfigurationValidatorBase
	{	//****************************************
		private TimeSpan _MinValue = TimeSpan.MinValue, _MaxValue = TimeSpan.MaxValue;
		private bool _ExcludeRange;
		//****************************************
		
		/// <summary>
		/// Creates a new nullable TimeSpan validator with a given minimum and maximum
		/// </summary>
		public NullableTimeSpanValidator(TimeSpan minValue, TimeSpan maxValue) : this(minValue, maxValue, false)
		{
		}
		
		/// <summary>
		/// Creates a new nullable TimeSpan validator with a given minimum and maximum, and exclusion flag
		/// </summary>
		public NullableTimeSpanValidator(TimeSpan minValue, TimeSpan maxValue, bool excludeRange)
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
			return type == typeof(TimeSpan?) || type == typeof(TimeSpan);
		}
		
		/// <summary>
		/// Validates the provided value
		/// </summary>
		/// <param name="value">The value to validate</param>
		public override void Validate(object value)
		{
			if (!(value is TimeSpan || value is TimeSpan?))
				throw new ArgumentException("value");
			
			var MyValue = (TimeSpan?) value;
			
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