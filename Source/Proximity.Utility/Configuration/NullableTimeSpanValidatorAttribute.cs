/****************************************\
 NullableTimeSpanValidatorAttribute.cs
 Created: 2013-05-21
\****************************************/
#if !NETSTANDARD1_3 && !NETSTANDARD2_0
using System;
using System.Configuration;
using System.Globalization;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// Represents a configuration validator attribute where the TimeSpan value can be null
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
	public sealed class NullableTimeSpanValidatorAttribute : ConfigurationValidatorAttribute
	{	//****************************************
		private TimeSpan _Min = TimeSpan.MinValue, _Max = TimeSpan.MaxValue;
		private bool _ExcludeRange;
		//****************************************
		
		/// <summary>
		/// Creates a new, blank nullable TimeSpan validator
		/// </summary>
		public NullableTimeSpanValidatorAttribute()
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Gets a validator instance from this attribute
		/// </summary>
		public override ConfigurationValidatorBase ValidatorInstance
		{
			get { return new NullableTimeSpanValidator(_Min, _Max, _ExcludeRange); }
		}
		
		/// <summary>
		/// Gets/Sets the minimum value
		/// </summary>
		public TimeSpan MinValue
		{
			get { return _Min; }
			set
			{
				if (_Max < value)
					throw new ArgumentOutOfRangeException("value");
				
				_Min = value;
			}
		}
		
		/// <summary>
		/// Gets/Sets the maximum value
		/// </summary>
		public TimeSpan MaxValue
		{
			get { return _Max; }
			set
			{
				if (_Min > value)
					throw new ArgumentOutOfRangeException("value");
				
				_Max = value;
			}
		}
		
		/// <summary>
		/// Gets/Sets the minimum value as a string
		/// </summary>
		public string MinValueString
		{
			get { return _Min.ToString(); }
			set
			{
				TimeSpan MyValue = TimeSpan.Parse(value, CultureInfo.InvariantCulture);
				
				if (_Max < MyValue)
					throw new ArgumentOutOfRangeException("value");
				
				_Min = MyValue;
			}
		}
		
		/// <summary>
		/// Gets/Sets the maximum value as a string
		/// </summary>
		public string MaxValueString
		{
			get { return _Max.ToString(); }
			set
			{
				TimeSpan MyValue = TimeSpan.Parse(value, CultureInfo.InvariantCulture);
				
				if (_Min > MyValue)
					throw new ArgumentOutOfRangeException("value");
				
				_Max = MyValue;
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