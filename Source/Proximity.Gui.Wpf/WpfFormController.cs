/****************************************\
 WpfFormController.cs
 Created: 24-05-2010
\****************************************/
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Proximity.Gui.Presentation;
using Proximity.Gui.View;
using Proximity.Gui.Wpf.Data;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.Wpf
{
	/// <summary>
	/// GuiFormController implementation for WPF
	/// </summary>
	public class WpfFormController : GuiFormController
	{	//****************************************
		private Window _Window;
		//****************************************

		internal WpfFormController(GuiFormPresenter presenter, Window window) : base(presenter)
		{	//****************************************
			WpfToolkit MyToolkit = ((WpfToolkit)GuiService.Toolkit);
			//****************************************
			
			_Window = window;
			_Window.Loaded += OnLoaded;
			_Window.Closed += OnClosed;
			_Window.DataContext = Presenter;
			_Window.Resources.Add("Proximity.Gui.Presentation.GuiPresenter", Presenter);

			// Register each Command that can be handled by this Presenter against the Windows Command Bindings
			foreach (GuiCommandTemplate MyTemplate in Presenter.Host.GetCommandTemplates(Presenter))
			{
				_Window.CommandBindings.Add(new CommandBinding(MyToolkit.FindCommand(MyTemplate.Name), OnExecutedRoutedEvent, OnCanExecuteRoutedEvent));
			}
		}

		//****************************************
		
		protected override void Close()
		{
			_Window.Close();
		}
		
		protected override void Show(GuiFormPresenter parent)
		{
			((WpfToolkit)GuiService.Toolkit).ShowDialog(parent, this);
		}

		//****************************************

		private void OnCanExecuteRoutedEvent(object sender, CanExecuteRoutedEventArgs e)
		{	//****************************************
			GuiCommandTemplate MyTemplate = Presenter.Host.GetCommandTemplate(Presenter, ((WpfCommand)e.Command).TemplateName);
			//****************************************

			// We received a Command, check if our Presenter can handle it
			e.CanExecute = MyTemplate.CanCheckExecute ? MyTemplate.CheckExecute(Presenter) : false;
		}
				
		private void OnLoaded(object sender, EventArgs e)
		{
			((WpfToolkit)GuiService.Toolkit).AttachTo(Presenter, _Window);
			
			_Window.Loaded -= OnLoaded;
		}
		
		private void OnClosed(object sender, EventArgs e)
		{
			
		}
		
		private void OnExecutedRoutedEvent(object sender, ExecutedRoutedEventArgs e)
		{
			// We received a Command, pass it to our Presenter
			Presenter.Host.GetCommandTemplate(Presenter, ((WpfCommand)e.Command).TemplateName).Execute(Presenter);
		}
		
		//****************************************
		
		public override string Name
		{
			get { return _Window.Name;}
		}
		
		protected override bool Visibility
		{
			get { return _Window.Visibility == System.Windows.Visibility.Visible; }
			set { _Window.Visibility = value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden; }
		}
		
		internal Window Window
		{
			get { return _Window; }
		}
	}
}
