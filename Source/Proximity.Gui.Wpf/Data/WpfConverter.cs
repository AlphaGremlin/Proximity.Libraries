/****************************************\
 WpfConverter.cs
 Created: 26-07-10
\****************************************/
using System;
using System.Globalization;
using System.Windows.Data;
using Proximity.Gui.Data;
//****************************************

namespace Proximity.Gui.Wpf.Data
{
	/// <summary>
	/// Wraps an IGuiConverter for WPF
	/// </summary>
	public class WpfConverter : IValueConverter
	{	//****************************************
		private IGuiConverter _Converter;
		//****************************************
		
		internal WpfConverter(IGuiConverter converter)
		{
			_Converter = converter;
		}
		
		//****************************************

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return _Converter.ConvertTo(value, targetType, parameter);
		}
		
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return _Converter.ConvertFrom(value, targetType, parameter);
		}
	}
}
