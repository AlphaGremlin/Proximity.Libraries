/****************************************\
 EnumConverter.cs
 Created: 21-05-10
\****************************************/
using System;
using System.Collections.Generic;
using System.Xml;
//****************************************

namespace Proximity.Gui.Data.Converters
{
	public class EnumConverter : IGuiConverter
	{	//****************************************
		private string _Enum;
		//****************************************

		public EnumConverter()
		{
			
		}
		
		public EnumConverter(string enumName)
		{
			_Enum = enumName;
		}

		public EnumConverter(XmlReader reader)
		{
			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
				case "Enum":
					_Enum = reader.Value;
					break;

				default:
					break;
				}

			}
		}

		//****************************************

		public object ConvertTo(object source, Type targetType, object parameter)
		{
			return source;
		}

		public object ConvertFrom(object source, Type targetType, object parameter)
		{
			return source;
		}
		
		//****************************************
		
		public string Enum
		{
			get { return _Enum; }
			set { _Enum = value; }
		}
	}
}
