/****************************************\
 BindingDef.cs
 Created: 20-05-10
\****************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using Proximity.Gui.Data;
using Proximity.Gui.Presentation;
using Proximity.Gui.WinForms;
using Proximity.Gui.WinForms.Data;
//****************************************

namespace Proximity.Gui.WinForms.View
{
	internal class BindingDef
	{	//****************************************
		private string _SourcePath;
		private string _TargetPath;
		private IGuiConverter _Converter;
		private BindingMode _Mode;

		private List<BindingDef> _ChildBindings = new List<BindingDef>();
		//****************************************

		internal BindingDef(XmlReader reader)
		{
			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
				case "Source":
					_SourcePath = reader.Value;
					break;
					
				case "Target":
					_TargetPath = reader.Value;
					break;
					
				case "Mode":
					_Mode = (BindingMode)Enum.Parse(typeof(BindingMode), reader.Value);
					break;
					
				default:
					break;
				}
			}
			
			reader.MoveToElement();
			
			if (reader.IsEmptyElement)
				return;

			while (true)
			{
				reader.Read();
				if (reader.MoveToContent() == XmlNodeType.EndElement)
					break;

				switch (reader.LocalName)
				{
				case "Binding":
					_ChildBindings.Add(new BindingDef(reader));
					break;
					
				case "Converter":
					
					break;

				default:
					reader.Skip();
					break;
				}
			}
			
		}

		//****************************************

		internal void ApplyTo(GuiPresenter presenter, Component targetComponent)
		{
			
		}
	}
}
