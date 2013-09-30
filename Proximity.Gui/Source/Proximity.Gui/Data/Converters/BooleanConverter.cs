/****************************************\
 BooleanConverter.cs
 Created: 2011-02-18
\****************************************/
using System;
using System.Collections.Generic;
using System.Xml;
//****************************************

namespace Proximity.Gui.Data.Converters
{
	/// <summary>
	/// Converts values to and from booleans
	/// </summary>
	public class BooleanConverter : IGuiConverter
	{	//****************************************
		private bool _IsInverse;
		//****************************************
		
		public BooleanConverter()
		{
		}
		
		public BooleanConverter(bool isInverse)
		{
			_IsInverse = isInverse;
		}
		
		public BooleanConverter(XmlReader reader)
		{
			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
				case "IsInverse":
						_IsInverse = bool.Parse(reader.Value);
					break;

				default:
					break;
				}
			}
		}
		
		//****************************************
		
		public object ConvertTo(object source, Type targetType, object parameter)
		{
			if (source == null)
				return _IsInverse;
			
			if (source is bool)
				return (bool)source ^ _IsInverse;
			
			return !_IsInverse;
		}
		
		public object ConvertFrom(object source, Type targetType, object parameter)
		{
			throw new NotSupportedException();
		}
		
		//****************************************
		
		public bool IsInverse
		{
			get { return _IsInverse; }
		}
	}
}
