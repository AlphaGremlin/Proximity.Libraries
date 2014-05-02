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
		
		/// <inheritdoc />
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
		
		/// <inheritdoc />
		protected internal override void Finish()
		{
			if (_Stream != null)
				_Stream.Close();
			
			_Stream = null;
		}
		
		//****************************************
		
		/// <inheritdoc />
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
