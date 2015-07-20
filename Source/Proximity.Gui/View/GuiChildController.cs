/****************************************\
 GuiChildController.cs
 Created: 25-04-10
\****************************************/
using System;
using Proximity.Gui.Presentation;
//****************************************

namespace Proximity.Gui.View
{
	/// <summary>
	/// Represents a View Controller for a sub-level Presenter (eg: User Control implementing set functionality)
	/// </summary>
	public abstract class GuiChildController : GuiViewController
	{	//****************************************
		private GuiViewController _Parent;
		//****************************************
		
		/// <summary>
		/// Creates a new Gui Child Controller
		/// </summary>
		/// <param name="presenter">The child presenter who will manage this controller</param>
		protected GuiChildController(GuiChildPresenter presenter) : base(presenter)
		{
			_Parent = presenter.Parent.View;
		}
		
		//****************************************



		//****************************************

		/// <summary>
		/// Gets the parent View Controller containing this child
		/// </summary>
		public GuiViewController Parent
		{
			get { return _Parent; }
		}
		
		/// <summary>
		/// Gets the Gui Presenter managing this controller
		/// </summary>
		public new GuiChildPresenter Presenter
		{
			get { return (GuiChildPresenter)base.Presenter; }
		}
	}
}
