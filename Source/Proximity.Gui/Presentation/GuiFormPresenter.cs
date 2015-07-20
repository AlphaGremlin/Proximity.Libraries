/****************************************\
 GuiPresenter.cs
 Created: 25-04-10
\****************************************/
using System;
using Proximity.Gui.View;
//****************************************

namespace Proximity.Gui.Presentation
{
	/// <summary>
	/// Represents a Presenter for a high-level Form
	/// </summary>
	public abstract class GuiFormPresenter : GuiPresenter
	{	//****************************************
		
		//****************************************
		
		protected GuiFormPresenter(GuiComponent host) : base(host)
		{
			base.View = host.Provider.GetFormController(this);
		}
		
		//****************************************

		/// <summary>
		/// Shows the view being presented
		/// </summary>
		public void Show()
		{
			View.Visibility = true;
		}
		
		/// <summary>
		/// Shows the view being presented
		/// </summary>
		/// <param name="parent">The parent to show modally against</param>
		public void Show(GuiFormPresenter parent)
		{
			View.Show(parent);
		}
		
		/// <summary>
		/// Closes the view being presented
		/// </summary>
		public void Close()
		{
			View.Close();
		}

		//****************************************
		
		protected internal virtual bool OnClosing()
		{
			return false;
		}

		//****************************************

		internal new GuiFormController View
		{
			get { return (GuiFormController)base.View; }
		}
	}
}
