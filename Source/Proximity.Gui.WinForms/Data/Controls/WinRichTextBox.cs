/****************************************\
 WinRichTextBox.cs
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
	/// DataBinding support for RichTextBox
	/// </summary>
	internal class WinRichTextBox : WinBoundControl
	{	//****************************************
		private RichTextBox _RichTextBox;
		//****************************************

		internal WinRichTextBox(WinBindingSource source, Control control) : base(source, control)
		{
			_RichTextBox = (RichTextBox)control;
			_RichTextBox.Validated += OnValidated;
		}

		//****************************************

		protected override void SetValue(object value)
		{
			BubbleValue = false;

			if (value == null)
				_RichTextBox.Rtf = "";
			else
				_RichTextBox.Rtf = value.ToString();
			BubbleValue = true;
		}

		//****************************************

		private void OnValidated(object sender, EventArgs e)
		{
			if (BubbleValue)
				ChangeValue(_RichTextBox.Rtf);
		}
	}
}
