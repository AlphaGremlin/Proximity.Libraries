/****************************************\
 WpfCommand.cs
 Created: 24-05-2010
\****************************************/
using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.ComponentModel;
using Proximity.Gui.Presentation;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.Wpf.Data
{
	/// <summary>
	/// Provides extra information on top of an existing RoutedCommand
	/// </summary>
	internal class WpfCommand : RoutedCommand
	{	//****************************************
		private string _TemplateName;
		//****************************************

		internal WpfCommand(string templateName) : base()
		{
			_TemplateName = templateName;
		}

		//****************************************

		/// <summary>
		/// Gets the name of the GuiCommandTemplate that this Command should execute
		/// </summary>
		internal string TemplateName
		{
			get { return _TemplateName; }
		}
	}
}
