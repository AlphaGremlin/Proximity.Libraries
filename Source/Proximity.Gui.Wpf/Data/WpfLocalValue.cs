/****************************************\
 WpfLocalValue.cs
 Created: 26-07-10
\****************************************/
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Proximity.Gui.Data;
using Proximity.Gui.Presentation;
//****************************************

namespace Proximity.Gui.Wpf.Data
{
	/// <summary>
	/// Description of WpfLocalValue.
	/// </summary>
	internal class WpfLocalValue : INotifyPropertyChanged
	{	//****************************************
		public event PropertyChangedEventHandler PropertyChanged;
		//****************************************
		private GuiPresenter _Presenter;
		private FrameworkElement _TargetElement;

		private string _Path, _Value;
		//****************************************
		
		internal WpfLocalValue(FrameworkElement targetElement, string path)
		{
			_TargetElement = targetElement;
			_Path = path;

			GuiService.CultureChanged += OnCultureChanged;
			
			((WpfToolkit)GuiService.Toolkit).Attach += OnAttach;
		}
		
		//****************************************
		
		private void OnAttach(object sender, EventArgs e)
		{
			_Presenter = (GuiPresenter)_TargetElement.TryFindResource("Proximity.Gui.Presentation.GuiPresenter");
			
			// No Presenter yet, the attach event wasn't for us
			if (_Presenter == null)
				return;
			
			//****************************************
			
			Value = _Presenter.GetString(_Path);
			
			((WpfToolkit)GuiService.Toolkit).Attach -= OnAttach;
		}
		
		private void OnCultureChanged(object sender, EventArgs e)
		{
			Value = _Presenter.GetString(_Path);
		}
		
		//****************************************
		
		public string Value
		{
			get
			{
				if (_Presenter == null)
					return string.Empty;
				
				return _Value;
			}
			private set
			{
				if (value == _Value)
					return;
				
				_Value = value;
				
				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("Value"));				
			}
		}
	}
}
