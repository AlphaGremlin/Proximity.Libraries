/****************************************\
 StaticWidget.cs
 Created: 26-09-2008
\****************************************/
using System;
using System.Drawing;
using System.Reflection;
//****************************************

namespace Proximity.Gui.Templating.Widgets
{
	/// <summary>
	/// Defines a Widget assigned to a specific delegate
	/// </summary>
	public class StaticWidget : WidgetDef
	{	//****************************************
		private EventHandler ClickHandler;
		//****************************************
		
		public StaticWidget(string name, Image icon, EventHandler clickHandler) : base(name, icon)
		{
			this.ClickHandler = clickHandler;
		}
		
		//****************************************
		
		public override EventHandler Connect(object target)
		{
			return ClickHandler;
		}
	}
}
