/****************************************\
 FreeWidget.cs
 Created: 26-09-2008
\****************************************/
using System;
using System.Drawing;
using System.Reflection;
//****************************************

namespace Proximity.Gui.Templating.Widgets
{
	/// <summary>
	/// Widget that is freely bound to a method when it is created
	/// </summary>
	public class FreeWidget : WidgetDef
	{	//****************************************
		private string ClickHandler;
		//****************************************
		
		public FreeWidget(string name, Image icon, string clickHandler) : base(name, icon)
		{
			this.ClickHandler = clickHandler;
		}
		
		//****************************************
		
		public override EventHandler Connect(object target)
		{	//****************************************
			MethodInfo MyMethod;
			//****************************************
			
			MyMethod = GuiFactory.FindMethod(target.GetType(), ClickHandler);
			
			if (MyMethod == null)
				return null;
			
			//****************************************

			return (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), target, MyMethod);
		}
	}
}
