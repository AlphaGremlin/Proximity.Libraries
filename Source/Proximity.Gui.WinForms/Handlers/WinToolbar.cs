/****************************************\
 WinToolbar.cs
 Created: 18-09-2008
\****************************************/
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using Proximity.Gui.Templating;
//****************************************

namespace Proximity.Gui.WinForms.Handlers
{
	/// <summary>
	/// Windows Forms Toolbar Handler
	/// </summary>
	internal class WinToolbar
	{	//****************************************
		private ToolStrip _ToolStrip;
		
		private List<WinToolItem> _Children;
		//****************************************
		
		public WinToolbar(ToolbarDef template, ToolStrip toolStrip, object target)
		{	//****************************************
			WinToolItem NewItem;
			//****************************************		
			
			_ToolStrip = toolStrip;
			_Children = new List<WinToolItem>();
			
			//****************************************
			
			toolStrip.SuspendLayout();
			
			foreach(ToolbarItemDef ChildItem in template.Items)
			{
				if (ChildItem == null || ChildItem.Style == null)
				{
					toolStrip.Items.Add(new ToolStripSeparator());
					
					continue;
				}
				
				NewItem = new WinToolItem(ChildItem, target);
				
				_Children.Add(NewItem);
				
				toolStrip.Items.Add(NewItem.Item);
			}
			
			toolStrip.ResumeLayout();			
		}
	}
}
