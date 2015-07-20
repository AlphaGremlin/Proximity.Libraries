/****************************************\
 WinToolItem.cs
 Created: 18-09-2008
\****************************************/
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Proximity.Gui.Templating;
//****************************************

namespace Proximity.Gui.WinForms.Handlers
{
	/// <summary>
	/// Windows Forms Toolbar Item Handler
	/// </summary>
	internal class WinToolItem
	{	//****************************************
		private ToolStripItem _Item;
		private List<WinToolItem> _Children;
		//****************************************
		
		internal WinToolItem(ToolbarItemDef itemDef, object target)
		{
			/*
			//****************************************
			bool HasClick = !(itemDef.Widget is DudWidget);
			bool ShowText = itemDef.ShowText;
			WinToolItem NewItem;
			
			ToolStripDropDownItem MyDropdown;
			//****************************************
			*/
			_Children = new List<WinToolItem>();
			_Item = new ToolStripButton();
			/*
			if (itemDef.Children.Count == 0)
			{
				if (HasClick)
					_Item = new ToolStripButton();
				else
				{
					_Item = new ToolStripLabel();
					
					ShowText = true; // Labels should always show text
				}
			}
			else
			{
				if (HasClick)
					MyDropdown = new ToolStripSplitButton();
				else
					MyDropdown = new ToolStripDropDownButton();
				
				// Add child items
				
				_Item = MyDropdown;
				
				foreach(ToolbarItemDef ChildItem in itemDef.Children)
				{
					if (ChildItem == null || ChildItem.Widget == null)
					{
						MyDropdown.DropDownItems.Add(new ToolStripSeparator());
						
						continue;
					}
					
					NewItem = new WinToolItem(ChildItem, target);
					
					_Children.Add(NewItem);
					
					MyDropdown.DropDownItems.Add(NewItem.Item);
				}				
			}
			
			// Bind click event
			if (HasClick)
				_Item.Click += itemDef.Widget.Connect(target);
			
			// Set up appearance
			if (itemDef.Widget.Icon == null)
				_Item.DisplayStyle = ToolStripItemDisplayStyle.Text;
			else if (ShowText)
				_Item.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
			else
				_Item.DisplayStyle = ToolStripItemDisplayStyle.Image;
			
			if (itemDef.Align == ToolbarItemAlign.Left)
				_Item.Alignment = ToolStripItemAlignment.Left;
			else
				_Item.Alignment = ToolStripItemAlignment.Right;
				*/
		}
		
		//****************************************
		
		internal ToolStripItem Item
		{
			get { return _Item; }
		}
	}
}
