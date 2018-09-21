/****************************************\
 GuiProviderAttribute.cs
 Created: 26-04-10
\****************************************/
using System;
//****************************************

namespace Proximity.Gui
{
	/// <summary>
	/// In a Toolkit Provider Assembly, defines the Type that contains the GuiProvider implementation
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	public sealed class GuiProviderAttribute : Attribute
	{	//****************************************
		private Type _ProviderType;
		//****************************************
		
		public GuiProviderAttribute(Type providerType)
		{
			_ProviderType = providerType;
		}
		
		//****************************************
		
		public Type ProviderType
		{
			get { return _ProviderType; }
		}
	}
}
