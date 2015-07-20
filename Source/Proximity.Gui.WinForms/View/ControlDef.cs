/****************************************\
 ControlDef.cs
 Created: 20-05-2010
\****************************************/
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using Proximity.Gui.Presentation;
using Proximity.Gui.Templating;
using Proximity.Gui.WinForms.Data;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.WinForms.View
{
	internal class ControlDef
	{	//****************************************
		private string _Name;
		private string _LocalPath;
		private bool _HasScanned;
		
		private ToolbarDef _Toolbar;
		private List<BindingDef> _Bindings = new List<BindingDef>();
		private List<LocalDef>_Locals = new List<LocalDef>();
		//****************************************

		internal ControlDef(string name)
		{
			_Name = name;
		}
		
		internal ControlDef(WinProvider provider, XmlReader reader)
		{
			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
				case "Name":
					_Name = reader.Value;
					break;
					
				case "Toolbar":
					_Toolbar = provider.Provider.Component.ComponentDef.GetToolbar(reader.Value);
					break;
					
				default:
					Log.Warning("Unknown attribute {0}", reader.LocalName);
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
					_Bindings.Add(new BindingDef(reader));
					break;

				default:
					reader.Skip();
					break;
				}
			}
		}

		//****************************************
		
		internal void ApplyTo(GuiPresenter presenter, Control targetControl)
		{
			if (!_HasScanned)
			{
				if (targetControl.Text != null)
					_LocalPath = targetControl.Text;
				
				if (targetControl is TabControl)
				{
					foreach(TabPage MyPage in ((TabControl)targetControl).TabPages)
					{
						_Locals.Add(new LocalDef(MyPage.Name, MyPage.Text));
					}
				}
				else if (targetControl is ListView)
				{
					foreach(ColumnHeader MyColumn in ((ListView)targetControl).Columns)
					{
						_Locals.Add(new LocalDef(MyColumn.Index, MyColumn.Text));
					}
				}
				
				_HasScanned = true;
			}
			
			//****************************************
			
			if (_Toolbar != null)
				ApplyToolbar(presenter, targetControl);
			
			//****************************************
			
			Localise(presenter, targetControl);
		}
		
		internal void Localise(GuiPresenter presenter, Control targetControl)
		{
			if (_LocalPath != null)
			{
				targetControl.Text = presenter.GetString(_LocalPath);
			}
			
			if (_Locals.Count != 0)
			{
				if (targetControl is TabControl)
				{
					foreach(LocalDef MyLocal in _Locals)
					{
						((TabControl)targetControl).TabPages[MyLocal.Name].Text = presenter.GetString(MyLocal.LocalPath);
					}
				}
				else if (targetControl is ListView)
				{
					foreach(LocalDef MyLocal in _Locals)
					{
						((ListView)targetControl).Columns[MyLocal.Index].Text = presenter.GetString(MyLocal.LocalPath);
					}
				}
			}
			
			if (targetControl is ToolStrip)
			{
				// TODO: Localise Toolstrip
			}
		}
		
		internal static bool IsControlManaged(Control targetControl)
		{
			if (targetControl is Label || targetControl is GroupBox || targetControl is ButtonBase)
				return true;
			
			if (targetControl is ListControl || targetControl is ListView || targetControl is TabControl)
				return true;
			
			return false;
		}
		
		//****************************************
		
		private void ApplyToolbar(GuiPresenter presenter, Control targetControl)
		{	//****************************************
			var MyToolStrip = (ToolStrip)targetControl;
			//****************************************
			
			foreach(ToolbarItemDef MyItemDef in _Toolbar.Items)
			{
				MyToolStrip.Items.Add(CreateToolbarItem(presenter, MyItemDef));
			}
		}
		
		private ToolStripItem CreateToolbarItem(GuiPresenter presenter, ToolbarItemDef itemDef)
		{	//****************************************
			ToolStripItem MyItem;
			//****************************************
			
			if (itemDef.Children != null)
			{
				if (!string.IsNullOrEmpty(itemDef.Command)) // Item has children and is clickable, use a Split Button
					MyItem = CreateSplitButton(presenter, itemDef);
				else // Children, but not clickable, use a Drop Down
					MyItem = CreateDropDownButton(presenter, itemDef);
			}
			else
			{
				if (itemDef.Style == null) // No style, use a Separator
					MyItem = new ToolStripSeparator();
				else if (!string.IsNullOrEmpty(itemDef.Command)) // Clickable, use a Button
					MyItem = CreateButton(presenter, itemDef);
				else // Not clickable, styled, use a Label
					MyItem = new ToolStripLabel();
			}
			
			//****************************************
			
			if (itemDef.Style != null)
			{
				if (itemDef.Style.Icon != null)
				{
					MyItem.Image = itemDef.Style.Icon;
					MyItem.ImageAlign = ContentAlignment.MiddleLeft;
					if (itemDef.HasText)
						MyItem.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
					else
						MyItem.DisplayStyle = ToolStripItemDisplayStyle.Image;
				}
				else
				{
					MyItem.DisplayStyle = ToolStripItemDisplayStyle.Text;
				}
			}
			
			switch (itemDef.Align)
			{
			case ToolbarItemAlign.Left:
				MyItem.Alignment = ToolStripItemAlignment.Left;
				break;
				
			case ToolbarItemAlign.Right:
				MyItem.Alignment = ToolStripItemAlignment.Right;
				break;
			}

			//****************************************
			
			return MyItem;
		}
		
		private ToolStripButton CreateButton(GuiPresenter presenter, ToolbarItemDef itemDef)
		{	//****************************************
			var MyButton = new ToolStripButton();
			//****************************************
			
			new WinCommand(itemDef.Command).AttachTo(presenter, MyButton);
			
			//****************************************
			
			return MyButton;
		}
		
		private ToolStripSplitButton CreateSplitButton(GuiPresenter presenter, ToolbarItemDef itemDef)
		{	//****************************************
			var MyButton = new ToolStripSplitButton();
			//****************************************
			
			new WinCommand(itemDef.Command).AttachTo(presenter, MyButton);
			
			foreach(ToolbarItemDef MyItemDef in itemDef.Children)
			{
				MyButton.DropDownItems.Add(CreateToolbarItem(presenter, MyItemDef));
			}
			
			//****************************************
			
			return MyButton;
		}
				
		private ToolStripDropDownButton CreateDropDownButton(GuiPresenter presenter, ToolbarItemDef itemDef)
		{	//****************************************
			var MyButton = new ToolStripDropDownButton();
			//****************************************
			
			foreach(ToolbarItemDef MyItemDef in itemDef.Children)
			{
				MyButton.DropDownItems.Add(CreateToolbarItem(presenter, MyItemDef));
			}
			
			//****************************************
			
			return MyButton;
		}
		
		//****************************************

		internal string Name
		{
			get { return _Name; }
		}
		
		internal string LocalPath
		{
			get { return _LocalPath; }
		}
	}
}
