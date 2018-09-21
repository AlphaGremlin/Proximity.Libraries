/****************************************\
 WinBoundListControl.cs
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
//****************************************

namespace Proximity.Gui.WinForms.Data
{
	internal abstract class WinBoundListControl : WinBoundControl
	{	//****************************************
		internal event EventHandler SelectedItemChanged;
		//****************************************
		private WinMonitor _SelectedMonitor;
		private IGuiConverter _SelectedConverter;
		private object _SelectedItem;

		private bool _BubbleSelection = true;
		//****************************************
		
		protected WinBoundListControl(WinBindingSource source, Control control) : base(source, control)
		{
		}

		//****************************************

		internal void BindSelection(WinBoundListControl sourceList, string sourcePath, IGuiConverter converter)
		{
			_SelectedConverter = converter;

			_SelectedMonitor = new WinMonitor(sourcePath);
			_SelectedMonitor.ValueChanged += OnValueChanged;

			if (sourceList != null)
				_SelectedMonitor.Target = sourceList;
		}

		//****************************************

		protected abstract void SetContents(IList contents);

		protected void ChangeSelection(object newValue)
		{
			try
			{
				_BubbleSelection = false;
				if (_SelectedMonitor == null)
				{
					_SelectedItem = newValue;

					RaiseSelectedItemChanged();
				}
				else
				{
					_SelectedItem = newValue;
					
					if (_SelectedConverter == null)
						_SelectedMonitor.Value = newValue;
					else
						_SelectedMonitor.Value = _SelectedConverter.ConvertFrom(newValue, null, null);
				}
			}
			finally
			{
				_BubbleSelection = true;
			}
		}

		protected abstract void SetSelection(object newValue);

		protected abstract object GetSelection();

		protected internal override void Attach()
		{
			base.Attach();

			if (_SelectedMonitor == null)
				_SelectedItem = GetSelection();
			else if (_SelectedMonitor.Target == null)
			{
				// Attach our current Selection to the Presenter
				// This will cause OnValueChanged below, which will change the selection in the underlying Control
				_SelectedMonitor.Target = Source.Presenter;
			}
		}

		protected override void SetValue(object newValue)
		{
			SetContents(newValue as IList);
			// Values have changed, make sure we have the right selection
			SetSelection(_SelectedItem);
		}

		//****************************************

		private void RaiseSelectedItemChanged()
		{
			if (SelectedItemChanged != null)
				SelectedItemChanged(this, new EventArgs());
		}

		//****************************************

		private void OnValueChanged(object sender, EventArgs e)
		{
			if (_BubbleSelection)
			{
				if (_SelectedConverter == null)
					_SelectedItem = _SelectedMonitor.Value;
				else
					_SelectedItem = _SelectedConverter.ConvertTo(_SelectedMonitor.Value, null, null);

				SetSelection(_SelectedItem);
			}

			RaiseSelectedItemChanged();
		}

		//****************************************

		internal object SelectedItem
		{
			get { return _SelectedItem; }
		}

		protected bool BubbleSelection
		{
			get { return _BubbleSelection; }
			set { _BubbleSelection = value; }
		}
	}
}
