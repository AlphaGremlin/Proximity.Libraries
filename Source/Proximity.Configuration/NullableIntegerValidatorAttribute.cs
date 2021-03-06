using System;
using System.Configuration;
using System.Globalization;
//****************************************

namespace Proximity.Configuration
{
	/// <summary>
	/// Represents a configuration validator attribute where the integer value can be null
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
	public sealed class NullableIntegerValidatorAttribute : ConfigurationValidatorAttribute
	{	//****************************************
		private int _Min = int.MinValue, _Max = int.MaxValue;
		//****************************************

		/// <summary>
		/// Creates a new, blank nullable integer validator
		/// </summary>
		public NullableIntegerValidatorAttribute()
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Gets a validator instance from this attribute
		/// </summary>
		public override ConfigurationValidatorBase ValidatorInstance
		{
			get { return new NullableIntegerValidator(_Min, _Max, ExcludeRange); }
		}
		
		/// <summary>
		/// Gets/Sets the minimum value
		/// </summary>
		public int MinValue
		{
			get => _Min;
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
		public int MaxValue
		{
			get => _Max;
			set
			{
				if (_Min > value)
					throw new ArgumentOutOfRangeException("value");

				_Max = value;
			}
		}

		/// <summary>
		/// Gets/Sets whether the range between Min and Max should be treated as exclusive, rather than inclusive
		/// </summary>
		public bool ExcludeRange { get; set; }
	}
}
