/****************************************\
 WinAssistant.cs
 Created: 18-09-2008
\****************************************/
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using Proximity.Gui.Templating;
using Proximity.Gui.WinForms.Handlers;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.WinForms
{
	/// <summary>
	/// Windows Forms Assistant
	/// </summary>
	/*
	public abstract class WinAssistant : GuiPresenter
	{	//****************************************
		private List<WinToolbar> _Toolbars = new List<WinToolbar>();
		//****************************************
		
		protected WinAssistant(GuiComponent host, IGuiForm form) : base(host, form)
		{
		}
		
		//****************************************
		
		protected override void ApplyToolbar(ToolbarDef template, object target)
		{	//****************************************
			ToolStrip MyToolStrip = target as ToolStrip;
			//****************************************
			
			_Toolbars.Add(new WinToolbar(template, MyToolStrip, Form));
		}
		
		protected override void ApplyButton(WidgetDef template, object target)
		{
			
		}
		
		protected override void ApplyText(TextValue text)
		{	//****************************************
			Control[] MyControls;
			Control MyControl;
			//****************************************
			
			MyControls = (Form as Control).Controls.Find(text.Name, true);
			
			if (MyControls.Length == 0)
			{
				Log.Warning(string.Format("Component {0}, Form {1}, could not find Control {2}", Host.Name, this.Form.Name, text.Name));
				
				return;
			}
			
			MyControl = MyControls[0];
			
			if (text.Values != null)
			{
				if (MyControl is TabControl)
				{
					TabControl MyTabs = (TabControl)MyControl;
					
					if (MyTabs.TabCount != text.Values.Length)
						Log.Warning(string.Format("Component {0}, Form {1}, Control {2} does not have the right number of tabs", Host.Name, this.Form.Name, text.Name));
					else
					{
						for (int Index = 0; Index < text.Values.Length; Index++)
						{
							MyTabs.TabPages[Index].Text = text.Values[Index];
						}
					}
				}
				else if (MyControl is ComboBox)
				{
					ComboBox MyCombo = (ComboBox)MyControl;
					
					
				}
				else if (MyControl is ListView)
				{
					ListView MyListView = (ListView)MyControl;
					
					if (MyListView.Columns.Count != text.Values.Length)
						Log.Warning(string.Format("Component {0}, Form {1}, Control {2} does not have the right number of columns", Host.Name, this.Form.Name, text.Name));
					else
					{
						for (int Index = 0; Index < text.Values.Length; Index++)
						{
							MyListView.Columns[Index].Text = text.Values[Index];
						}
					}
				}
				else
					Log.Warning(string.Format("Component {0}, Form {1}, unable to apply text values to Control {2}", Host.Name, this.Form.Name, text.Name));
			}
			else
				MyControl.Text = text.Value;
		}
	}
	*/
}
