/****************************************\
 GuiViewController.cs
 Created: 25-04-10
\****************************************/
using System;
using Proximity.Gui.Presentation;
//****************************************

namespace Proximity.Gui.View
{
	/// <summary>
	/// View Controllers abstract the interface between a GuiPresenter and the underlying Toolkit
	/// </summary>
	public abstract class GuiViewController
	{	//****************************************
		private GuiPresenter _Presenter;
		//****************************************
		
		internal GuiViewController(GuiPresenter presenter)
		{
			_Presenter = presenter;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the name of the View being controlled
		/// </summary>
		public abstract string Name { get; }
		
		//****************************************
		
		/// <summary>
		/// Gets the Gui Presenter managing this controller
		/// </summary>
		public GuiPresenter Presenter
		{
			get { return _Presenter; }
		}
	}
}
