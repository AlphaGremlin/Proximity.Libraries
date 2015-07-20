/****************************************\
 LocalExtension.cs
 Created: 28-05-2009
\****************************************/
using System;
using System.Windows;
using System.Windows.Markup;
using System.ComponentModel;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.Wpf
{
	/// <summary>
	/// XAML Extension to provide localisation
	/// </summary>
	public abstract class LocalExtension<TType> : MarkupExtension, INotifyPropertyChanged
	{	//****************************************
		private string _Key;
		
		private EventHandler<PropertyChangedEventArgs> _PropertyChanged;
		//****************************************
		
		protected LocalExtension()
		{
		}
		
		protected LocalExtension(string key)
		{
			_Key = key;
		}
		
		//****************************************
		
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return null;
		}
		
		public event PropertyChangedEventHandler PropertyChanged
		{
			add
			{
				_PropertyChanged += new WeakHandler<PropertyChangedEventArgs>(value, delegate(EventHandler<PropertyChangedEventArgs> handler)
	      {
	        _PropertyChanged -= handler;
	      });
			}
			remove
			{
				if (_PropertyChanged == null)
					return;
				
				_PropertyChanged -= WeakHandler<PropertyChangedEventArgs>.FindHandler(_PropertyChanged, value);
			}
		}
	}
}
