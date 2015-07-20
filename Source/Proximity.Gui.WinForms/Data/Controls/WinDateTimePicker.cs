/****************************************\
 WinDateTimePicker.cs
 Created: 28-05-10
\****************************************/
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using Proximity.Gui.Presentation;
using Proximity.Gui.WinForms;
using Proximity.Gui.WinForms.Data;
//****************************************

namespace Proximity.Gui.WinForms.Data.Controls
{
	/// <summary>
	/// DataBinding support for DateTimePicker
	/// </summary>
	internal class WinDateTimePicker : WinBoundControl
	{	//****************************************
		private DateTimePicker _DateTimePicker;
		//****************************************

		internal WinDateTimePicker(WinBindingSource source, Control control) : base(source, control)
		{
			_DateTimePicker = (DateTimePicker)control;
		}

		//****************************************

		protected override void SetValue(object newValue)
		{
			_DateTimePicker.Value = (newValue is DateTime) ? (DateTime)newValue : DateTime.Today;
		}
	}
}
