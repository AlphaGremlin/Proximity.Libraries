/****************************************\
 ConsoleState.cs
 Created: 6-02-2008
\****************************************/
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections.Generic;
using Proximity.Utility;
//****************************************

namespace Proximity.Console
{
	/// <summary>
	/// Stores a snapshot of the console
	/// </summary>
	[Serializable()]
	public class ConsoleState : IXmlSerializable
	{	//****************************************
		private Dictionary<string, object> Variables;
		//****************************************

		/// <summary>
		/// Creates a new, empty console state
		/// </summary>
		public ConsoleState()
		{
			Variables = new Dictionary<string, object>();
		}

		//****************************************

		XmlSchema IXmlSerializable.GetSchema()
		{
			return null;
		}

		void IXmlSerializable.ReadXml(XmlReader reader)
		{	//****************************************
			string Variable;
			//****************************************

			Variables.Clear();

			while (true)
			{
				reader.Read();
				if (reader.MoveToContent() == XmlNodeType.EndElement)
					break;

				Variable = reader.LocalName;

				if (reader.MoveToAttribute("Value"))
					Variables.Add(Variable, reader.Value);
				else
					Variables.Add(Variable, null);

				reader.Skip();
			}
		}

		void IXmlSerializable.WriteXml(XmlWriter writer)
		{	//****************************************
			object Value;
			//****************************************

			foreach (KeyValuePair<string, object> Variable in Variables)
			{
				writer.WriteStartElement(Variable.Key);

				Value = Variable.Value;

				if (Value != null)
					writer.WriteAttributeString("Value", Value.ToString());

				writer.WriteEndElement();
			}
		}

		//****************************************

		/// <summary>
		/// Captures the current state of the console into this object
		/// </summary>
		public void Capture()
		{	//****************************************
			object[] Attribs;
			//****************************************

			Variables.Clear();

			foreach (PropertyInfo Variable in ConsoleParser.RawListVariables())
			{
				if (!Variable.CanWrite)
					continue;

				Attribs = Variable.GetCustomAttributes(typeof(ConsoleBindingAttribute), false);

				if (Attribs.Length == 0)
					continue;

				if (!((ConsoleBindingAttribute)Attribs[0]).Persist)
					continue;

				Variables.Add(Variable.Name, Variable.GetValue(null, null));
			}
		}

		/// <summary>
		/// Restores the state of the console from this object
		/// </summary>
		public void Restore()
		{
			foreach (KeyValuePair<string, object> Variable in Variables)
			{
				if (Variable.Value == null)
					ConsoleParser.ClearVariable(Variable.Key);
				else
					ConsoleParser.SetVariable(Variable.Key, Variable.Value.ToString());
			}
		}
	}
}
