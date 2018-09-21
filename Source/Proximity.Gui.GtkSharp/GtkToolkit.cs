/****************************************\
 Toolkit.cs
 Created: 11-08-2008
\****************************************/
using System;
using System.Globalization;
using Gtk;
using Proximity.Gui.GtkSharp.Data;
using Proximity.Gui.Presentation;
using Proximity.Gui.Toolkit;
using Proximity.Gui.View;
//****************************************

namespace Proximity.Gui.GtkSharp
{
	/// <summary>
	/// GtkSharp Gui Toolkit
	/// </summary>
	public class GtkToolkit : GuiToolkit
	{	//****************************************
		
		public GtkToolkit()
		{
		}
		
		//****************************************
		
		public override void Init()
		{
			Application.Init();
		}
		
		public override void Run()
		{
			Application.Run();
		}
		
		public override void Exit()
		{
			Application.Quit();
		}
						
		//****************************************
		
		public override void ShowException(Exception e)
		{
			GtkExceptionWindow FormException = new GtkExceptionWindow(e);
			
			FormException.Show();
			
			Application.RunIteration(true);
		}
				
		public override void Register(GuiProvider provider)
		{
			
		}
		
		//****************************************
		
		public override GuiFormController CreateFormController(GuiFormPresenter presenter, object view)
		{
			return new GtkFormController(presenter, view as Window);
		}
		
		public override GuiChildController FindChildController(GuiChildPresenter presenter, GuiViewController parent, Type childType)
		{
			return new GtkChildController(presenter, parent, childType);
		}
		
		//****************************************
		
		internal void ShowDialog(GuiFormPresenter parent, GtkFormController dialog)
		{
			dialog.Window.ParentWindow = ((GtkFormController)GetView(parent)).Window.GdkWindow;
			dialog.Window.Show();
		}
		
		//****************************************
		
		protected override void OnCultureChanged(CultureInfo newCulture)
		{
			
		}
		
		//****************************************
		
		public override string Name
		{
			get { return "GtkSharp"; }
		}
	}
}
