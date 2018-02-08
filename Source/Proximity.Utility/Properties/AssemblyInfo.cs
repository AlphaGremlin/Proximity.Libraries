/****************************************\
 Proximity Utility Library
 Copyright (c) Daniel Chandler
 Released under the GNU LGPL 2.1 or later
 ****************************************
 AssemblyInfo.cs
\****************************************/
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
//****************************************

[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

//****************************************

[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]

#if !NETSTANDARD1_3
[assembly: SecurityRules(SecurityRuleSet.Level2)]
#endif
[assembly: AllowPartiallyTrustedCallers()]