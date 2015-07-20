/****************************************\
 GuiFormController.cs
 Created: 25-04-10
\****************************************/
using System;
using Proximity.Gui.Presentation;
//****************************************

namespace Proximity.Gui.View
{
	/// <summary>
	/// Represents a view Controller for a top-level (Form) Presenter
	/// </summary>
	public abstract class GuiFormController : GuiViewController
	{	//****************************************
		
		/// <summary>
		/// Creates a new Gui Form Controller
		/// </summary>
		/// <param name="presenter">The form presenter that will manage this controller</param>
		protected GuiFormController(GuiFormPresenter presenter) : base(presenter)
		{
		}
		
		//****************************************
		
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected bool OnClosing()
		{
			return Presenter.OnClosing();
		}
		
		/// <summary>
		/// Shows the form being controlled
		/// </summary>
		/// <param name="parent">An optional parent presenter to show modally</param>
		protected internal abstract void Show(GuiFormPresenter parent);
		
		/// <summary>
		/// Closes the form being controlled
		/// </summary>
		protected internal abstract void Close();
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets the visibility of the form being controlled
		/// </summary>
		protected internal abstract bool Visibility { get; set; }

		/// <summary>
		/// Gets the Gui Presenter managing this controller
		/// </summary>
		public new GuiFormPresenter Presenter
		{
			get { return (GuiFormPresenter)base.Presenter; }
		}
	}
}
