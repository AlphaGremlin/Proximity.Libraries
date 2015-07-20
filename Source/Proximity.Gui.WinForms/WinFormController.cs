/****************************************\
 WinFormController.cs
 Created: 13-09-2008
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
	/// GuiFormController implementation for WinForms
	/// </summary>
	public class WinFormController : GuiFormController, IWinController
	{	//****************************************
		private Form _Form;

		private WinProvider _Provider;
		private ViewDef _ViewDef;
		
		private WinChildController _ActiveChild;
		//****************************************
		
		internal WinFormController(GuiFormPresenter presenter, Form form) : base(presenter)
		{
			_Form = form;
			
			form.FormClosing += OnFormClosing;
			form.Activated += OnGotFocus;
			form.HandleCreated += OnLoad;
			form.Deactivate += OnLostFocus;

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
		
		protected override void Show(GuiFormPresenter parent)
		{
			((WinToolkit)GuiService.Toolkit).ShowDialog(parent, this);
		}
		
		protected override void Close()
		{
			_Form.Close();
		}

		//****************************************
		
		private void RefreshChildBindings()
		{
			
		}
		
		//****************************************

		private void OnFormClosing(object sender, FormClosingEventArgs e)
		{
			_Form.Validate(); // Forces validation on the focus control, otherwise we can skip it
			
			e.Cancel = base.OnClosing();
		}

		private void OnGotFocus(object sender, EventArgs e)
		{
			_Provider.Toolkit.SwitchPresenter(_ActiveChild == null ? (GuiPresenter)this.Presenter : _ActiveChild.Presenter);
			
			RefreshChildBindings();
		}

		private void OnLoad(object sender, EventArgs e)
		{	//****************************************
			WinBindingSource MyBindingSource = _Form.Tag as WinBindingSource;
			//****************************************
			
			_ViewDef.ApplyTo(Presenter, _Form);

			if (MyBindingSource != null)
				MyBindingSource.Connect(Presenter);
		}

		private void OnLostFocus(object sender, EventArgs e)
		{

		}
		
		private void OnGotChildFocus(object sender, EventArgs e)
		{
			_ActiveChild = (WinChildController)sender;
			
			if (_Form == Form.ActiveForm)
			{
				_Provider.Toolkit.SwitchPresenter(_ActiveChild == null ? (GuiPresenter)this.Presenter : _ActiveChild.Presenter);
				
				RefreshChildBindings();
			}
		}
		
		private void OnLostChildFocus(object sender, EventArgs e)
		{
			_ActiveChild = ((WinChildController)sender).Parent as WinChildController;
			
			if (_Form == Form.ActiveForm)
			{
				_Provider.Toolkit.SwitchPresenter(_ActiveChild == null ? (GuiPresenter)this.Presenter : _ActiveChild.Presenter);
				
				RefreshChildBindings();
			}
		}
		
		//****************************************

		public override string Name
		{
			get { return _Form.Name; }
		}

		protected override bool Visibility
		{
			get { return _Form.Visible; }
			set { _Form.Visible = value; }
		}
		
		internal Form Form
		{
			get { return _Form; }
		}
				
		Control IWinController.Control
		{
			get { return _Form; }
		}
	}
}
