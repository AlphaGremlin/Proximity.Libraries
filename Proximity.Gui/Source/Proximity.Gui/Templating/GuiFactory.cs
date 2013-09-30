/****************************************\
 GuiFactory.cs
 Created: 26-09-2008
\****************************************/
using System;
using System.Drawing;
using System.Reflection;
using Proximity.Gui.Templating.Widgets;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.Templating
{
	/// <summary>
	/// Creates Widgets for linking Text/Icon pairs to events
	/// </summary>
	public static class GuiFactory
	{
		public static WidgetDef MakeWidget(string name, Image icon)
		{
			return new DudWidget(name, icon);
		}
		
		public static WidgetDef MakeWidget(string name, Image icon, string clickMethod)
		{
			return new FreeWidget(name, icon, clickMethod);
		}
		
		public static WidgetDef MakeWidget(string name, Image icon, EventHandler clickMethod)
		{
			return new StaticWidget(name, icon, clickMethod);
		}
		
		public static WidgetDef MakeWidget(string name, Image icon, Type clickType, string clickMethod)
		{	//****************************************
			MethodInfo MyMethod;
			//****************************************
			
			MyMethod = FindMethod(clickType, clickMethod);
			
			if (MyMethod == null)
			{
				Log.Warning("Unable to link widget method {0} on type {1}", clickMethod, clickType.Name);
				
				return null;
			}
			
			if (MyMethod.IsStatic)
				return new StaticWidget(name, icon, (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), MyMethod));
			
			return new InterfaceWidget(name, icon, MyMethod);
		}
		
		//****************************************
		
		internal static MethodInfo FindMethod(Type sourceType, string method)
		{	//****************************************
			Type TargetType = sourceType;
			MethodInfo MyMethod;
			//****************************************
			
			//****************************************
			// Find a matching public method
			
			MyMethod = TargetType.GetMethod(method, BindingFlags.Instance | BindingFlags.Public);
			
			if (MyMethod != null)
				return MyMethod;
			
			//****************************************
			// Find a matching private method instead
			
			while (TargetType != null)
			{
				MyMethod = TargetType.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
				
				if (MyMethod != null)
					return MyMethod;
				
				TargetType = TargetType.BaseType;
			}
			
			//****************************************
			
			return null;
		}
	}
}
