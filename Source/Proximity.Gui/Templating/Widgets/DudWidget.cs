/****************************************\
 DudWidget.cs
 Created: 29-12-2008
\****************************************/
using System;
using System.Drawing;
//****************************************

namespace Proximity.Gui.Templating.Widgets
{
	/// <summary>
	/// Dummy widget for simply displaying text
	/// </summary>
	public class DudWidget : WidgetDef
	{
		public DudWidget(string name, Image icon) : base(name, icon)
		{
		}
		
		public override EventHandler Connect(object target)
		{
			return null;
		}
	}
}
