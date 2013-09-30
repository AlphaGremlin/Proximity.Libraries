/****************************************\
 WinChildController.cs
 Created: 29-12-2008
\****************************************/
using System;
using System.ComponentModel;
using System.Windows.Forms;
using Proximity.Gui.Presentation;
using Proximity.Gui.View;
using Proximity.Gui.WinForms.Data;
using Proximity.Gui.WinForms.View;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.WinForms
{
	/// <summary>
	/// GuiChildController implementation for WinForms
	/// </summary>
	public class WinChildController : GuiChildController, IWinController
	{	//****************************************
		private UserControl _UserControl;

		private WinProvider _Provider;
		private ViewDef _ViewDef;
		//****************************************
		
		internal WinChildController(GuiChildPresenter presenter, GuiViewController parent, UserControl control) : base(presenter)
		{
			if (parent is IWinController)
				((IWinController)parent).Attach(this);
			else
				throw new ArgumentException("Not a WinForms controller");

			//****************************************
			
			_UserControl = control;
			_Provider = ((WinToolkit)GuiService.Toolkit).GetProvider(presenter.Host);
			_ViewDef = _Provider.GetViewDef(this.Name);
			
			//****************************************

			_UserControl.Enter += OnGotFocus;
			if (_UserControl.IsHandleCreated)
				OnLoad(_UserControl, EventArgs.Empty);
			else
				_UserControl.HandleCreated += OnLoad;
			
			_UserControl.Leave += OnLostFocus;
		}

		internal WinChildController(GuiChildPresenter presenter, GuiViewController parent, Type targetType) : base(presenter)
		{	//****************************************
			Control ParentControl;
			IWinController ParentController = parent as IWinController;
			//****************************************
			
			if (ParentController == null)
				throw new ArgumentException("Not a WinForms controller");
			
			ParentControl = ParentController.Control;
			ParentController.Attach(this);

			//****************************************
			
			// Search the controls underneath the parent for our desired UserControl
			_UserControl = WalkControls(ParentControl, targetType);
			
			if (_UserControl == null)
				throw new InvalidOperationException("Control does not exist");

			_UserControl.Enter += OnGotFocus;
			_UserControl.HandleCreated += OnLoad;
			_UserControl.Leave += OnLostFocus;

			//****************************************

			_Provider = ((WinToolkit)GuiService.Toolkit).GetProvider(presenter.Host);
			_ViewDef = _Provider.GetViewDef(this.Name);
		}
		
		//****************************************
		
		void IWinController.Attach(WinChildController child)
		{
			child.GotFocus += OnGotChildFocus;
			child.LostFocus += OnLostChildFocus;
		}
		
		private UserControl WalkControls(Control parent, Type targetType)
		{	//****************************************
			UserControl ChildControl;
			//****************************************
			
			foreach(Control MyControl in parent.Controls)
			{
				if (MyControl.GetType() == targetType)
					return (UserControl)MyControl;
				
				if (MyControl.Controls.Count == 0)
					continue;
				
				ChildControl = WalkControls(MyControl, targetType);
				
				if (ChildControl != null)
					return ChildControl;
			}
			
			return null;
		}

		//****************************************

		private void OnGotFocus(object sender, EventArgs e)
		{
			if (GotFocus != null)
				GotFocus(this, EventArgs.Empty);
		}
		
		private void OnGotChildFocus(object sender, EventArgs e)
		{
			if (GotFocus != null)
				GotFocus(sender, EventArgs.Empty);
		}

		private void OnLoad(object sender, EventArgs e)
		{	//****************************************
			WinBindingSource MyBindingSource;
			//****************************************
			
			if (_UserControl.Tag is WinBindingSource)
				MyBindingSource = (WinBindingSource)_UserControl.Tag;
			else
				_UserControl.Tag = MyBindingSource = new WinBindingSource(_UserControl);

			_ViewDef.ApplyTo(Presenter, _UserControl);

			MyBindingSource.Connect(Presenter);
		}

		private void OnLostFocus(object sender, EventArgs e)
		{
			if (LostFocus != null)
				LostFocus(this, EventArgs.Empty);
		}
		
		private void OnLostChildFocus(object sender, EventArgs e)
		{
			if (LostFocus != null)
				LostFocus(sender, EventArgs.Empty);
		}

		//****************************************
		
		public event EventHandler GotFocus;
		public event EventHandler LostFocus;

		public override string Name
		{
			get { return _UserControl.Name; }
		}
		
		internal UserControl UserControl
		{
			get { return _UserControl; }
		}
		
		Control IWinController.Control
		{
			get { return _UserControl; }
		}
	}
}
