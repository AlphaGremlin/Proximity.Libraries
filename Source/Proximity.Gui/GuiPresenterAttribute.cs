/****************************************\
 GuiPresenterAttribute.cs
 Created: 26-04-10
\****************************************/
using System;
//****************************************

namespace Proximity.Gui
{
	/// <summary>
	/// Declares the GuiPresenter to be used with a specific View class
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class GuiPresenterAttribute : Attribute
	{	//****************************************
		private Type _PresenterType;
		//****************************************
		
		public GuiPresenterAttribute(Type presenterType)
		{
			_PresenterType = presenterType;
		}
		
		//****************************************
		
		public Type PresenterType
		{
			get { return _PresenterType; }
		}
	}
}
