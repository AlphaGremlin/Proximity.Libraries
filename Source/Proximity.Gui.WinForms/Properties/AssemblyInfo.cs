/****************************************\
 AssemblyInfo.cs
\****************************************/
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Proximity.Gui.Toolkit;
using Proximity.Gui.WinForms;
//****************************************

[assembly: AssemblyTitle("WinForms Toolkit")]
[assembly: AssemblyDescription("GUI Toolkit for WinForms")]
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

[assembly: GuiToolkit(typeof(WinToolkit))]