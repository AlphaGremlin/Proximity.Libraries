/****************************************\
 ExceptionForm.cs
 Created: 13-09-2008
\****************************************/
using Gtk;
using System;
//****************************************

namespace Proximity.Gui.GtkSharp
{
	/// <summary>
	/// Displays Exceptions to the user
	/// </summary>
	internal class ExceptionForm : Window
	{
		public ExceptionForm(Exception e) : base("GtkExceptionForm")
		{
		}
	}
}
