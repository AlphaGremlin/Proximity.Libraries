/****************************************\
 WinTabControl.cs
 Created: 2011-02-18
\****************************************/
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Proximity.Gui.Data;
using Proximity.Gui.Presentation;
using Proximity.Gui.View;
using Proximity.Gui.WinForms;
using Proximity.Gui.WinForms.Data;
//****************************************

namespace Proximity.Gui.WinForms.Data.Controls
{
	/// <summary>
	/// DataBinding support for TabControl
	/// </summary>
	internal class WinTabControl : WinBoundListControl
	{	//****************************************
		private TabControl _TabControl;
		private Dictionary<GuiChildPresenter, TabPage> _Pages = new Dictionary<GuiChildPresenter, TabPage>();
		
		private string[] _DisplayPath;
		private IGuiConverter _Converter;
		//****************************************

		internal WinTabControl(WinBindingSource source, Control control) : base(source, control)
		{
			_TabControl = (TabControl)control;
		}

		//****************************************
		
		internal void BindTitle(string sourcePath, IGuiConverter converter)
		{
			_DisplayPath = sourcePath.Split('.');
			_Converter = converter;
		}
		
		//****************************************
		
		protected override void SetSelection(object newValue)
		{
			foreach (TabPage MyPage in _TabControl.TabPages)
			{
				if (MyPage.Tag == newValue)
				{
					_TabControl.SelectedTab = MyPage;
					
					return;
				}
			}
			
			_TabControl.SelectedTab = null;
		}
		
		protected override void SetContents(IList contents)
		{	//****************************************
			Type ControlType;
			UserControl NewControl;
			TabPage NewPage;
			List<TabPage> MissingPages = new List<TabPage>();
			//****************************************

			if (contents == null)
			{
				_TabControl.TabPages.Clear();
				_Pages.Clear();
				
				return;
			}
			
			MissingPages.AddRange(_Pages.Values);

			foreach(GuiChildPresenter MyPresenter in contents)
			{
				if (_Pages.ContainsKey(MyPresenter))
				{
					MissingPages.Remove(_Pages[MyPresenter]);
					continue;
				}
				
				// Find the type of Control to add
				ControlType = MyPresenter.Host.Provider.PresenterMappings[MyPresenter.GetType()];
				
				// Create a new View for the Presenter
				NewControl = (UserControl)Activator.CreateInstance(ControlType);
				NewControl.Size = new Size(84, 84);
				NewControl.Location = new Point(8, 8);
				NewControl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
	
				// Attach the Presenter to the View via a Controller
				((WinToolkit)GuiService.Toolkit).AttachController(Source.Presenter, MyPresenter, NewControl);
				
				// Add the View Control to the Tab Control
				NewPage = new TabPage();
				NewPage.Size = new Size(100, 100);
				NewPage.Controls.Add(NewControl);
				NewPage.Tag = MyPresenter;
				NewPage.Text = GetTitle(MyPresenter);
				
				_Pages.Add(MyPresenter, NewPage);
				_TabControl.TabPages.Add(NewPage);
			}
			
			foreach(TabPage OldPage in MissingPages)
			{
				_TabControl.TabPages.Remove(OldPage);
			}
		}
		
		protected override object GetSelection()
		{
			return _TabControl.SelectedTab != null ? _TabControl.SelectedTab.Tag : null;
		}
		
		//****************************************
		
		private string GetTitle(GuiChildPresenter presenter)
		{	//****************************************
			object PropertyValue = WinBindingSource.GetFromPath(presenter, _DisplayPath);
			//****************************************
			
			if (_Converter != null)
				return (string)_Converter.ConvertTo(PropertyValue, typeof(string), null);
			else if (PropertyValue is string)
				return (string)PropertyValue;
			else if (PropertyValue != null)
				return PropertyValue.ToString();
			else
				return "";
		}
	}
}
