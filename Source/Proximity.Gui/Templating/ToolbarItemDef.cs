/****************************************\
 ToolbarItemDef.cs
 Created: 18-09-2008
\****************************************/
using System;
using System.Collections.Generic;
using System.Xml;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.Templating
{
	public enum ToolbarItemAlign
	{
		Left,
		Right
	}
	
	/// <summary>
	/// Defines a single Item on a Toolbar
	/// </summary>
	public class ToolbarItemDef
	{	//****************************************
		private StyleDef _Style;
		
		private List<ToolbarItemDef> _Children;
		
		private ToolbarItemAlign _Align;
		private bool _HasText;
		private string _Command;
		//****************************************
		
		public ToolbarItemDef(StyleDef style)
		{
			_Style = style;
			
			_Children  = new List<ToolbarItemDef>();
		}
				
		public ToolbarItemDef(StyleDef style, ToolbarItemAlign align, bool hasText)
		{
			_Style = style;
			_Align = align;
			_HasText = hasText;
			
			_Children  = new List<ToolbarItemDef>();
		}
		
		public ToolbarItemDef(StyleDef style, IList<ToolbarItemDef> children)
		{
			_Style = style;
			
			_Children = new List<ToolbarItemDef>(children);
		}
		
		internal ToolbarItemDef(ComponentDef componentDef, XmlReader reader)
		{
			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
				case "Style":
					_Style = componentDef.GetStyle(reader.Value);
					break;
					
				case "Align":
					_Align = (ToolbarItemAlign)Enum.Parse(typeof(ToolbarItemAlign), reader.Value);
					break;
					
				case "HasText":
					_HasText = bool.Parse(reader.Value);
					break;
					
				case "Command":
					_Command = reader.Value;
					break;

				default:
					Log.Warning("Unknown attribute {0}", reader.LocalName);
					break;
				}
			}
			
			reader.MoveToElement();
			
			if (reader.IsEmptyElement)
				return;
			
			_Children = new List<ToolbarItemDef>();
			
			while (reader.Read())
			{
				if (reader.MoveToContent() == XmlNodeType.EndElement)
					break;

				switch (reader.LocalName)
				{
				case "Item":
					ToolbarItemDef MyItem = new ToolbarItemDef(componentDef, reader);
					
					_Children.Add(MyItem);
					break;
					
				default:
					reader.Skip();
					break;
				}
			}
		}
		
		//****************************************
		
		public StyleDef Style
		{
			get { return _Style; }
		}
		
		public IList<ToolbarItemDef> Children
		{
			get { return _Children; }
		}
		
		public bool HasText
		{
			get { return _HasText; }
			set { _HasText = value; }
		}
		
		public ToolbarItemAlign Align
		{
			get { return _Align; }
			set { _Align = value; }
		}
		
		public string Command
		{
			get { return _Command; }
			set { _Command = value; }
		}
	}
}
