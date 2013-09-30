/****************************************\
 WinGroupBox.cs
 Created: 2011-02-18
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
	/// DataBinding support for GroupBox
	/// </summary>
	internal class WinGroupBox : WinBoundControl
	{	//****************************************
		private GroupBox _GroupBox;
		//****************************************

		internal WinGroupBox(WinBindingSource source, Control control) : base(source, control)
		{
			_GroupBox = (GroupBox)control;
		}

		//****************************************

		protected override void SetValue(object newValue)
		{
			throw new NotSupportedException();
		}
	}
}
