/****************************************\
 Toolkit.cs
 Created: 11-08-2008
\****************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using Proximity.Gui;
using Proximity.Gui.Presentation;
using Proximity.Gui.Toolkit;
using Proximity.Gui.View;
using Proximity.Gui.WinForms.Data;
using Proximity.Gui.WinForms.View;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.WinForms
{
	/// <summary>
	/// Windows Forms Gui Toolkit
	/// </summary>
	public class WinToolkit : GuiToolkit
	{	//****************************************
		private ApplicationContext MyContext;

		private Dictionary<string, WinProvider> _Providers = new Dictionary<string, WinProvider>();

		private GuiPresenter _ActivePresenter;
		//****************************************
		
		public WinToolkit()
		{
		}
		
		//****************************************
		
		public override void Init()
		{
			MyContext = new ApplicationContext();
			
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
		}
		
		public override void Run()
		{
			Application.Run(MyContext);
		}
		
		public override void Exit()
		{
			MyContext.ExitThread();

			_Providers.Clear();
		}

		//****************************************

		public override void ShowException(Exception e)
		{	//****************************************
			ExceptionForm FormException = new ExceptionForm(e, false);
			//****************************************
			
			FormException.ShowDialog();
		}
		
		public override void Register(GuiProvider provider)
		{	//****************************************
			WinProvider MyProvider;
			//****************************************

			_Providers.Add(provider.Component.Name, MyProvider = new WinProvider(this, provider));

			MyProvider.Load();
		}
		
		//****************************************
		
		public override GuiFormController CreateFormController(GuiFormPresenter presenter, object view)
		{
			return new WinFormController(presenter, view as Form);
		}
		
		public override GuiChildController FindChildController(GuiChildPresenter presenter, GuiViewController parent, Type childType)
		{
			return new WinChildController(presenter, parent, childType);
		}

		//****************************************

		internal void SwitchPresenter(GuiPresenter presenter)
		{
			_ActivePresenter = presenter;
		}

		internal WinProvider GetProvider(GuiComponent component)
		{	//****************************************
			WinProvider MyProvider;
			//****************************************

			if (_Providers.TryGetValue(component.Name, out MyProvider))
				return MyProvider;
			
			throw new GuiException("Provider is not registered");
		}
		
		internal void AttachController(GuiPresenter parent, GuiChildPresenter presenter, UserControl control)
		{
			var MyParentView = GetView(parent);
			
			var MyController = new WinChildController(presenter, MyParentView, control);
			
			SetView(presenter, MyController);
		}
		
		internal void ShowDialog(GuiFormPresenter parent, WinFormController dialog)
		{
			dialog.Form.ShowDialog(((WinFormController)GetView(parent)).Form);
		}

		//****************************************

		internal void Execute(GuiPresenter parent, string commandName)
		{	//****************************************
			GuiPresenter CurrentPresenter = parent;
			GuiCommandTemplate MyTemplate;
			//****************************************

			while (CurrentPresenter != null)
			{
				MyTemplate = CurrentPresenter.Host.GetCommandTemplate(CurrentPresenter, commandName);
				
				if (MyTemplate != null && (!MyTemplate.CanCheckExecute || MyTemplate.CheckExecute(CurrentPresenter)))
				{
					MyTemplate.Execute(CurrentPresenter);
					
					return;
				}

				CurrentPresenter = CurrentPresenter.Parent;
			}
			
			// Not found on the parent, try the active context
			if (parent != _ActivePresenter)
			{
				Execute(_ActivePresenter, commandName);
				
				return;
			}

			Log.Warning("Command {0} was not found in the Presenter hierarchy", commandName);
			
			//throw new InvalidOperationException(string.Format("Command {0} was not found in the Presenter hierarchy", commandName));
		}

		//****************************************
		
		protected override void OnCultureChanged(CultureInfo newCulture)
		{
			
		}
		
		//****************************************
		
		public override string Name
		{
			get { return "WinForms"; }
		}
		
		internal GuiPresenter ActivePresenter
		{
			get { return _ActivePresenter; }
		}
	}
}
