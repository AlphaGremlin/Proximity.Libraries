/****************************************\
 ToolbarExtension.cs
 Created: 26-05-2010
\****************************************/
using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.ComponentModel;
using Proximity.Gui.Wpf.Data;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.Wpf.Extensions
{
	/// <summary>
	/// XAML extension for providing the list of items for a toolbar
	/// </summary>
	public class ToolbarExtension : MarkupExtension
	{	//****************************************
		private string _Toolbar;
		//****************************************

		public ToolbarExtension()
			: base()
		{
		}

		public ToolbarExtension(string toolbar)
			: base()
		{
			_Toolbar = toolbar;
		}

		//****************************************

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return null;
		}

		//****************************************

		public string Toolbar
		{
			get { return _Toolbar; }
			set { _Toolbar = value; }
		}
	}
}
