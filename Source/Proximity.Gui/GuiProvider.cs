/****************************************\
 GuiProvider.cs
 Created: 25-04-10
\****************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using Proximity.Gui.Presentation;
using Proximity.Gui.View;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui
{
	/// <summary>
	/// Class to provide Toolkit-specific Views
	/// </summary>
	public abstract class GuiProvider : MarshalByRefObject
	{	//****************************************
		private Assembly _PrimaryAssembly;
		
		private GuiComponent _Component;
		
		private IDictionary<Type, Type> _PresenterMappings;
		//****************************************
		
		protected GuiProvider(GuiComponent component)
		{
			_Component = component;
			
			_PrimaryAssembly = this.GetType().Assembly;
		}
		
		//****************************************
		
		internal void Init()
		{
			Log.Info("Using Provider {0} from {1}", this.GetType().Name, this.GetType().Assembly.FullName);
			
			_PresenterMappings = LoadPresenterMappings();
			
			GuiService.Toolkit.Register(this);
		}
		
		//****************************************
		
		/// <summary>
		/// Loads the list of Presenter to View mappings
		/// </summary>
		/// <returns>A dictionary mapping each supported Presenter Type to its corresponding View Type</returns>
		/// <remarks>Used to automatically create the associated View when a Presenter is instantiated</remarks>
		protected virtual IDictionary<Type, Type> LoadPresenterMappings()
		{	//****************************************
			Dictionary<Type, Type> MyMappings = new Dictionary<Type, Type>();
			object[] MyAttributes;
			GuiPresenterAttribute MyAttribute;
			//****************************************

			foreach (Type MyType in _PrimaryAssembly.GetTypes())
			{
				if (MyType.IsAbstract || !MyType.IsClass)
					continue;
				
				MyAttributes = MyType.GetCustomAttributes(typeof(GuiPresenterAttribute), false);
				
				if (MyAttributes.Length != 1)
					continue;
				
				MyAttribute = (GuiPresenterAttribute)MyAttributes[0];
				
				if (MyMappings.ContainsKey(MyAttribute.PresenterType))
					// Presenter already has an associated View
					Log.Warning("Component {0} contains multiple Views for {1}", _Component.Name, MyAttribute.PresenterType.FullName);
				else
					// Add the Presenter and View to the list
					MyMappings.Add(MyAttribute.PresenterType, MyType);
			}
			
			//****************************************
			
			return MyMappings;
		}
		
		//****************************************
		
		internal GuiFormController GetFormController(GuiFormPresenter presenter)
		{	//****************************************
			Type ViewType;
			object MyView;
			//****************************************
			
			if (!_PresenterMappings.TryGetValue(presenter.GetType(), out ViewType))
				throw new ArgumentException(string.Format("Component {0} does not have any Views for Presenter {1}", _Component.Name, presenter.GetType().FullName));

			//****************************************
			
			Log.Verbose("Attaching Controller {0} to Presenter {1}", ViewType.Name, presenter.GetType().Name);
			
			MyView = Activator.CreateInstance(ViewType);
			
			return GuiService.Toolkit.CreateFormController(presenter, MyView);
		}
		
		internal GuiChildController GetChildController(GuiChildPresenter presenter, GuiViewController parent)
		{	//****************************************
			Type ViewType;
			//****************************************
			
			if (!_PresenterMappings.TryGetValue(presenter.GetType(), out ViewType))
				throw new ArgumentException(string.Format("Component {0} does not have any Views for Presenter {1}", _Component.Name, presenter.GetType().FullName));

			//****************************************
			
			Log.Verbose("Attaching Child Controller {0} to Presenter {1}", ViewType.Name, presenter.GetType().Name);

			return GuiService.Toolkit.FindChildController(presenter, parent, ViewType);
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the Component that defines this Provider
		/// </summary>
		public GuiComponent Component
		{
			get { return _Component; }
		}
		
		/// <summary>
		/// Gets the Primary Assembly of the Provider
		/// </summary>
		public Assembly PrimaryAssembly
		{
			get { return _PrimaryAssembly; }
		}
		
		/// <summary>
		/// Gets the list of Presenter to View Mappings
		/// </summary>
		public IDictionary<Type, Type> PresenterMappings
		{
			get { return _PresenterMappings; }
		}
	}
}
