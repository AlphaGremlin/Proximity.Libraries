/****************************************\
 InterfaceWidget.cs
 Created: 26-09-2008
\****************************************/
using System;
using System.Drawing;
using System.Reflection;
//****************************************

namespace Proximity.Gui.Templating.Widgets
{
	/// <summary>
	/// Widget that is bound to a specific interface method
	/// </summary>
	public class InterfaceWidget : WidgetDef
	{	//****************************************
		private MethodInfo ClickHandler;
		//****************************************
		
		public InterfaceWidget(string name, Image icon, MethodInfo clickHandler) : base(name, icon)
		{
			this.ClickHandler = clickHandler;
		}
		
		//****************************************
		
		public override EventHandler Connect(object target)
		{	//****************************************
			Type TargetType = target.GetType();
			//****************************************
			
			// Ensure the target implements our interface
			if (!ClickHandler.DeclaringType.IsAssignableFrom(TargetType))
				return null;
			
			return (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), target, ClickHandler);
		}
	}
}
