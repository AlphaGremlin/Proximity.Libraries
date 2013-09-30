/****************************************\
 GuiPresenter.cs
 Created: 13-09-2008
\****************************************/
using System;
using System.ComponentModel;
using Proximity.Gui.Templating;
using Proximity.Gui.View;
//****************************************

namespace Proximity.Gui.Presentation
{
	/// <summary>
	/// Base Class for Presentation Managers
	/// </summary>
	public abstract class GuiPresenter : MarshalByRefObject, INotifyPropertyChanged
	{	//****************************************
		public event PropertyChangedEventHandler PropertyChanged;
		//****************************************
		private GuiComponent _Host;
		private GuiViewController _View;
		private GuiPresenter _Parent;
		//****************************************
		
		internal GuiPresenter(GuiComponent host)
		{
			_Host = host;
		}
		
		//****************************************
		
		public string GetString(string name)
		{	//****************************************
			string FinalValue = _Host.LocalisationManager.GetString(name);
			//****************************************
			
			
			
			return FinalValue;
		}
		
		//****************************************

		protected void RaisePropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		
		protected void RaiseCommand(string commandName)
		{
			
		}

		//****************************************
		
		public GuiComponent Host
		{
			get { return _Host; }
		}

		internal GuiViewController View
		{
			get { return _View; }
			set
			{
				_View = value;

				RaisePropertyChanged("View");
			}
		}

		public GuiPresenter Parent
		{
			get { return _Parent; }
			protected set
			{
				if (_Parent != null)
					throw new InvalidOperationException("Cannot change the Parent");
				
				_Parent = value;
			}
		}
	}
}
