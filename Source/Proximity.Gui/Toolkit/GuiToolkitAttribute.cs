/****************************************\
 GuiToolkitAttribute.cs
 Created: 29-05-10
\****************************************/
using System;
//****************************************

namespace Proximity.Gui.Toolkit
{
	/// <summary>
	/// In a Toolkit Assembly, defines the Type that contains the GuiToolkit implementation
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	public sealed class GuiToolkitAttribute : Attribute
	{	//****************************************
		private Type _ToolkitType;
		//****************************************
		
		public GuiToolkitAttribute(Type toolkitType)
		{
			_ToolkitType = toolkitType;
		}
		
		//****************************************
		
		public Type ToolkitType
		{
			get { return _ToolkitType; }
		}
	}
}
