/****************************************\
 LocalDef.cs
 Created: 26-04-10
\****************************************/
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using Proximity.Gui.Presentation;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.WinForms
{
	internal class LocalDef
	{	//****************************************
		private int _Index;
		private string _Name;
		private string _LocalPath;
		//****************************************
		
		internal LocalDef(int index, string localPath)
		{
			_Index = index;
			_LocalPath = localPath;
		}
		
		internal LocalDef(string name, string localPath)
		{
			_Name = name;
			_LocalPath = localPath;
		}
		
		//****************************************
		
		internal int Index
		{
			get { return _Index; }
		}
		
		internal string Name
		{
			get { return _Name; }
		}
		
		internal string LocalPath
		{
			get { return _LocalPath; }
		}
	}
}
