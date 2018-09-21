/****************************************\
 WpfChildController.cs
 Created: 27-05-10
\****************************************/
using System;
using System.Windows;
using System.Windows.Controls;
using Proximity.Gui.Presentation;
using Proximity.Gui.View;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.Wpf
{
	/// <summary>
	/// Description of WpfChildController.
	/// </summary>
	public class WpfChildController : GuiChildController
	{	//****************************************
		private UserControl _UserControl;
		private Type _TargetType;
		//****************************************
		
		internal WpfChildController(GuiChildPresenter presenter, GuiViewController parent, Type targetType) : base(presenter)
		{	//****************************************
			ContentControl ParentControl;
			//****************************************
			
			_TargetType = targetType;
			
			if (parent is WpfChildController)
				ParentControl = ((WpfChildController)parent).UserControl;
			else if (parent is WpfFormController)
				ParentControl = ((WpfFormController)parent).Window;
			else
				throw new ArgumentException("Not a WPF controller");
			
			_UserControl = WalkControls(ParentControl, targetType);
			_UserControl.DataContext = Presenter;
			_UserControl.Resources.Add("Proximity.Gui.Presentation.GuiPresenter", Presenter);
		}
		
		//****************************************
		
		private UserControl WalkControls(FrameworkElement parent, Type targetType)
		{	//****************************************
			UserControl ChildControl;
			//****************************************
			
			if (parent is ContentControl)
				return WalkControls(((ContentControl)parent).Content as FrameworkElement, targetType);
			
			if (parent is Panel)
			{
				foreach(FrameworkElement MyControl in ((Panel)parent).Children)
				{
					if (MyControl.GetType() == targetType)
						return (UserControl)MyControl;
				
					ChildControl = WalkControls(MyControl, targetType);
					
					if (ChildControl != null)
						return ChildControl;
				}
			}
			
			if (parent is ItemsControl)
			{
				foreach(FrameworkElement MyControl in ((ItemsControl)parent).Items)
				{
					if (MyControl.GetType() == targetType)
						return (UserControl)MyControl;
				
					ChildControl = WalkControls(MyControl, targetType);
					
					if (ChildControl != null)
						return ChildControl;
				}
			}
			
			return null;
		}
		
		//****************************************
		
		public override string Name
		{
			get { return _UserControl.Name; }
		}
		
		internal UserControl UserControl
		{
			get { return _UserControl; }
		}
	}
}
