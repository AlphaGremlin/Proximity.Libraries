/****************************************\
 TerminalManager.cs
 Created: 2014-02-28
\****************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Proximity.Utility;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Provides a Terminal interface on top of the Console
	/// </summary>
	public sealed class TerminalManager : IDisposable
	{	//****************************************
		private bool _IsDisposed;
		
		private readonly bool _HasCommandLine, _IsRedirected;
		private readonly TerminalRegistry _Registry;
		//****************************************
		
		/// <summary>
		/// Creates a new Terminal Manager
		/// </summary>
		/// <param name="hasCommandLine">True to try and enable command-line input, False to be an output-only interface</param>
		public TerminalManager(bool hasCommandLine)
		{
			// Do we have output, or is it redirected?
			try
			{
				bool DummyBool = System.Console.CursorVisible;
				int TotalWidth = System.Console.BufferWidth;
				
				_IsRedirected = TotalWidth <= 0;
			}
			catch (IOException)
			{
				_IsRedirected = true;
			}
			
			// Do we have input, or is that redirected/unavailable?
			try
			{
				bool Dummy = System.Console.KeyAvailable;
				
				_HasCommandLine = hasCommandLine;
			}
			catch (IOException)
			{
				// Likely running as a service
				_HasCommandLine = false;
				_IsRedirected = true;
			}
			catch (InvalidOperationException)
			{
				// Input redirected from a file, don't bother initing the command line
				_HasCommandLine = false;
				_IsRedirected = true;
			}
			
			//****************************************
			
			_Registry = new TerminalRegistry();
		}
		
		//****************************************
		
		/// <summary>
		/// Returns control of the Console to the caller
		/// </summary>
		public void Dispose()
		{
			if (_IsDisposed)
				return;
			
			_IsDisposed = true;
		}
		
		/// <summary>
		/// Processes command-line input
		/// </summary>
		/// <remarks>Requires that <see cref="HasCommandLine" /> is True</remarks>
		public void ProcessInput()
		{
			if (!_HasCommandLine)
				throw new InvalidOperationException("Command-line is not available");
			
			
		}
		
		//****************************************
		
		/// <summary>
		/// Gets whether the command-line is available for input
		/// </summary>
		public bool HasCommandLine
		{
			get { return _HasCommandLine; }
		}
		
		public TerminalRegistry Registry
		{
			get { return _Registry; }
		}
	}
}