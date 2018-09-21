/****************************************\
 GuiFormPresenter.cs
 Created: 25-04-10
\****************************************/
using System;
using Proximity.Gui.View;
//****************************************

namespace Proximity.Gui.Presentation
{
	/// <summary>
	/// Description of GuiChildPresenter.
	/// </summary>
	public abstract class GuiChildPresenter : GuiPresenter
	{
		protected GuiChildPresenter(GuiPresenter parent) : base(parent.Host)
		{
			Parent = parent;
			
			base.View = Host.Provider.GetChildController(this, parent.View);
		}
		
		protected GuiChildPresenter(GuiPresenter parent, bool attachController) : base(parent.Host)
		{
			Parent = parent;
			
			if (attachController)
				base.View = Host.Provider.GetChildController(this, parent.View);
		}

		//****************************************

		internal new GuiChildController View
		{
			get { return (GuiChildController)base.View; }
		}
	}
}
