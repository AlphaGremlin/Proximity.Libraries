/****************************************\
 GuiService.cs
 Created: 11-08-2008
\****************************************/
using System;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using Proximity.Gui.Data;
using Proximity.Gui.Presentation;
using Proximity.Gui.Toolkit;
using Proximity.Utility;
using Proximity.Utility.Events;
//****************************************

namespace Proximity.Gui
{
	/// <summary>
	/// GUI Service Control
	/// </summary>
	/// <remarks>Programs should first call <see cref="Init()"/>, and then call <see cref="Register"/> for each component</remarks>
	public static class GuiService
	{	//****************************************
		private static GuiToolkit _Toolkit;
		private static List<GuiComponent> _Components = new List<GuiComponent>();
		
		private static CultureInfo _Culture, _DefaultCulture;
		
		private static WeakEvent<EventArgs> _CultureChanging = new WeakEvent<EventArgs>();
		private static WeakEvent<EventArgs> _CultureChanged = new WeakEvent<EventArgs>();
		//****************************************
		
		/// <summary>
		/// Initialise the GuiService, automatically selecting the GUI Toolkit
		/// </summary>
		public static void Init()
		{	//****************************************
			GuiToolkit NewToolkit;
			
			string RootPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Toolkits");
			string ToolkitPath = null;
			Assembly MyAssembly;
			object[] Attribs;
			//****************************************
			
			foreach(string Argument in Environment.GetCommandLineArgs())
			{
				// Was the toolkit specified on the command line?
				if (Argument.StartsWith("-Toolkit:"))
				{
					ToolkitPath = Path.Combine(RootPath, string.Format("Proximity.Gui.{0}.dll", Argument.Substring(9)));
					
					break;
				}
			}
			
			//****************************************
			
			if (ToolkitPath == null)
			{
				// No toolkit specified, pick an appropriate one automatically
				switch (Environment.OSVersion.Platform)
				{
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.WinCE:
					try
					{
						// Check if WPF is installed
						MyAssembly = Assembly.Load("PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
						
						// Success, use the WPF toolkit
						ToolkitPath = Path.Combine(RootPath, "Proximity.Gui.Wpf.dll");
					}
					catch(FileNotFoundException)
					{
						// Exception, use WinForms instead
						ToolkitPath = Path.Combine(RootPath, "Proximity.Gui.WinForms.dll");
					}
					break;
				
				case PlatformID.MacOSX:
				case PlatformID.Unix:
				case (PlatformID)128:
					ToolkitPath = Path.Combine(RootPath, "Proximity.Gui.GtkSharp.dll");
					break;
				
				default:
					throw new PlatformNotSupportedException("Platform is unsupported");
				}
			}
			
			//****************************************
			
			// Load the Toolkit Assembly
			MyAssembly = Assembly.LoadFrom(ToolkitPath);
			
			// Find the Attribute that tells us the Toolkit Class
			Attribs = MyAssembly.GetCustomAttributes(typeof(GuiToolkitAttribute), false);
			
			if (Attribs.Length == 0)
				throw new GuiException("Missing Toolkit Attribute");

			// Create the Toolkit
			NewToolkit = (GuiToolkit)Activator.CreateInstance(((GuiToolkitAttribute)Attribs[0]).ToolkitType);
			
			//****************************************
			
			Init(NewToolkit);
		}
		
		/// <summary>
		/// Initialise the GuiService with a specific Toolkit
		/// </summary>
		/// <param name="guiToolkit">The toolkit to use</param>
		/// <remarks>This allows programs to use only one Toolkit, or provide a custom GUI (like an in-game interface)</remarks>
		public static void Init(GuiToolkit guiToolkit)
		{
			Log.Info("Using Toolkit {0} from {1}", guiToolkit.Name, guiToolkit.GetType().Assembly.FullName);
			
			_DefaultCulture = CultureInfo.CurrentCulture;
			_Culture = _DefaultCulture;

			_Toolkit = guiToolkit;
			
			_Toolkit.Init();
		}
		
		/// <summary>
		/// Registers a GuiComponent with the service
		/// </summary>
		/// <param name="component">The new GuiComponent</param>
		public static void Register(GuiComponent component)
		{
			Log.Info("Using Component {0} from {1}", component.Name, component.GetType().Assembly.FullName);

			_Components.Add(component);
			
			component.Init();
		}
		
		/// <summary>
		/// Unregisters a GuiComponent from the service
		/// </summary>
		/// <param name="component">The GuiComponent to unregister</param>
		public static void Unregister(GuiComponent component)
		{
			component.Close();
			
			_Components.Remove(component);
		}
		
		//****************************************
		
		/// <summary>
		/// Retrieves a localised string from the currently registered GuiComponents
		/// </summary>
		/// <param name="name">The path of the string to retrieve</param>
		/// <returns>The requested string, or null if it does not exist</returns>
		/// <remarks>
		/// <para>Searches through the registered GuiComponents and returns the first instance</para>
		/// <para>Does not support string replacement</para></remarks>
		public static string GetString(string name)
		{	//****************************************
			string LocalString;
			//****************************************
			
			foreach(GuiComponent MyComponent in _Components)
			{
				LocalString = MyComponent.LocalisationManager.GetString(name);
				
				if (LocalString != null)
					return LocalString;
			}
			
			return null;
		}
		
		/// <summary>
		/// Retrieves a named Gui Converter
		/// </summary>
		/// <param name="name">The name of the converter to retrieve</param>
		/// <returns>The requested converter, or null if it does not exist</returns>
		public static IGuiConverter FindConverter(string name)
		{
			return null;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the currently active Toolkit
		/// </summary>
		public static GuiToolkit Toolkit
		{
			get { return _Toolkit; }
		}
		
		/// <summary>
		/// Gets/Sets the currently active Culture
		/// </summary>
		/// <remarks>
		/// <para>Users should always set this property when changing the culture, as the Toolkits will automatically switch the User Interface localisation</para>
		/// <para>This property will automatically maintain Thread.CurrentThread.CurrentCulture</para>
		/// </remarks>
		public static CultureInfo Culture
		{
			get { return _Culture; }
			set
			{
				if (value == null)
					value = _DefaultCulture;
				
				if (_Culture == value)
					return;
				
				_Culture = value;
				
				System.Threading.Thread.CurrentThread.CurrentCulture = _Culture;
				
				_CultureChanging.Invoke(null, EventArgs.Empty);
				
				_Toolkit.CultureChanged();
				
				_CultureChanged.Invoke(null, EventArgs.Empty);
			}
		}
		
		/// <summary>
		/// Gets the Default Culture
		/// </summary>
		/// <remarks>This is defined as the Culture when the GuiService was started</remarks>
		public static CultureInfo DefaultCulture
		{
			get { return _DefaultCulture; }
		}

		//****************************************
		
		/// <summary>
		/// Occurs when the culture is changing
		/// </summary>
		/// <remarks>This is a Weak Event, subscribers do not need to unsubscribe</remarks>
		public static event EventHandler<EventArgs> CultureChanging
		{
			add { _CultureChanging.Add(value); }
			remove { _CultureChanging.Remove(value); }
		}
		
		/// <summary>
		/// Occurs when the culture has changed
		/// </summary>
		public static event EventHandler<EventArgs> CultureChanged
		{
			add { _CultureChanged.Add(value); }
			remove { _CultureChanged.Remove(value); }
		}
	}
}