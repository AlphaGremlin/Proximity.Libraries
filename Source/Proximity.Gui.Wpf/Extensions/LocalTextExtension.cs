/****************************************\
 LocalTextExtension.cs
 Created: 28-05-2009
\****************************************/
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.ComponentModel;
using Proximity.Gui.Wpf.Data;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.Wpf.Extensions
{
	/// <summary>
	/// XAML Extension to provide localisation
	/// </summary>
	[MarkupExtensionReturnType(typeof(string))]
	public class LocalTextExtension : WpfExtension
	{	//****************************************
		private string _Path;
		//****************************************

		public LocalTextExtension() : base()
		{
		}

		public LocalTextExtension(string path) : base()
		{
			_Path = path;
		}

		//****************************************
		
		public override object ProvideValue(IServiceProvider serviceProvider)
		{	//****************************************
			Binding MyBinding;
			//****************************************
			
			if (IsDesignMode)
				return _Path;
			
			//****************************************
			
			MyBinding = new Binding("Value");
			MyBinding.Source = new WpfLocalValue(GetTargetElement(serviceProvider), _Path);

			return MyBinding.ProvideValue(serviceProvider);
		}

		//****************************************

		/// <summary>
		/// Gets/Sets the localised text path
		/// </summary>
		[ConstructorArgument("path")]
		public string Path
		{
			get { return _Path; }
			set { _Path = value; }
		}
	}
}
