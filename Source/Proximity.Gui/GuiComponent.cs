/****************************************\
 GuiComponent.cs
 Created: 12-09-2008
\****************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Xml;
using Proximity.Gui.Templating;
using Proximity.Gui.Presentation;
using Proximity.Utility;
using Proximity.Utility.Resources;
//****************************************

namespace Proximity.Gui
{
	/// <summary>
	/// Represents a component in your application.
	/// </summary>
	/// <remarks>Most applications will have one main component and several sub-components (ie: plugins).
	/// The GuiComponent lets each plugin manage its own language and resources independent of each other.</remarks>
	public abstract class GuiComponent : MarshalByRefObject
	{	//****************************************
		private string _Name;
		private string _DefaultNamespace;
		private GuiProvider _Provider;
		private ComponentDef _ComponentDef;

		private Assembly _PrimaryAssembly;
		private ResourceManager _LocalisationManager;
		private ResourceManager _ResourceManager;
		
		private IList<CultureInfo> _Languages;
		private IDictionary<Type, IDictionary<string, GuiCommandTemplate>> _CommandMappings;
		//****************************************
		
		/// <summary>
		/// Creates a new GuiComponent.
		/// </summary>
		/// <param name="name">The name to associate with this component (for logging/debugging)</param>
		protected GuiComponent(string name)
		{
			_Name = name;
			_PrimaryAssembly = this.GetType().Assembly;
			_DefaultNamespace = _PrimaryAssembly.EntryPoint.DeclaringType.Namespace;
		}
		
		//****************************************
		
		internal void Init()
		{
			// Load Resources
			_ResourceManager = OpenResourceManager();
			_LocalisationManager = OpenLocalisationManager();
			_ComponentDef = OpenComponent();
			_Languages = OpenLanguages();
			
			_CommandMappings = LoadCommands();
			
			// Load the View Provider
			_Provider = OpenProvider();
			_Provider.Init();
		}

		internal void Close()
		{

		}

		//****************************************

		public abstract void Start();

		/// <summary>
		/// Retrieves all the Command Templates defined by this Presenter
		/// </summary>
		/// <param name="presenter">The Presenter to check</param>
		/// <returns>The matching Command Templates</returns>
		public ICollection<GuiCommandTemplate> GetCommandTemplates(GuiPresenter presenter)
		{	//****************************************
			IDictionary<string, GuiCommandTemplate> CommandTemplates;
			//****************************************

			if (_CommandMappings.TryGetValue(presenter.GetType(), out CommandTemplates))
				return CommandTemplates.Values;

			throw new ArgumentException("Presenter does not exist");
		}

		/// <summary>
		/// Retrieves the named Command Template if defined by this Presenter
		/// </summary>
		/// <param name="presenter">The Presenter to check</param>
		/// <param name="commandName">The name of the Command Template</param>
		/// <returns>The requested Command Template, or null if not defined</returns>
		public GuiCommandTemplate GetCommandTemplate(GuiPresenter presenter, string commandName)
		{	//****************************************
			IDictionary<string, GuiCommandTemplate> CommandTemplates;
			GuiCommandTemplate MyTemplate;
			//****************************************

			if (!_CommandMappings.TryGetValue(presenter.GetType(), out CommandTemplates))
				throw new ArgumentException("Presenter does not exist");

			if (CommandTemplates.TryGetValue(commandName, out MyTemplate))
				return MyTemplate;

			return null;
		}

		/// <summary>
		/// Retrieves a list of all the Command Template names defined by this Component
		/// </summary>
		/// <returns>A list of unique Command Template names</returns>
		public IList<string> ListCommands()
		{	//****************************************
			SortedList<string, string> MyCommandList = new SortedList<string, string>();
			//****************************************
			
			foreach(IDictionary<string, GuiCommandTemplate> MyCommands in _CommandMappings.Values)
			{
				foreach(string MyTemplate in MyCommands.Keys)
				{
					if (MyCommandList.ContainsKey(MyTemplate))
						continue;
					
					MyCommandList.Add(MyTemplate, null);
				}
			}
			
			//****************************************
			
			return MyCommandList.Keys;
		}
		
		//****************************************
				
		/// <summary>
		/// Retrieves the standard Resource Manager assigned to this Component
		/// </summary>
		/// <returns>Resource Manager</returns>
		protected virtual ResourceManager OpenResourceManager()
		{
			return new ResourceManager(string.Format("{0}.Resources.{1}", _DefaultNamespace, _Name), _PrimaryAssembly);
		}
		
		/// <summary>
		/// Retrieves the localisation Resource Manager assigned to this Component
		/// </summary>
		/// <returns>Resource Manager</returns>
		protected virtual ResourceManager OpenLocalisationManager()
		{
			return new XmlResourceManager(_Name, _PrimaryAssembly);
		}
		
		/// <summary>
		/// Retrieves the Component Definition for this Component
		/// </summary>
		/// <returns>A Component Definition describing the templating data for this Component</returns>
		protected virtual ComponentDef OpenComponent()
		{	//****************************************
			XmlReader Reader;
			//****************************************
			
			Reader = XmlReader.Create(ResourceLoader.Load(_PrimaryAssembly, _Name + ".xml"));
			
			//****************************************
			
			return new ComponentDef(this, Reader);
		}

		/// <summary>
		/// Retrieves a list of the Cultures supported by this Component
		/// </summary>
		/// <returns>A list of supported CultureInfo objects</returns>
		protected virtual IList<CultureInfo> OpenLanguages()
		{	//****************************************
			string RootName = string.Format("{0}.Resources.{1}-", _DefaultNamespace, _Name);
			string CultureName;
			SortedList<string, CultureInfo> MyLanguages = new SortedList<string, CultureInfo>();
			//****************************************
			
			foreach(string ResourceName in this.GetType().Assembly.GetManifestResourceNames())
			{
				if (!(ResourceName.StartsWith(RootName, StringComparison.OrdinalIgnoreCase) && ResourceName.EndsWith(".xml")))
					continue;
				
				// Lang resource, find the culture name
				
				CultureName = ResourceName.Substring(RootName.Length, ResourceName.Length - RootName.Length - 4);
				
				MyLanguages[CultureName] = new CultureInfo(CultureName);
			}
			
			foreach(string FileName in Directory.GetFiles(Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), "Resources"), _Name + "-*.xml"))
			{
				CultureName = Path.GetFileNameWithoutExtension(FileName).Substring(_Name.Length + 1);
				
				MyLanguages[CultureName] = new CultureInfo(CultureName);
			}
			
			//****************************************
			
			return new List<CultureInfo>(MyLanguages.Values);
		}
		
		/// <summary>
		/// Retrieves the appropriate View Provider for this Component
		/// </summary>
		/// <returns>The View Provider for this Component that supports the currently loaded Toolkit</returns>
		/// <exception cref="PlatformNotSupportedException">Component does not support the loaded Toolkit</exception>
		protected virtual GuiProvider OpenProvider()
		{	//****************************************
			Assembly ProviderAssembly;
			string ProviderPath;
			object[] MyAttributes;
			GuiProviderAttribute MyAttribute;
			//****************************************
			
			ProviderPath = Path.GetDirectoryName(_PrimaryAssembly.Location);
			ProviderPath = Path.Combine(ProviderPath, "Providers");
			ProviderPath = Path.Combine(ProviderPath, string.Format("{0}.{1}.dll", _DefaultNamespace, GuiService.Toolkit.Name));
			
			try
			{
				ProviderAssembly = Assembly.LoadFrom(ProviderPath);
			}
			catch (FileNotFoundException)
			{
				throw new PlatformNotSupportedException(string.Format("The Component {0} does not support the {1} Toolkit", _Name, GuiService.Toolkit.Name));
			}
			
			//****************************************
			
			MyAttributes = ProviderAssembly.GetCustomAttributes(typeof(GuiProviderAttribute), false);
			
			if (MyAttributes.Length != 1)
				throw new FormatException(string.Format("Assembly {0} does not have a GuiProvider Attribute", ProviderAssembly.FullName));
			
			MyAttribute = (GuiProviderAttribute)MyAttributes[0];
			
			//****************************************
			
			return (GuiProvider)Activator.CreateInstance(MyAttribute.ProviderType, new object[] {this});
		}
		
		//****************************************

		private IDictionary<Type, IDictionary<string, GuiCommandTemplate>> LoadCommands()
		{	//****************************************
			Dictionary<Type, IDictionary<string, GuiCommandTemplate>> MyPresenters;
			Dictionary<string, GuiCommandTemplate> MyCommandTemplates;
			object[] MyAttributes;
			GuiCommandTemplate MyTemplate;
			//****************************************

			MyPresenters = new Dictionary<Type, IDictionary<string, GuiCommandTemplate>>();

			foreach (Type MyType in _PrimaryAssembly.GetTypes())
			{
				if (MyType.IsAbstract || !MyType.IsClass)
					continue;

				if (!typeof(GuiPresenter).IsAssignableFrom(MyType))
					continue;

				//****************************************

				MyCommandTemplates = new Dictionary<string, GuiCommandTemplate>();

				foreach (MethodInfo MyMethod in MyType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
				{
					if (MyMethod.ReturnType != typeof(void) || MyMethod.GetParameters().Length != 0)
						continue;

					MyAttributes = MyMethod.GetCustomAttributes(typeof(GuiCommandAttribute), true);

					if (MyAttributes.Length == 0)
						continue;

					MyTemplate = new GuiCommandTemplate(MyType, MyMethod, (GuiCommandAttribute)MyAttributes[0]);

					MyCommandTemplates.Add(MyTemplate.Name, MyTemplate);
				}

				//****************************************

				MyPresenters.Add(MyType, MyCommandTemplates);
			}


			//****************************************

			return MyPresenters;
		}

		//****************************************
		
		/// <summary>
		/// Gets the global name of this Component (for logging/debugging)
		/// </summary>
		public string Name
		{
			get { return _Name; }
		}
		
		/// <summary>
		/// Gets the Toolkit Provider for this Component
		/// </summary>
		public GuiProvider Provider
		{
			get { return _Provider; }
		}
		
		/// <summary>
		/// Gets the primary Assembly for this Component
		/// </summary>
		public Assembly PrimaryAssembly
		{
			get { return _PrimaryAssembly; }
		}
				
		/// <summary>
		/// Gets the standard Resource Manager
		/// </summary>
		public ResourceManager ResourceManager
		{
			get { return _ResourceManager; }
		}
		
		/// <summary>
		/// Gets the localisation Resource Manager
		/// </summary>
		public ResourceManager LocalisationManager
		{
			get { return _LocalisationManager; }
		}
		
		/// <summary>
		/// Gets/Sets the Default Namespace
		/// </summary>
		/// <remarks>This is the prefix used by OpenLanguages and OpenProvider to detect assemblies and resources</remarks>
		protected string DefaultNamespace
		{
			get { return _DefaultNamespace; }
			set { _DefaultNamespace = value; }
		}
		
		/// <summary>
		/// Gets the definition data for this component
		/// </summary>
		public ComponentDef ComponentDef
		{
			get { return _ComponentDef; }
		}
	}
}
