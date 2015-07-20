/****************************************\
 GtkFormController.cs
 Created: 26-04-10
\****************************************/
using System;
using Gtk;
using Proximity.Gui.Presentation;
using Proximity.Gui.View;
//****************************************

namespace Proximity.Gui.GtkSharp
{
	/// <summary>
	/// Description of GtkFormController.
	/// </summary>
	public class GtkFormController : GuiFormController
	{	//****************************************
		private Window _Window;
		//****************************************
		
		internal GtkFormController(GuiFormPresenter presenter, Window window) : base(presenter)
		{
			_Window = window;
			
			
			
		}
		
		//****************************************
		
		protected override void Show(GuiFormPresenter parent)
		{
			((GtkToolkit)GuiService.Toolkit).ShowDialog(parent, this);
		}
		
		protected override void Close()
		{
			_Window.Destroy();
		}
		
		//****************************************

		public override string Name
		{
			get { return _Window.Name; }
		}

		protected override bool Visibility
		{
			get { return _Window.Visible; }
			set { _Window.Visible = value; }
		}
		
		internal Window Window
		{
			get { return _Window; }
		}
	}
}
