/****************************************\
 GtkExceptionWinow.cs
 Created: 17-07-10
\****************************************/
using System;
using Gtk;
using Proximity.Gui.Presentation;
using Proximity.Gui.View;
//****************************************

namespace Proximity.Gui.GtkSharp
{
	[CLSCompliant(false)]
	public partial class GtkExceptionWindow : Window
	{
		public GtkExceptionWindow(Exception exception) : base(WindowType.Toplevel)
		{
			this.Build();
		}
	}
}
