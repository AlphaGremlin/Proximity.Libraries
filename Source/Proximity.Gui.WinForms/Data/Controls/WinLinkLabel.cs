/****************************************\
 WinLinkLabel.cs
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
	/// DataBinding support for LinkLabel
	/// </summary>
	internal class WinLinkLabel : WinBoundControl
	{	//****************************************
		private LinkLabel _LinkLabel;
		//****************************************

		internal WinLinkLabel(WinBindingSource source, Control control) : base(source, control)
		{
			_LinkLabel = (LinkLabel)control;
			_LinkLabel.Click += OnClick;
		}

		//****************************************

		protected override void SetValue(object newValue)
		{
			throw new NotSupportedException();
		}

		//****************************************

		private void OnClick(object sender, EventArgs e)
		{
			InvokeCommand();
		}
	}
}
