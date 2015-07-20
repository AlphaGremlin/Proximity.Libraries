/****************************************\
 ConverterExtension.cs
 Created: 26-07-2010
\****************************************/
using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.ComponentModel;
using Proximity.Gui.Wpf.Data;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.Wpf.Extensions
{
	/// <summary>
	/// XAML Extension for attaching Converters
	/// </summary>
	[MarkupExtensionReturnType(typeof(ICommand))]
	public class ConverterExtension : WpfExtension
	{	//****************************************
		private string _Name;
		//****************************************
		
		public ConverterExtension() : base()
		{
		}

		public ConverterExtension(string name) : base()
		{
			_Name = name;
		}
		
		//****************************************

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (IsDesignMode)
				return null;

			// Find the named converter from the service dictionary
			return new WpfConverter(GuiService.FindConverter(_Name));
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets the name of the Command
		/// </summary>
		[ConstructorArgument("name")]
		public string Name
		{
			get { return _Name; }
			set { _Name = value; }
		}
	}
}
