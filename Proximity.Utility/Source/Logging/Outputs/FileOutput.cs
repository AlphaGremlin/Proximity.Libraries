/****************************************\
 FileOutput.cs
 Created: 2-06-2009
\****************************************/
using System;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Diagnostics;
//****************************************

namespace Proximity.Utility.Logging.Outputs
{
	/// <summary>
	/// Base class for all outputs that write to a local file
	/// </summary>
	public abstract class FileOutput : LogOutput
	{	//****************************************
		private string _FileName;

		private Stream _Stream;
		//****************************************
		
		/// <summary>
		/// Creates a new File Outout
		/// </summary>
		/// <param name="reader">Configuration Settings</param>
		protected FileOutput(XmlReader reader) : base(reader)
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Starts the logging output process
		/// </summary>
		protected internal override void Start()
		{
			string FormattedFileName = _FileName;
			string FilePath;

			FormattedFileName = FormattedFileName.Replace("{datetime}", DateTime.Now.ToString("yyyyMMdd'T'HHmmss"));
			
			FilePath = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", Path.Combine(LogManager.OutputPath, FormattedFileName), GetExtension());
			
			try
			{
				_Stream = File.Open(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
			}
			catch(IOException)
			{
				FilePath = string.Format(CultureInfo.InvariantCulture, "{0} ({1}).{2}", Path.Combine(LogManager.OutputPath, FormattedFileName), Process.GetCurrentProcess().Id, GetExtension());
				
				try
				{
					_Stream = File.Open(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
				}
				catch (IOException e)
				{
					Debug.Print(e.Message);
				}
			}
			
			Debug.Print(FilePath);
		}
		
		/// <summary>
		/// Ends the logging output process
		/// </summary>
		protected internal override void Finish()
		{
			if (_Stream != null)
				_Stream.Close();
			
			_Stream = null;
		}
		
		//****************************************
		
		/// <summary>
		/// Reads an attribute from the configuration
		/// </summary>
		/// <param name="name">The name of the attribute</param>
		/// <param name="value">The attribute's value</param>
		/// <returns>True if the Attribute is known, otherwise False</returns>
		protected override bool ReadAttribute(string name, string value)
		{
			switch (name)
			{
			case "FileName":
				_FileName = value;
				break;
				
			default:
				return base.ReadAttribute(name, value);
			}
			
			return true;
		}
		
		/// <summary>
		/// Retrieves the extension of the file to create
		/// </summary>
		/// <returns>The extension  of the file</returns>
		protected virtual string GetExtension()
		{
			return "txt";
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets the filename to write to, minus the extension
		/// </summary>
		public string FileName
		{
			get { return _FileName; }
			set
			{
				if (_Stream != null)
					throw new InvalidOperationException("Cannot change output file whilst logging is running");

				_FileName = value;
			}
		}

		/// <summary>
		/// Gets the stream to be written to
		/// </summary>
		protected Stream Stream
		{
			get { return _Stream; }
		}
	}
}
