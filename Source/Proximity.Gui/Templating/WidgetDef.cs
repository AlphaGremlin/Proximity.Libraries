/****************************************\
 WidgetDef.cs
 Created: 18-09-2008
\****************************************/
using System;
using System.Drawing;
using System.Reflection;
//****************************************

namespace Proximity.Gui.Templating
{
	/// <summary>
	/// Defines a Widget's icon and data linkages
	/// </summary>
	public abstract class WidgetDef
	{	//****************************************
		private string _Name;
		
		private Image _Icon;
		//****************************************
		
		protected WidgetDef(string name, Image icon)
		{
			_Name = name;
			_Icon = icon;
		}
		
		//****************************************
		
		public abstract EventHandler Connect(object target);
		
		//****************************************
		
		public string Name
		{
			get { return _Name; }
		}
		
		public Image Icon
		{
			get { return _Icon; }
		}
	}
}
