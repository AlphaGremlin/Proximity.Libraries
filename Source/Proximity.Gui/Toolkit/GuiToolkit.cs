/****************************************\
 GuiToolkit.cs
 Created: 11-08-2008
\****************************************/
using System;
using System.Globalization;
using System.Collections.Generic;
using Proximity.Gui;
using Proximity.Gui.Presentation;
using Proximity.Gui.View;
//****************************************

namespace Proximity.Gui.Toolkit
{
	/// <summary>
	/// Represents a GUI Toolkit
	/// </summary>
	public abstract class GuiToolkit
	{	//****************************************
		
		protected GuiToolkit()
		{
		}
		
		//****************************************
		
		public abstract void Init();
		
		public abstract void Run();
		
		public abstract void Exit();
				
		public abstract void ShowException(Exception e);

		public abstract void Register(GuiProvider provider);
		
		//****************************************
		
		public abstract GuiFormController CreateFormController(GuiFormPresenter presenter, object view);
		
		public abstract GuiChildController FindChildController(GuiChildPresenter presenter, GuiViewController parent, Type childType);

		//****************************************
		
		internal void CultureChanged()
		{
			OnCultureChanged(GuiService.Culture);
		}
		
		//****************************************
		
		protected abstract void OnCultureChanged(CultureInfo newCulture);
		
		protected GuiViewController GetView(GuiPresenter presenter)
		{
			return presenter.View;
		}
		
		protected void SetView(GuiPresenter presenter, GuiViewController controller)
		{
			if (presenter.View != null)
				throw new InvalidOperationException("View has already been set");
			
			presenter.View = controller;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the internal name of this Gui Toolkit
		/// </summary>
		public abstract string Name { get; }
	}
}
