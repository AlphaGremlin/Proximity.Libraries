/****************************************\
 ComponentDef.cs
 Created: 25-04-10
\****************************************/
using System;
using System.Collections.Generic;
using System.Xml;
using Proximity.Gui.Data;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.Templating
{
	/// <summary>
	/// Describes configuration data for a Component
	/// </summary>
	public sealed class ComponentDef
	{	//****************************************
		private GuiComponent _Component;
		//****************************************
		private Dictionary<string, StyleDef> _Styles = new Dictionary<string, StyleDef>();
		private Dictionary<string, ToolbarDef> _Toolbars = new Dictionary<string, ToolbarDef>();
		private Dictionary<string, IGuiConverter> _Converters = new Dictionary<string, IGuiConverter>();
		//****************************************
		
		public ComponentDef(GuiComponent component)
		{
			_Component = component;
		}
		
		public ComponentDef(GuiComponent component, XmlReader reader)
		{	//****************************************
			StyleDef NewStyle;
			ToolbarDef NewToolbar;
			IGuiConverter NewConverter;
			//****************************************
			
			_Component = component;
			
			//****************************************
			
			reader.ReadToFollowing("GuiComponent");

			while (reader.Read())
			{
				if (reader.MoveToContent() == XmlNodeType.EndElement)
					break;

				switch (reader.LocalName)
				{
				case "Styles":
					reader.MoveToElement();
					
					if (reader.IsEmptyElement)
						continue;
		
					while (true)
					{
						reader.Read();
						if (reader.MoveToContent() == XmlNodeType.EndElement)
							break;
		
						switch (reader.LocalName)
						{
						case "Style":
							NewStyle = new StyleDef(this, reader);
							
							_Styles.Add(NewStyle.Name, NewStyle);
							break;
		
						default:
							reader.Skip();
							break;
						}
					}
					break;
					
				case "Toolbars":
					reader.MoveToElement();
					
					if (reader.IsEmptyElement)
						continue;
		
					while (true)
					{
						reader.Read();
						if (reader.MoveToContent() == XmlNodeType.EndElement)
							break;
		
						switch (reader.LocalName)
						{
						case "Toolbar":
							NewToolbar = new ToolbarDef(this, reader);
							
							_Toolbars.Add(NewToolbar.Name, NewToolbar);
							break;
		
						default:
							reader.Skip();
							break;
						}
					}
					break;
					
				case "Converters":
					reader.MoveToElement();
					
					if (reader.IsEmptyElement)
						continue;
		
					while (true)
					{
						reader.Read();
						if (reader.MoveToContent() == XmlNodeType.EndElement)
							break;
		
						switch (reader.LocalName)
						{
						case "Converter":
							reader.Skip();
							
							// TODO: Custom Converters
							
							break;
		
						default:
							reader.Skip();
							break;
						}
					}
					break;

				default:
					reader.Skip();
					break;
				}
			}
		}
		
		//****************************************
		
		public ToolbarDef GetToolbar(string toolbarName)
		{	//****************************************
			ToolbarDef MyToolbar;
			//****************************************
			
			if (_Toolbars.TryGetValue(toolbarName, out MyToolbar))
				return MyToolbar;
			
			Log.Warning("Unknown Toolbar {0}", toolbarName);
			
			return null;
		}
				
		public StyleDef GetStyle(string styleName)
		{	//****************************************
			StyleDef MyStyle;
			//****************************************
			
			if (_Styles.TryGetValue(styleName, out MyStyle))
				return MyStyle;
			
			Log.Warning("Unknown Style {0}", styleName);
			
			return null;
		}
		
		//****************************************
		
		public GuiComponent Component
		{
			get { return _Component; }
		}
	}
}
