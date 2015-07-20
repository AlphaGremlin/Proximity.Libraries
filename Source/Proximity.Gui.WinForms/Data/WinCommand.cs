/****************************************\
 WinCommand.cs
 Created: 23-05-10
\****************************************/
using System;
using System.Windows.Forms;
using Proximity.Gui;
using Proximity.Gui.Presentation;
//****************************************

namespace Proximity.Gui.WinForms.Data
{
	/// <summary>
	/// WinForms Command Handler object
	/// </summary>
	internal class WinCommand
	{	//****************************************
		private GuiPresenter _Presenter;
		private string _Command;
		//****************************************
		
		internal WinCommand(string command)
		{
			_Command = command;
		}

		//****************************************

		internal void AttachTo(GuiPresenter presenter, System.ComponentModel.Component targetControl)
		{
			_Presenter = presenter;
			
			if (targetControl is ButtonBase)
			{
				(targetControl as ButtonBase).Click += OnCommand;
			}
			else if (targetControl is LinkLabel)
			{
				(targetControl as LinkLabel).Click += OnCommand;
			}
			else if (targetControl is ToolStripButton)
			{
				(targetControl as ToolStripButton).Click += OnCommand;
			}
			else if (targetControl is ToolStripDropDownButton)
			{
				(targetControl as ToolStripDropDownButton).Click += OnCommand;
			}
			else
				throw new ArgumentException("Target Control does not support Command bindings");
		}
		
		//****************************************
		
		private void OnCommand(object sender, EventArgs e)
		{
			((WinToolkit)GuiService.Toolkit).Execute(_Presenter, _Command);
		}
	}
}
