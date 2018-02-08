/****************************************\
 XmlResourceSet.cs
 Created: 25-05-2010
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
//****************************************

namespace Proximity.Utility.Resources
{
	internal class XmlResourceSet : ResourceSet
	{
		internal XmlResourceSet(CultureInfo culture, Stream xmlResource) : base(new XmlResourceReader(culture, xmlResource))
		{
		}

		//****************************************

		public override Type GetDefaultReader()
		{
			return typeof(XmlResourceReader);
		}
	}
}
#endif