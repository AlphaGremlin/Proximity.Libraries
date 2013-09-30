/****************************************\
 WinBoundControl.cs
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
using Proximity.Gui.WinForms.Data.Controls;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.WinForms.Data
{
	/// <summary>
	/// Base class for WinForms DataBinding managers
	/// </summary>
	internal abstract class WinBoundControl
	{	//****************************************
		private WinBindingSource _Source;
		private Control _Control;

		private string _Command;

		private WinMonitor _ValueMonitor, _EnabledMonitor, _VisibleMonitor;
		private IGuiConverter _ValueConverter, _EnabledConverter, _VisibleConverter;
		private object _Value;
		private bool _BubbleValue = true;
		//****************************************

		protected WinBoundControl(WinBindingSource source, Control control)
		{
			_Source = source;
			_Control = control;
		}

		//****************************************

		internal void Bind(WinBoundListControl sourceList, string sourcePath, IGuiConverter converter)
		{
			_ValueConverter = converter;

			_ValueMonitor = new WinMonitor(sourcePath);
			_ValueMonitor.ValueChanged += OnValueChanged;

			if (sourceList != null)
			{
				_ValueMonitor.Target = sourceList;
			}
		}

		internal void BindCommand(string command)
		{
			_Command = command;
		}
		
		internal void BindEnabled(WinBoundListControl sourceList, string sourcePath, IGuiConverter converter)
		{
			_EnabledConverter = converter;

			_EnabledMonitor = new WinMonitor(sourcePath);
			_EnabledMonitor.ValueChanged += OnEnabledChanged;

			if (sourceList != null)
			{
				_EnabledMonitor.Target = sourceList;
			}
		}

		internal void BindVisible(WinBoundListControl sourceList, string sourcePath, IGuiConverter converter)
		{
			_VisibleConverter = converter;

			_VisibleMonitor = new WinMonitor(sourcePath);
			_VisibleMonitor.ValueChanged += OnVisibleChanged;

			if (sourceList != null)
			{
				_VisibleMonitor.Target = sourceList;
			}
		}
		
		internal void BindEventKeyPress(Keys sourceKey, string command)
		{
			_Control.KeyDown += (sender, e) =>
			{
				if (e.KeyData == sourceKey)
					((WinToolkit)GuiService.Toolkit).Execute(_Source.Presenter, command);
			};
		}
		
		internal void BindEventMouseClick(MouseButtons sourceButton, string command)
		{
			_Control.MouseDown += (sender, e) =>
			{
				if (e.Button == sourceButton)
					((WinToolkit)GuiService.Toolkit).Execute(_Source.Presenter, command);
			};
		}
		
		//****************************************

		protected internal virtual void Attach()
		{
			if (_ValueMonitor != null && _ValueMonitor.Target == null)
				_ValueMonitor.Target = _Source.Presenter;
			
			if (_EnabledMonitor != null && _EnabledMonitor.Target == null)
				_EnabledMonitor.Target = _Source.Presenter;
			
			if (_VisibleMonitor != null && _VisibleMonitor.Target == null)
				_VisibleMonitor.Target = _Source.Presenter;
		}

		protected void ChangeValue(object newValue)
		{
			try
			{
				_BubbleValue = false;

				_Value = newValue;

				if (_ValueConverter == null)
					_ValueMonitor.Value = newValue;
				else
					_ValueMonitor.Value = _ValueConverter.ConvertFrom(newValue, null, null);
			}
			finally
			{
				_BubbleValue = true;
			}
		}

		protected abstract void SetValue(object newValue);

		protected void InvokeCommand()
		{
			if (_Command == null)
				return;
			
			((WinToolkit)GuiService.Toolkit).Execute(_Source.Presenter, _Command);
		}

		//****************************************

		private void OnValueChanged(object sender, EventArgs e)
		{
			if (!_BubbleValue)
				return;

			if (_ValueConverter == null)
				_Value = _ValueMonitor.Value;
			else
				_Value = _ValueConverter.ConvertTo(_ValueMonitor.Value, null, null);

			SetValue(_Value);
		}
		
		private void OnEnabledChanged(object sedner, EventArgs e)
		{
			bool IsEnabled;
			
			if (!_BubbleValue)
				return;

			if (_EnabledConverter == null)
				IsEnabled = (bool)_EnabledMonitor.Value;
			else
				IsEnabled = (bool)_EnabledConverter.ConvertTo(_EnabledMonitor.Value, typeof(bool), null);

			_Control.Enabled = IsEnabled;
		}

		private void OnVisibleChanged(object sedner, EventArgs e)
		{
			bool IsVisible;
			
			if (!_BubbleValue)
				return;

			if (_VisibleConverter == null)
				IsVisible = (bool)_VisibleMonitor.Value;
			else
				IsVisible = (bool)_VisibleConverter.ConvertTo(_VisibleMonitor.Value, typeof(bool), null);

			_Control.Visible = IsVisible;
		}
		
		//****************************************

		internal WinBindingSource Source
		{
			get { return _Source; }
		}

		internal string Command
		{
			get { return _Command; }
		}

		internal object Value
		{
			get { return _Value; }
		}

		protected bool BubbleValue
		{
			get { return _BubbleValue; }
			set { _BubbleValue = value; }
		}

		//****************************************

		internal static WinBoundControl GetControl(WinBindingSource source, Control targetControl)
		{
			if (targetControl is ListControl)
				return new WinListControl(source, targetControl);
			else if (targetControl is RichTextBox)
				return new WinRichTextBox(source, targetControl);
			else if (targetControl is TextBoxBase)
				return new WinTextBoxBase(source, targetControl);
			else if (targetControl is ListView)
				return new WinListView(source, targetControl);
			else if (targetControl is ButtonBase)
				return new WinButtonBase(source, targetControl);
			else if (targetControl is UpDownBase)
				return new WinUpDownBase(source, targetControl);
			else if (targetControl is DateTimePicker)
				return new WinDateTimePicker(source, targetControl);
			else if (targetControl is TabControl)
				return new WinTabControl(source, targetControl);
			else if (targetControl is GroupBox)
				return new WinGroupBox(source, targetControl);
			else if (targetControl is Panel)
				return new WinPanelControl(source, targetControl);
			else
			{
				Log.Warning("Unable to bind {0}, not supported", targetControl.GetType().FullName);
				
				return null;
			}
		}
	}
}
