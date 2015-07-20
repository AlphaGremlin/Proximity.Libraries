/****************************************\
 StyleDef.cs
 Created: 25-04-10
\****************************************/
using System;
using System.Drawing;
using System.Xml;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.Templating
{
	/// <summary>
	/// Describes a Style, which holds a localised text value and an Icon
	/// </summary>
	public class StyleDef
	{	//****************************************
		private string _Name;
		
		private Image _Icon;
		//****************************************
		
		public StyleDef()
		{
		}
		
		public StyleDef(ComponentDef componentDef, XmlReader reader)
		{
			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
				case "Name":
					_Name = reader.Value;
					break;
					
				case "Icon":
					_Icon = (Image)componentDef.Component.ResourceManager.GetObject(reader.Value);
					break;
					
				default:
					Log.Warning("Unknown attribute {0}", reader.LocalName);
					break;
				}
			}
		}
		
		//****************************************
		
		public string Name
		{
			get { return _Name; }
			set { _Name = value; }
		}
		
		public Image Icon
		{
			get { return _Icon; }
			set { _Icon = value; }
		}
	}
}
