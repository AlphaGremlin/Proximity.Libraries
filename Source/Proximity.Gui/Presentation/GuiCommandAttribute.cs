/****************************************\
 GuiCommandAttribute.cs
 Created: 24-05-10
\****************************************/
using System;
//****************************************

namespace Proximity.Gui.Presentation
{
	public delegate void GuiCommandHandler();
	public delegate bool GuiCommandCheckHandler();

	[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public sealed class GuiCommandAttribute : Attribute
	{	//****************************************
		private string _Name;
		private string _CheckMethod;
		//****************************************

		public GuiCommandAttribute()
		{
		}

		public GuiCommandAttribute(string name)
		{
			_Name = name;
		}

		public GuiCommandAttribute(string name, string checkMethod)
		{
			_Name = name;
			_CheckMethod = checkMethod;
		}

		//****************************************

		public string Name
		{
			get { return _Name; }
			set { _Name = value; }
		}

		public string CheckMethod
		{
			get { return _CheckMethod; }
			set { _CheckMethod = value; }
		}
	}
}
