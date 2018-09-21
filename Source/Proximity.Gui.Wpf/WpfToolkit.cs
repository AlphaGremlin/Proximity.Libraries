/****************************************\
 WpfToolkit.cs
 Created: 28-05-2009
\****************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Proximity.Gui;
using Proximity.Gui.Presentation;
using Proximity.Gui.Toolkit;
using Proximity.Gui.View;
using Proximity.Gui.Wpf.Data;
using Proximity.Utility;
using Proximity.Utility.Events;
//****************************************

namespace Proximity.Gui.Wpf
{
	/// <summary>
	/// Windows Presentation Foundation Toolkit
	/// </summary>
	public class WpfToolkit : GuiToolkit
	{	//****************************************
		private WpfApplication _Application;

		private Dictionary<string, WpfCommand> _Commands = new Dictionary<string, WpfCommand>();
		private Dictionary<Type, GuiProvider> _ProviderMappings = new Dictionary<Type, GuiProvider>();
		
		private WeakEvent<EventArgs> _Attach = new WeakEvent<EventArgs>();
		//****************************************
		
		public WpfToolkit()
		{
		}
		
		//****************************************
		
		public override void Init()
		{
			_Application = new WpfApplication();
		}
		
		public override void Run()
		{
			_Application.Run();
		}
		
		public override void Exit()
		{
			_Application.Shutdown();
			_Application = null;
		}
		
		public override void ShowException(Exception e)
		{
			
		}
		
		public override void Register(GuiProvider provider)
		{
			foreach(string commandName in provider.Component.ListCommands())
			{
				if (_Commands.ContainsKey(commandName))
					continue;
				    
				_Commands.Add(commandName, new WpfCommand(commandName));
			}
			
			foreach(Type MyViewType in provider.PresenterMappings.Values)
			{
				_ProviderMappings.Add(MyViewType, provider);
			}
		}
				
		//****************************************
		
		public override GuiFormController CreateFormController(GuiFormPresenter presenter, object view)
		{
			return new WpfFormController(presenter, view as Window);
		}
		
		public override GuiChildController FindChildController(GuiChildPresenter presenter, GuiViewController parent, Type childType)
		{
			return new WpfChildController(presenter, parent, childType);
		}
		
		internal WpfCommand FindCommand(string templateName)
		{	//****************************************
			WpfCommand MyCommand;
			//****************************************
			
			if (_Commands.TryGetValue(templateName, out MyCommand))
				return MyCommand;
			
			throw new ArgumentException("Invalid Template Name");
		}
		
		internal GuiProvider FindProvider(Type viewType)
		{	//****************************************
			GuiProvider MyProvider;
			//****************************************
			
			if (_ProviderMappings.TryGetValue(viewType, out MyProvider))
				return MyProvider;
			
			return null;
		}

		internal void AttachTo(GuiPresenter presenter, FrameworkElement element)
		{
			_Attach.Invoke(this, EventArgs.Empty);
		}
				
		internal void ShowDialog(GuiFormPresenter parent, WpfFormController dialog)
		{
			dialog.Window.Owner = ((WpfFormController)GetView(parent)).Window;
			dialog.Window.ShowDialog();
		}

		//****************************************
		
		protected override void OnCultureChanged(CultureInfo newCulture)
		{
			
		}

		//****************************************
		
		public override string Name
		{
			get { return "WPF"; }
		}
		
		internal event EventHandler<EventArgs> Attach
		{
			add { _Attach.Add(value); }
			remove { _Attach.Remove(value); }
		}		
	}
}