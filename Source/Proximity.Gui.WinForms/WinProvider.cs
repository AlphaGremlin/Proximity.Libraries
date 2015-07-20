/****************************************\
 WinProvider.cs
 Created: 20-05-10
\****************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using Proximity.Gui.WinForms.View;
using Proximity.Utility;
using Proximity.Utility.Resources;
//****************************************

namespace Proximity.Gui.WinForms
{
	internal class WinProvider
	{	//****************************************
		private WinToolkit _Toolkit;
		private GuiProvider _Provider;

		private Dictionary<string, ViewDef> _Views;
		//****************************************

		internal WinProvider(WinToolkit toolkit, GuiProvider provider)
		{
			_Toolkit = toolkit;
			_Provider = provider;
		}

		//****************************************

		internal void Load()
		{	//****************************************
			Stream ResourceStream;
			XmlReader Reader;

			ViewDef MyViewDef;
			//****************************************

			_Views = new Dictionary<string, ViewDef>();

			using (ResourceStream = ResourceLoader.Load(_Provider.PrimaryAssembly, "WinForms.xml"))
			{
				Reader = XmlReader.Create(ResourceStream);

				if (!Reader.ReadToFollowing("WinForms"))
					throw new InvalidDataException("Not a valid WinForms Provider Definition");

				while (Reader.Read())
				{
					if (Reader.MoveToContent() == XmlNodeType.EndElement)
						break;

					switch (Reader.LocalName)
					{
					case "View":
						MyViewDef = new ViewDef(this, Reader);

						_Views.Add(MyViewDef.Name, MyViewDef);
						break;

					default:
						Reader.Skip();
						break;
					}
				}

				Reader.Close();
			}
		}
		
		internal ViewDef GetViewDef(string name)
		{	//****************************************
			ViewDef MyViewDef;
			//****************************************

			if (_Views.TryGetValue(name, out MyViewDef))
				return MyViewDef;

			//****************************************

			_Views.Add(name, MyViewDef = new ViewDef(name));
			
			return MyViewDef;
		}

		//****************************************

		internal WinToolkit Toolkit
		{
			get { return _Toolkit; }
		}
		
		internal GuiProvider Provider
		{
			get { return _Provider; }
		}
	}
}
