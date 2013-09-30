/****************************************\
 XmlResourceReader.cs
 Created: 25-05-2010
\****************************************/
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Xml;
//****************************************

namespace Proximity.Utility.Resources
{
	/// <summary>
	/// Represents a Resource Reader
	/// </summary>
	internal class XmlResourceReader : IResourceReader
	{	//****************************************
		private Hashtable _ResourceValues;
		//****************************************

		internal XmlResourceReader(CultureInfo culture, Stream resourceStream)
		{
			_ResourceValues = new Hashtable();

			using (XmlReader Reader = XmlReader.Create(resourceStream))
			{
				ReadResourceFile(culture, Reader);
			}
		}

		//****************************************

		private void ReadResourceFile(CultureInfo culture, XmlReader reader)
		{	//****************************************
			string Name, Value;
			//****************************************

			if (!reader.ReadToFollowing("Resource"))
				throw new InvalidDataException("Resource is not valid");

			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
				case "Culture":
					if (reader.Value != culture.Name)
						Log.Warning("Requested culture {0} does not match the Resource {1}", culture.Name, reader.Value);
					break;

				default:
					break;
				}
			}

			reader.MoveToElement();

			while (reader.Read())
			{
				if (reader.MoveToContent() == XmlNodeType.EndElement)
					break;

				switch (reader.LocalName)
				{
				case "Title":
					_ResourceValues.Add("Title", reader.ReadElementString());
					break;

				case "Group":
					Name = reader.GetAttribute("Name");
					ReadGroup(reader, Name);
					break;

				case "Item":
					Name = reader.GetAttribute("Name");
					Value = reader.ReadElementString();
					_ResourceValues.Add(Name, Value);
					break;

				default:
					reader.Skip();
					break;
				}
			}

		}

		private void ReadGroup(XmlReader reader, string rootPath)
		{	//****************************************
			string Name, Value;
			//****************************************

			while (reader.Read())
			{
				if (reader.MoveToContent() == XmlNodeType.EndElement)
					break;

				switch (reader.LocalName)
				{
				case "Group":
					Name = string.Format("{0}.{1}", rootPath, reader.GetAttribute("Name"));
					ReadGroup(reader, Name);
					break;

				case "Item":
					Name = reader.GetAttribute("Name");
					if (string.IsNullOrEmpty(Name))
						Name = rootPath;
					else
						Name = string.Format("{0}.{1}", rootPath, Name);
					Value = reader.ReadElementString();
					_ResourceValues.Add(Name, Value);
					break;

				default:
					reader.Skip();
					break;
				}
			}
		}

		//****************************************

		void IResourceReader.Close()
		{
			_ResourceValues = null;
		}

		IDictionaryEnumerator IResourceReader.GetEnumerator()
		{
			return _ResourceValues.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _ResourceValues.GetEnumerator();
		}

		void IDisposable.Dispose()
		{
			_ResourceValues = null;
		}
	}
}
