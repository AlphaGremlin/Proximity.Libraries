/****************************************\
 IGuiConverter.cs
 Created: 26-07-10
\****************************************/
using System;
//****************************************

namespace Proximity.Gui.Data
{
	/// <summary>
	/// Provides a way to convert between values
	/// </summary>
	public interface IGuiConverter
	{
		object ConvertTo(object source, Type targetType, object parameter);

		object ConvertFrom(object source, Type targetType, object parameter);
	}
}
