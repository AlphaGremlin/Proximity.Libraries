/****************************************\
 GtkChildController.cs
 Created: 26-04-10
\****************************************/
using System;
using Gtk;
using Proximity.Gui.Presentation;
using Proximity.Gui.View;
//****************************************

namespace Proximity.Gui.GtkSharp
{
	public class GtkChildController : GuiChildController
	{	//****************************************
		private Bin _Control;
		//****************************************
		
		public GtkChildController(GuiChildPresenter presenter, GuiViewController parent, Type targetType) : base(presenter)
		{	//****************************************
			Container ParentWidget;
			//****************************************
			
			if (parent is GtkChildController)
				ParentWidget = ((GtkChildController)parent).Control;
			else if (parent is GtkFormController)
				ParentWidget = ((GtkFormController)parent).Window;
			else
				throw new ArgumentException("Not a Gtk controller");

			//****************************************
			
			// Search the widgets underneath the parent for our desired user-control
			_Control = WalkWidgets(ParentWidget, targetType);

			//****************************************
		}
		
		//****************************************
		
		private Bin WalkWidgets(Container parent, Type targetType)
		{	//****************************************
			Bin ChildControl;
			//****************************************
			
			foreach(Widget MyWidget in parent.Children)
			{
				if (MyWidget.GetType() == targetType)
					return (Bin)MyWidget;
				
				if (!(MyWidget is Container) || ((Container)MyWidget).Children.Length == 0)
					continue;
				
				ChildControl = WalkWidgets((Container)MyWidget, targetType);
				
				if (ChildControl != null)
					return ChildControl;
			}
			
			return null;
		}

		
		//****************************************

		public override string Name
		{
			get { return _Control.Name; }
		}
		
		internal Gtk.Bin Control
		{
			get { return _Control; }
		}
	}
}
