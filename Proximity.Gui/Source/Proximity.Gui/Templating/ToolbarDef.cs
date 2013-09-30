/****************************************\
 ToolbarDef.cs
 Created: 18-09-2008
\****************************************/
using System;
using System.Xml;
using System.Collections.Generic;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.Templating
{
	/// <summary>
	/// Defines a Toolbar to be shared among Toolkits
	/// </summary>
	public class ToolbarDef
	{	//****************************************
		private string _Name;
		
		private List<ToolbarItemDef> _Items;
		//****************************************
				
		public ToolbarDef(string name)
		{
			_Name = name;
			
			_Items = new List<ToolbarItemDef>();
		}
		
		public ToolbarDef(string name, IList<ToolbarItemDef> items)
		{
			_Name = name;
			
			_Items = new List<ToolbarItemDef>(items);
		}
		
		internal ToolbarDef(ComponentDef componentDef, XmlReader reader)
		{
			_Items = new List<ToolbarItemDef>();
			
			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
				case "Name":
					_Name = reader.Value;
					break;
					
				default:
					Log.Warning("Unknown attribute {0}", reader.LocalName);
					break;
				}
			}
			
			reader.MoveToElement();
			
			if (reader.IsEmptyElement)
				return;
			
			while (reader.Read())
			{
				if (reader.MoveToContent() == XmlNodeType.EndElement)
					break;

				switch (reader.LocalName)
				{
				case "Item":
					ToolbarItemDef MyItem = new ToolbarItemDef(componentDef, reader);
					
					_Items.Add(MyItem);
					break;
					
				default:
					reader.Skip();
					break;
				}
			}
		}
		
		//****************************************
		
		public string Name
		{
			get { return _Name; }
		}
		
		public IList<ToolbarItemDef> Items
		{
			get { return _Items; }
		}
	}
}
