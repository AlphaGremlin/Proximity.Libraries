/****************************************\
 AssemblyInfo.cs
\****************************************/
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Proximity.Gui.Toolkit;
using Proximity.Gui.GtkSharp;
//****************************************

[assembly: AssemblyTitle("GtkSharp Toolkit")]
[assembly: AssemblyDescription("GUI Toolkit for GtkSharp")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Proximity Interactive")]
[assembly: AssemblyProduct("GUI Library")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

//****************************************

[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]

//****************************************

[assembly: AssemblyVersion("1.0.*")]

[assembly: GuiToolkit(typeof(GtkToolkit))]