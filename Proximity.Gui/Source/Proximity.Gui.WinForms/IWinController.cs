/****************************************\
 IWinController.cs
 Created: 2011-04-29
\****************************************/
using System;
using System.ComponentModel;
using System.Windows.Forms;
using Proximity.Gui.Presentation;
using Proximity.Gui.View;
using Proximity.Gui.WinForms.Data;
using Proximity.Gui.WinForms.View;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.WinForms
{
	/// <summary>
	/// Represents a WinForms View Controller
	/// </summary>
	internal interface IWinController
	{
		/// <summary>
		/// Attaches a child View Controller
		/// </summary>
		/// <param name="child">The child to attach</param>
		void Attach(WinChildController child);
		
		/// <summary>
		/// Gets the underlying WinForms control
		/// </summary>
		Control Control { get; }
	}
}
