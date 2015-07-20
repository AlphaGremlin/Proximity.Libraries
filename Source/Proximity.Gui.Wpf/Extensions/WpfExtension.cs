/****************************************\
 WpfExtension.cs
 Created: 26-07-10
\****************************************/
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.ComponentModel;
//****************************************

namespace Proximity.Gui.Wpf.Extensions
{
	/// <summary>
	/// Base class for Gui Library XAML Extensions
	/// </summary>
	public abstract class WpfExtension : MarkupExtension
	{	//****************************************
		private FrameworkElement _TargetElement, _ParentElement;
		//****************************************
		
		protected WpfExtension() : base()
		{
		}
		
		//****************************************
		
		protected FrameworkElement GetTargetElement(IServiceProvider serviceProvider)
		{	//****************************************
			IProvideValueTarget ValueTarget;
			//****************************************
			
			if (_TargetElement != null)
				return _TargetElement;
			
			//****************************************
			
			ValueTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
			
			if (ValueTarget == null || !(ValueTarget.TargetObject is FrameworkElement))
				return null;
			
			//****************************************

			_TargetElement = (FrameworkElement)ValueTarget.TargetObject;
			
			return _TargetElement;
		}
		
		protected FrameworkElement GetParentElement(IServiceProvider serviceProvider)
		{	//****************************************
			FrameworkElement MyElement;
			//****************************************
			
			MyElement = GetTargetElement(serviceProvider);

			if (MyElement == null)
				return null;
			
			//****************************************

			while (!(MyElement is Window || MyElement is UserControl))
			{
				MyElement = MyElement.Parent as FrameworkElement;
				
				if (MyElement == null)
					return null;
			}
			
			//****************************************
			
			_ParentElement = MyElement;
			
			return MyElement;
		}
				
		protected GuiProvider GetGuiProvider(IServiceProvider serviceProvider)
		{	//****************************************
			FrameworkElement MyElement;
			//****************************************
			
			MyElement = GetParentElement(serviceProvider);

			if (MyElement == null)
				return null;
			
			//****************************************
			
			return ((WpfToolkit)GuiService.Toolkit).FindProvider(MyElement.GetType());
		}
		
		//****************************************
		
		protected bool IsDesignMode
		{
			get { return (bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue); }
		}
	}
}
