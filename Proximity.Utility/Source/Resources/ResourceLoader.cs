/****************************************\
 ResourceLoader.cs
 Created: 25-05-2010
\****************************************/
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
//****************************************

namespace Proximity.Utility.Resources
{
	/// <summary>
	/// Provides methods for loading resources, with override support from a local folder
	/// </summary>
	public static class ResourceLoader
	{	//****************************************
		private static string _ResourcePath = "Resources";
		//****************************************
		
		/// <summary>
		/// Retrieves a resource data stream for the calling assembly
		/// </summary>
		/// <param name="fullName">The full name of the resource to find (eg: 'Setup.xml')</param>
		/// <returns>The requested stream if found, otherwise null</returns>
		/// <remarks>The directory containing the assembly will be searched first, and if no file is found, the assembly resources will be searched</remarks>
		public static Stream Load(string fullName)
		{
			return Load(Assembly.GetCallingAssembly(), fullName);
		}
		
		/// <summary>
		/// Retrieve a resource data stream
		/// </summary>
		/// <param name="source">The assembly to look in</param>
		/// <param name="fullName">The full name of the resource to find (eg: 'Setup.xml')</param>
		/// <returns>The requested stream if found, otherwise null</returns>
		/// <remarks>The directory containing the assembly will be searched first, and if no file is found, the assembly resources will be searched</remarks>
		public static Stream Load(Assembly source, string fullName)
		{	//****************************************
			string FileName;
			//****************************************

			FileName = Path.GetDirectoryName(source.Location);
			FileName = Path.Combine(FileName, _ResourcePath);
			FileName = Path.Combine(FileName, fullName);

			if (File.Exists(FileName))
				return File.OpenRead(FileName);

			//****************************************

			foreach (string ResourceName in source.GetManifestResourceNames())
			{
				if (ResourceName.EndsWith(fullName, StringComparison.OrdinalIgnoreCase))
					return source.GetManifestResourceStream(ResourceName);
			}

			//****************************************

			return null;
		}
		
		/// <summary>
		/// Retrieves a resource data stream localised to the current thread culture
		/// </summary>
		/// <param name="source">The assembly to look in</param>
		/// <param name="name">The short name of the resource (eg: 'Setup')</param>
		/// <param name="type">The type (extension) of the resource (eg: 'xml')</param>
		/// <returns>The requested stream if found, otherwise null</returns>
		/// <remarks>
		/// <para>Searches using the <see cref="CultureInfo.CurrentCulture" /> culture (eg: 'Setup.en-AU.xml'), then its parent if given a specific culture (eg: 'Setup.en.xml').</para>
		/// <para>If not found, searches for the culture specifified using <see cref="NeutralResourcesLanguageAttribute" /> (eg: 'Setup.en-US.xml'), and finally the invariant culture (eg: 'Setup.xml').</para>
		/// <para>At each step, searches the <see cref="ResourcePath" /> folder, then the <paramref name="source" /> assembly.</para>
		/// </remarks>
		public static Stream Load(Assembly source, string name, string type)
		{
			return Load(source, name, CultureInfo.CurrentCulture, type);
		}
		
		/// <summary>
		/// Retrieves a resource data stream localised to the provided culture
		/// </summary>
		/// <param name="source">The assembly to look in</param>
		/// <param name="name">The short name of the resource (eg: 'Setup')</param>
		/// <param name="culture">The culture to search for</param>
		/// <param name="type">The type (extension) of the resource (eg: 'xml')</param>
		/// <returns>The requested stream if found, otherwise null</returns>
		/// <remarks>
		/// <para>Searches using <paramref name="culture" /> (eg: 'Setup.en-AU.xml'), then its parent if given a specific culture (eg: 'Setup.en.xml').</para>
		/// <para>If not found, searches for the culture specifified using <see cref="NeutralResourcesLanguageAttribute" /> (eg: 'Setup.en-US.xml'), and finally the invariant culture (eg: 'Setup.xml').</para>
		/// <para>At each step, searches the <see cref="ResourcePath" /> folder, then the <paramref name="source" /> assembly.</para>
		/// </remarks>
		public static Stream Load(Assembly source, string name, CultureInfo culture, string type)
		{	//****************************************
			Stream MyStream;
			object[] Attributes;
			//****************************************
			
			if (culture != null && culture != CultureInfo.InvariantCulture)
			{
				// Not the Invariant culture, search on the given culture
				MyStream = Load(source, string.Format("{0}.{1}.{2}", name, culture.Name, type));
				
				if (MyStream != null)
					return MyStream;
				
				// If we weren't given a neutral culture, search on its parent
				if (!culture.IsNeutralCulture)
				{
					MyStream = Load(source, string.Format("{0}.{1}.{2}", name, culture.Parent.Name, type));
				
					if (MyStream != null)
						return MyStream;
				}
				
				// No match so far, attempt to search on the netural culture
				Attributes = source.GetCustomAttributes(typeof(NeutralResourcesLanguageAttribute), false);
				
				if (Attributes.Length != 0)
				{
					MyStream = Load(source, string.Format("{0}.{1}.{2}", name, ((NeutralResourcesLanguageAttribute)Attributes[0]).CultureName, type));
					
					if (MyStream != null)
						return MyStream;
				}
			}
			
			// Attempt to search on the Invariant Culture
			return Load(source, string.Format("{0}.{1}", name, type));
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets the path used to locate file-based resources
		/// </summary>
		/// <remarks>Defaults to 'Resources'</remarks>
		public static string ResourcePath
		{
			get { return _ResourcePath; }
			set { _ResourcePath = value; }
		}
	}
}
