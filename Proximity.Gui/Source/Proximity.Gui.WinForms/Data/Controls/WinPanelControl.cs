/****************************************\
 WinPanelControl.cs
 Created: 2011-04-29
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
	/// DataBinding support for Panel controls (splitters, flow layout, table layout, etc)
	/// </summary>
	internal class WinPanelControl : WinBoundControl
	{	//****************************************
		private Panel _Panel;
		//****************************************

		internal WinPanelControl(WinBindingSource source, Control control) : base(source, control)
		{
			_Panel = (Panel)control;
		}

		//****************************************

		protected override void SetValue(object newValue)
		{
			throw new NotSupportedException();
		}
	}
}
