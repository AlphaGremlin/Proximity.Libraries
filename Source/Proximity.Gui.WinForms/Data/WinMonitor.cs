/****************************************\
 WinMonitor.cs
 Created: 28-05-10
\****************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
//****************************************

namespace Proximity.Gui.WinForms.Data
{
	internal class WinMonitor
	{	//****************************************
		internal event EventHandler ValueChanged;
		//****************************************
		private object _Target, _Value;
		private string _DataPath;
		private string[] _Segments;

		private bool _BubbleValue = true;
		//****************************************

		internal WinMonitor(string dataPath)
		{
			_DataPath = dataPath;
			_Segments = dataPath.Split('.');
		}

		//****************************************

		private void ChangeValue(object newValue)
		{
			if (_Value != null)
			{
				if (_Value is INotifyPropertyChanged)
					((INotifyPropertyChanged)_Value).PropertyChanged -= OnValueChanged;
			}

			_Value = newValue;

			if (_Value != null)
			{
				if (_Value is INotifyPropertyChanged)
					((INotifyPropertyChanged)_Value).PropertyChanged += OnValueChanged;
			}
			
			if (ValueChanged != null)
				ValueChanged(this, new EventArgs());
		}

		private void RefreshValue()
		{	//****************************************
			object NewValue = null;
			//****************************************

			if (_Target is WinBoundListControl)
				NewValue = ((WinBoundListControl)_Target).SelectedItem;
			else
				NewValue = _Target;

			//****************************************

			NewValue = WinBindingSource.GetFromPath(NewValue, _Segments);

			//****************************************
			
			ChangeValue(NewValue);
		}

		//****************************************

		private void OnTargetChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != _Segments[0])
				return;

			RefreshValue();
		}

		private void OnSelectionChanged(object sender, EventArgs e)
		{
			RefreshValue();
		}

		private void OnValueChanged(object sender, PropertyChangedEventArgs e)
		{
			if (_BubbleValue && ValueChanged != null)
				ValueChanged(this, new EventArgs());
		}

		//****************************************

		internal object Target
		{
			get { return _Target; }
			set
			{
				if (_Target is INotifyPropertyChanged)
					((INotifyPropertyChanged)_Target).PropertyChanged -= OnTargetChanged;
				else if (_Target is WinBoundListControl)
					((WinBoundListControl)_Target).SelectedItemChanged -= OnSelectionChanged;

				_Target = value;

				if (_Target is INotifyPropertyChanged)
					((INotifyPropertyChanged)_Target).PropertyChanged += OnTargetChanged;
				else if (_Target is WinBoundListControl)
					((WinBoundListControl)_Target).SelectedItemChanged += OnSelectionChanged;

				RefreshValue();
			}
		}

		internal object Value
		{
			get { return _Value; }
			set
			{	//****************************************
				object TargetObject;
				//****************************************

				if (_Target is WinBoundListControl)
					TargetObject = ((WinBoundListControl)_Target).SelectedItem;
				else
					TargetObject = _Target;

				try
				{
					_BubbleValue = false;

					WinBindingSource.SetToPath(TargetObject, _Segments, value);
				}
				finally
				{
					_BubbleValue = true;
				}

				ChangeValue(value);
			}
		}
	}
}
