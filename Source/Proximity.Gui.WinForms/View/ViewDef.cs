/****************************************\
 ViewDef.cs
 Created: 26-04-10
\****************************************/
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using Proximity.Gui.Presentation;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.WinForms.View
{
	/// <summary>
	/// Describes the Bindings and Templating settings for a WinForms-based View
	/// </summary>
	internal class ViewDef
	{	//****************************************
		private string _Name;
		private bool _HasScanned;
		
		private string _LocalPath;

		private List<ControlDef> _Controls = new List<ControlDef>();
		//****************************************

		internal ViewDef(string name)
		{
			_Name = name;
		}
		
		internal ViewDef(WinProvider provider, XmlReader reader)
		{
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

			while (true)
			{
				reader.Read();
				if (reader.MoveToContent() == XmlNodeType.EndElement)
					break;

				switch (reader.LocalName)
				{
				case "Control":
					_Controls.Add(new ControlDef(provider, reader));
					break;

				default:
					reader.Skip();
					break;
				}
			}
		}

		//****************************************
		
		internal void ApplyTo(GuiPresenter presenter, ContainerControl targetControl)
		{	//****************************************
			Control[] MyControls;
			Control MyControl;
			//****************************************
			
			if (!_HasScanned)
			{
				_LocalPath = targetControl.Text;
				
				// First application, scan the view for localisation codes
				WalkControls(targetControl);
				
				_HasScanned = true;
			}
			
			//****************************************
			
			if (_LocalPath != null)
				targetControl.Text = presenter.GetString(_LocalPath);
			
			foreach(ControlDef MyControlDef in _Controls)
			{
				MyControls = targetControl.Controls.Find(MyControlDef.Name, true);
				
				if (MyControls.Length == 0)
					continue;
				
				MyControl = MyControls[0];
				
				MyControlDef.ApplyTo(presenter, MyControl);
				MyControlDef.Localise(presenter, MyControl);
			}
		}

		internal void Localise(GuiPresenter presenter, ContainerControl targetControl)
		{	//****************************************
			Control[] MyControls;
			Control MyControl;
			//****************************************
			
			foreach(ControlDef MyControlDef in _Controls)
			{
				MyControls = targetControl.Controls.Find(MyControlDef.Name, true);
				
				if (MyControls.Length == 0)
					continue;
				
				MyControl = MyControls[0];
				
				MyControlDef.Localise(presenter, MyControl);
			}
		}
		
		//****************************************
		
		private void WalkControls(Control parent)
		{
			foreach(Control MyControl in parent.Controls)
			{
				if (ControlDef.IsControlManaged(MyControl))
					_Controls.Add(new ControlDef(MyControl.Name));

				if (MyControl.Controls.Count == 0 || MyControl is UserControl)
					continue;
				
				WalkControls(MyControl);
			}
			
			return;
		}
		
		//****************************************

		internal string Name
		{
			get { return _Name; }
		}
		
		internal IList<ControlDef> Controls
		{
			get { return _Controls; }
		}
	}
}
