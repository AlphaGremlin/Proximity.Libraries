/****************************************\
 XmlResourceManager.cs
 Created: 25-05-2010
\****************************************/
#if !PORTABLE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
//****************************************

namespace Proximity.Utility.Resources
{
	/// <summary>
	/// Provides a Resource Manager reading strings from XML files
	/// </summary>
	public sealed class XmlResourceManager : ResourceManager
	{	//****************************************
		private CultureInfo _NeutralCulture;
		
		private Dictionary<string, XmlResourceSet> _Resources;
		//****************************************
		
		/// <summary>
		/// Creates a new Xml Resource Manager
		/// </summary>
		/// <param name="baseName">The base name of the XML files we wish to access</param>
		/// <param name="assembly">The core neutral assembly</param>
		public XmlResourceManager(string baseName, Assembly assembly) : base(baseName, assembly)
		{	//****************************************
			object[] Attributes;
			//****************************************
			
			_Resources = new Dictionary<string, XmlResourceSet>();
			
			Attributes = assembly.GetCustomAttributes(typeof(NeutralResourcesLanguageAttribute), false);
			
			if (Attributes.Length != 0)
			{
				_NeutralCulture = new CultureInfo(((NeutralResourcesLanguageAttribute)Attributes[0]).CultureName);
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Retrieves a list of strings underneath a root path
		/// </summary>
		/// <param name="rootPath">The root path</param>
		/// <returns>The list of strings within that path</returns>
		public IList<string> GetStrings(string rootPath)
		{
			return GetStrings(rootPath, false);
		}
		
		/// <summary>
		/// Retrieves a list of strings underneath a root path
		/// </summary>
		/// <param name="rootPath">The root path</param>
		/// <param name="includeExact">True to include exact matches</param>
		/// <returns>The list of strings within that path</returns>
		public IList<string> GetStrings(string rootPath, bool includeExact)
		{
			return null;
		}
		
		//****************************************

		/// <summary>
		/// 
		/// </summary>
		/// <param name="culture"></param>
		/// <param name="createIfNotExists"></param>
		/// <param name="tryParents"></param>
		/// <returns></returns>
		protected sealed override ResourceSet InternalGetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
		{	//****************************************
			Stream ResourceStream = null;
			XmlResourceSet MyResourceSet;
			//****************************************
			
			if (_Resources.TryGetValue(culture.Name, out MyResourceSet))
				return MyResourceSet;

			//****************************************

			ResourceStream = ResourceLoader.Load(MainAssembly, string.Format("{0}.{1}.xml", BaseNameField, culture.Name));

			if (ResourceStream == null)
			{
				if (!tryParents)
					return null;
				
				if (culture != CultureInfo.InvariantCulture)
					return this.InternalGetResourceSet(culture.Parent, createIfNotExists, tryParents) as XmlResourceSet;
				
				if (_NeutralCulture == null)
					throw new MissingManifestResourceException("No Neutral Resources Language was defined");
				
				return this.InternalGetResourceSet(_NeutralCulture, createIfNotExists, false) as XmlResourceSet;
			}
			
			//****************************************
			
			MyResourceSet = new XmlResourceSet(culture, ResourceStream);
			
			if (createIfNotExists)
				_Resources.Add(culture.Name, MyResourceSet);
			
			return MyResourceSet;
		}
	}
}
#endif