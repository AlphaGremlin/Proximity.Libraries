/****************************************\
 Platform.cs
 Created: 14-03-2009
\****************************************/
#if !MOBILE && !PORTABLE
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Policy;
using System.Threading;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Manages Platform-Specific tasks
	/// </summary>
	[SecurityCritical]
	public static class Platform
	{	//****************************************
		[DllImport("kernel32.dll", EntryPoint = "LoadLibrary", CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity()]
		private static extern IntPtr LoadLibrary(string fileName);
		
		private static string _ArchX86 = "x86";
		private static string _ArchX64 = "x64";
		//****************************************
		
		/// <summary>
		/// Re-initialises the entrypoint in a new AppDomain on a new Primary Thread, with the architecture-specific binary path
		/// </summary>
		/// <param name="name">Name to assign to the new AppDomain</param>
		/// <returns>True if a new  architecture-specific AppDomain was spawned, otherwise False</returns>
		/// <remarks>
		/// <para>As this method re-enters the starting assembly in order to change the binary search path, callers should do so straight away in their Main method.</para>
		/// </remarks>
		/// <example>
		///	static void Main()
		///	{
		///		// Changes to the Arch properties go here
		///		
		///		if (Platform.SpawnDomain("Test Application"))
		///			return;
		///		
		///		// Run program as normal
		///	}
		/// </example>
		public static bool SpawnDomain(string name)
		{	//****************************************
			AppDomain MyDomain = CreateDomain(name);
			Assembly MyAssembly;
			//****************************************
			
			// Are we within the platform-specific domain, or on an unsupported platform?
			if (MyDomain == null)
				return false;
			
			// Store setup data against the new AppDomain
			MyAssembly = Assembly.GetEntryAssembly();
			
			//MyDomain.SetData("Platform.Evidence", MyAssembly.Evidence);
			MyDomain.SetData("Platform.Arguments", Environment.GetCommandLineArgs());
			MyDomain.SetData("Platform.ThreadName", name);
			MyDomain.SetData("Platform.Name", MyAssembly.GetName());
			
			// Switch to the other AppDomain
			MyDomain.DoCallBack(StartPlatform);
			
			return true;
		}
		
		/// <summary>
		/// Re-enters the entrypoint in a new AppDomain, with the architecture-specific binary path
		/// </summary>
		/// <param name="name">Name to assign to the new AppDomain</param>
		/// <returns>The return code of the application, or null if this is called from the architecture-specific AppDomain</returns>
		/// <remarks>
		/// <para>As this method re-enters the starting assembly in order to change the binary search path, callers should do so straight away in their Main method.</para>
		/// <para>When a value is returned, the caller should return this from their Main method. When a value is not returned, the caller should continue execution.</para>
		/// </remarks>
		/// <example>
		///	static int Main()
		///	{
		///		Nullable&lt;int&gt; Result;
		///		
		///		// Changes to the Arch properties go here
		///		
		///		if ((Result = Platform.InitDomain("Test Application")).HasValue)
		///			return Result.Value;
		///		
		///		// Run program as normal
		///	}
		///	
		///	static void Main()
		///	{
		///		// Changes to the Arch properties go here
		///		
		///		if (Platform.InitDomain("Test Application").HasValue)
		///			return;
		///		
		///		// Run program as normal
		///	}
		/// </example>
		public static Nullable<int> ExecDomain(string name)
		{	//****************************************
			AppDomain MyDomain = CreateDomain(name);
			Assembly MyAssembly;
			//****************************************
			
			// Are we within the platform-specific domain, or on an unsupported platform?
			if (MyDomain == null)
				return null;
			
			MyAssembly = Assembly.GetEntryAssembly();
			
			// Switch to the other AppDomain
			return MyDomain.ExecuteAssemblyByName(MyAssembly.GetName(), Environment.GetCommandLineArgs());
		}
		
		/// <summary>
		/// Installs an AssemblyResolve event that will redirect assembly loads to the appropriate architecture-specific folder
		/// </summary>
		/// <remarks>
		/// <para>This is more memory efficient than spawning a secondary AppDomain. However, it does alter the usual .Net Assembly Resolution process.</para>
		/// <para>Failures to load an Assembly that has been found in the arch folder will log a critical error.</para>
		/// </remarks>
		public static void InstallRedirect()
		{
			AppDomain.CurrentDomain.AssemblyResolve += OnArchitectureResolve;
		}
		
		/// <summary>
		/// Preload a native assembly loaded via DllImport from an architecture-specific folder
		/// </summary>
		/// <param name="nativeAssembly">The file name of the assembly to load</param>
		/// <remarks>Assembly should be given including extension (xyz.dll)</remarks>
		public static void NativePreload(string nativeAssembly)
		{	//****************************************
			IntPtr hLibrary;
			//****************************************
			
			// Get the path the assembly should be in
			nativeAssembly = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, GetPlatformPath(nativeAssembly));
			
			if (!File.Exists(nativeAssembly))
				return;
			
			// Operating-System specific code to load the assembly
			switch (Environment.OSVersion.Platform)
			{
			case PlatformID.Win32NT:
			case PlatformID.Win32S:
			case PlatformID.Win32Windows:
			case PlatformID.WinCE:
				hLibrary = LoadLibrary(nativeAssembly);
				return;

			case PlatformID.Xbox:
			case PlatformID.MacOSX:
			case PlatformID.Unix:
			case (PlatformID)128:
				return;
			
			default:
				throw new PlatformNotSupportedException("Platform is unsupported");
			}
		}

		//****************************************
		
		private static AppDomain CreateDomain(string name)
		{	//****************************************
			AppDomainSetup MySetup;
			AppDomain MyDomain, ActiveDomain = AppDomain.CurrentDomain;
			Assembly MyAssembly = Assembly.GetEntryAssembly();
			//****************************************

			// Have we already switched to the platform-specific AppDomain?
			if (ActiveDomain.GetData("Platform.Switched") != null)
				return null;
			
			// Do we support this Operating System?
			switch (Environment.OSVersion.Platform)
			{
			case PlatformID.Win32NT:
			case PlatformID.Win32S:
			case PlatformID.Win32Windows:
			case PlatformID.WinCE:
				break;

			case PlatformID.Xbox:
			case PlatformID.MacOSX:
			case PlatformID.Unix:
			case (PlatformID)128:
				return null;
			
			default:
				throw new PlatformNotSupportedException("Platform is unsupported");
			}
			
			//****************************************
			
			// Duplicate the Activation Context (if any)
			if (AppDomain.CurrentDomain.ActivationContext != null)
				MySetup = new AppDomainSetup(AppDomain.CurrentDomain.ActivationContext);
			else
				MySetup = new AppDomainSetup();
			
			MySetup.ApplicationName = ActiveDomain.SetupInformation.ApplicationName;
			MySetup.ApplicationBase = ActiveDomain.SetupInformation.ApplicationBase;
			MySetup.ConfigurationFile = ActiveDomain.SetupInformation.ConfigurationFile;
			
			// Add the architecture-specific path to get binaries from
			// All this, just to be able to change this property dynamically!
			if (ActiveDomain.SetupInformation.PrivateBinPath == "")
				MySetup.PrivateBinPath = GetPlatformPath("");
			else
				MySetup.PrivateBinPath = string.Concat(ActiveDomain.SetupInformation.PrivateBinPath, ";", GetPlatformPath(""));
			
			// Create the domain
			MyDomain = AppDomain.CreateDomain(name, AppDomain.CurrentDomain.Evidence, MySetup);
			MyDomain.SetData("Platform.Switched", true);
			
			return MyDomain;
		}

		private static string GetPlatformPath(string library)
		{
			if (IntPtr.Size == 4)
				return Path.Combine(_ArchX86, library);
			else if (IntPtr.Size == 8)
				return Path.Combine(_ArchX64, library);
			else
				return string.Empty;
		}
		
		private static void StartPlatform()
		{	//****************************************
			Thread MyThread = new Thread(StartPlatformThread);
			//****************************************

			// Switch to a new thread so exceptions stay within the AppDomain, and we don't lose the stack on transition
			
			MyThread.Name = (string)AppDomain.CurrentDomain.GetData("Platform.ThreadName");
			MyThread.Priority = Thread.CurrentThread.Priority;
			MyThread.IsBackground = false;
			MyThread.SetApartmentState(Thread.CurrentThread.GetApartmentState());

			MyThread.Start();
		}

		[DebuggerHidden]
		private static void StartPlatformThread()
		{	//****************************************
			AssemblyName Name = (AssemblyName)AppDomain.CurrentDomain.GetData("Platform.Name");
			//Evidence Evidence = (Evidence)AppDomain.CurrentDomain.GetData("Platform.Evidence");
			string[] Arguments = (string[])AppDomain.CurrentDomain.GetData("Platform.Arguments");
			//****************************************

			AppDomain.CurrentDomain.ExecuteAssemblyByName(Name, Arguments);
		}
		
		private static Assembly OnArchitectureResolve(object sender, ResolveEventArgs e)
		{	//****************************************
			string[] SegName = e.Name.Split(',');
			string FileName = GetPlatformPath(SegName[0] + ".dll");
			//****************************************
			
			FileName = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, FileName);
			
			if (!File.Exists(FileName))
				return null;
			
			try
			{
				return Assembly.LoadFrom(FileName);
			}
			catch (Exception ex)
			{
				Log.Critical("Failed to load Platform Specific Assembly {0}: {1}", FileName, ex.Message);
				
				return null;
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets the relative path for 32-bit (x86) libraries
		/// </summary>
		public static string ArchX86
		{
			get { return _ArchX86; }
			set { _ArchX86 = value; }
		}
		
		/// <summary>
		/// Gets/Sets the relative path for 64-bit (x64) libraries
		/// </summary>
		public static string ArchX64
		{
			get { return _ArchX64; }
			set { _ArchX64 = value; }
		}
	}
}
#endif