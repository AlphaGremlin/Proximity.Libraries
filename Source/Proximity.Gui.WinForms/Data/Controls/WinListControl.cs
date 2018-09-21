/****************************************\
 WinListControl.cs
 Created: 28-05-10
\****************************************/
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using Proximity.Gui.Data;
using Proximity.Gui.Presentation;
using Proximity.Gui.WinForms;
using Proximity.Gui.WinForms.Data;
//****************************************

namespace Proximity.Gui.WinForms.Data.Controls
{
	/// <summary>
	/// DataBinding support for ListBox and ComboBox
	/// </summary>
	internal class WinListControl : WinBoundListControl
	{	//****************************************
		private ListControl _ListControl;
		private string[] _DisplayPath;
		private IGuiConverter _Converter;
		//****************************************

		internal WinListControl(WinBindingSource source, Control control) : base(source, control)
		{
			_ListControl = (ListControl)control;

			_ListControl.SelectedValueChanged += OnSelectedValueChanged;
			_ListControl.DoubleClick += OnDoubleClick;
		}

		//****************************************

		internal void BindColumn(string sourcePath, IGuiConverter converter)
		{
			_DisplayPath = sourcePath.Split('.');
			_Converter = converter;
		}

		//****************************************

		protected override object GetSelection()
		{
			return _ListControl.SelectedValue;
		}

		protected override void SetContents(IList contents)
		{	//****************************************
			ListBox MyListBox;
			ComboBox MyComboBox;
			//****************************************

			if (_ListControl is ListBox)
			{
				MyListBox = (ListBox)_ListControl;

				MyListBox.BeginUpdate();
				MyListBox.Items.Clear();

				foreach (object Value in contents)
				{
					MyListBox.Items.Add(Value);
				}

				MyListBox.EndUpdate();
			}
			else if (_ListControl is ComboBox)
			{
				MyComboBox = (ComboBox)_ListControl;

				MyComboBox.BeginUpdate();
				MyComboBox.Items.Clear();

				foreach (object Value in contents)
				{
					MyComboBox.Items.Add(Value);
				}

				MyComboBox.EndUpdate();
			}
		}

		protected override void SetSelection(object newValue)
		{
			BubbleSelection = false;
			
			if (_ListControl is ListBox)
				((ListBox)_ListControl).SelectedItem = newValue;
			else if (_ListControl is ComboBox)
				((ComboBox)_ListControl).SelectedItem = newValue;
			
			BubbleSelection = true;
		}

		//****************************************

		private void OnSelectedValueChanged(object sender, EventArgs e)
		{
			if (BubbleSelection)
			{
				if (_ListControl is ListBox)
					ChangeSelection(((ListBox)_ListControl).SelectedItem);
				else if (_ListControl is ComboBox)
					ChangeSelection(((ComboBox)_ListControl).SelectedItem);
			}
		}

		private void OnDoubleClick(object sender, EventArgs e)
		{
			if (_ListControl.SelectedIndex == -1)
				return;
			
			InvokeCommand();
		}
		
		//****************************************

		private class WinListControlItem
		{	//****************************************
			private WinListControl _Parent;
			private object _Value;
			//****************************************

			internal WinListControlItem(WinListControl parent, object value)
			{
				_Parent = parent;
				_Value = value;
			}

			//****************************************

			public override string ToString()
			{	//****************************************
				object PropertyValue = WinBindingSource.GetFromPath(_Value, _Parent._DisplayPath);
				//****************************************
				
				if (_Parent._Converter != null)
					return (string)_Parent._Converter.ConvertTo(PropertyValue, typeof(string), null);
				else if (PropertyValue is string)
					return (string)PropertyValue;
				else if (PropertyValue != null)
					return PropertyValue.ToString();
				else
					return "";
			}
		}
	}
}
