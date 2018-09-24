using System;
using System.Xml;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.Collections.Generic;
using Proximity.Utility.Logging.Outputs;
//****************************************

namespace Proximity.Utility.Logging.Config
{
	/// <summary>
	/// An configuration entry defining a Logging Output
	/// </summary>
	public sealed class OutputConfig : ConfigurationElement
	{
		/// <summary>
		/// Creates a new Output Configuration element
		/// </summary>
		public OutputConfig()
		{
		}

		//****************************************
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="serializeCollectionKey"></param>
		protected sealed override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
		{	//****************************************
			Type MyOutputType;
			Assembly OutputAssembly;
			string TypeName;
			//****************************************
			
			// Check we have a Type attribute
			if (!reader.MoveToAttribute("Type"))
			{
				Debug.Print("Output has no Type");
				
				return;
			}
			
			OutputType = reader.Value;
			
			//****************************************
			
			if (OutputType.IndexOf(',') == -1)
			{ // No Assembly definition, just a Type
				TypeName = OutputType;
				OutputAssembly = typeof(OutputConfig).Assembly;
				MyOutputType = OutputAssembly.GetType(typeof(FileOutput).Namespace + System.Type.Delimiter + TypeName);
			}
			else
			{ // Assembly definition, split
				string[] SplitType = OutputType.Split(',');
				
				OutputAssembly = Assembly.Load(string.Join(",", SplitType, 1, SplitType.Length - 1));
				
				if (OutputAssembly == null)
				{
					Debug.Print("Output Assembly is missing");
					
					reader.Skip();
					return;
				}
				
				MyOutputType = OutputAssembly.GetType(SplitType[0]);
			}
			
			if (MyOutputType == null)
			{
				Debug.Print("Output Type is missing");
				
				reader.Skip();
				return;
			}
			
			Output = Activator.CreateInstance(MyOutputType, reader) as LogOutput;
			
			if (Output == null)
			{
				Debug.Print("Output could not be created");
				
				reader.Skip();
				return;
			}
			
			reader.Read();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="serializeCollectionKey"></param>
		/// <returns></returns>
		protected sealed override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey) => base.SerializeElement(writer, serializeCollectionKey);

		//****************************************

		/// <summary>
		/// Gets/Sets the type of output to log
		/// </summary>
		public string OutputType { get; set; }

		/// <summary>
		/// Gets/Sets the output object
		/// </summary>
		public LogOutput Output { get; private set; }
	}
}