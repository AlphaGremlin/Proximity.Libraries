/****************************************\
 WinTextBoxBase.cs
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
	/// DataBinding support for TextBox, MaskedTextBox
	/// </summary>
	internal class WinTextBoxBase : WinBoundControl
	{	//****************************************
		private TextBoxBase _TextBoxBase;
		//****************************************

		internal WinTextBoxBase(WinBindingSource source, Control control) : base(source, control)
		{
			_TextBoxBase = (TextBoxBase)control;
			_TextBoxBase.KeyPress += OnKeyPress;
			_TextBoxBase.Validated += OnValidated;
		}

		//****************************************

		protected override void SetValue(object value)
		{
			BubbleValue = false;

			if (value == null)
				_TextBoxBase.Text = "";
			else
				_TextBoxBase.Text = value.ToString();
			BubbleValue = true;
		}

		//****************************************
		
		private void OnKeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\r')
			{
				ChangeValue(_TextBoxBase.Text);
				
				InvokeCommand();
				
				e.Handled = true;
			}
		}

		private void OnValidated(object sender, EventArgs e)
		{
			if (BubbleValue)
				ChangeValue(_TextBoxBase.Text);
		}
	}
}
