/****************************************\
 GtkCommand.cs
 Created: 23-05-10
\****************************************/
using System;
using Proximity.Gui.Presentation;
//****************************************

namespace Proximity.Gui.GtkSharp.Data
{
	/// <summary>
	/// Description of GtkCommand.
	/// </summary>
	internal class GtkCommand
	{	//****************************************
		private Delegate _Handler;
		//****************************************
		
		internal GtkCommand(Delegate handler)
		{
			_Handler = handler;
		}
		
		//****************************************
		
		public void Invoke(object param)
		{
			
		}
	}
}
