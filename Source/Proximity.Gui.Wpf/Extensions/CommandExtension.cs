/****************************************\
 CommandExtension.cs
 Created: 24-05-2010
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
	/// XAML Extension for attaching Commands
	/// </summary>
	[MarkupExtensionReturnType(typeof(ICommand))]
	public class CommandExtension : WpfExtension
	{	//****************************************
		private string _Command;
		//****************************************

		public CommandExtension() : base()
		{
		}
		
		public CommandExtension(string command) : base()
		{
			_Command = command;
		}

		//****************************************

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (IsDesignMode)
				return null;

			// Find the named command from the toolkits dictionary
			return ((WpfToolkit)GuiService.Toolkit).FindCommand(_Command);
		}

		//****************************************
		
		/// <summary>
		/// Gets/Sets the name of the Command
		/// </summary>
		[ConstructorArgument("command")]
		public string Command
		{
			get { return _Command; }
			set { _Command = value; }
		}
	}
}
