/****************************************\
 WinUpDownBase.cs
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
	/// DataBinding support for NumericUpDown, DomainUpDown
	/// </summary>
	internal class WinUpDownBase : WinBoundControl
	{	//****************************************
		private UpDownBase _UpDownBase;
		//****************************************

		internal WinUpDownBase(WinBindingSource source, Control control) : base(source, control)
		{
			_UpDownBase = (UpDownBase)control;
			_UpDownBase.Validated += OnValidated;
		}

		//****************************************

		protected override void SetValue(object newValue)
		{
			BubbleValue = false;
			
			try
			{
				if (_UpDownBase is NumericUpDown)
					((NumericUpDown)_UpDownBase).Value = Convert.ToDecimal(newValue);
				else
					((DomainUpDown)_UpDownBase).SelectedItem = newValue;
			}
			catch (ArgumentOutOfRangeException)
			{
				// Ignore, don't update the binding
			}
				
			BubbleValue = true;
		}
		
		//****************************************
		
		private void OnValidated(object sender, EventArgs e)
		{
			if (BubbleValue)
			{
				if (_UpDownBase is NumericUpDown)
					ChangeValue(((NumericUpDown)_UpDownBase).Value);
				else
					ChangeValue(((DomainUpDown)_UpDownBase).SelectedItem);
			}
		}
	}
}
