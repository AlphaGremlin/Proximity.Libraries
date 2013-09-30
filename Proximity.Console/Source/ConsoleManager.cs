/****************************************\
 ConsoleManager.cs
 Created: 13-09-2009
\****************************************/
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using Proximity.Utility;
using Proximity.Utility.Logging;
//****************************************

namespace Proximity.Console
{
	/// <summary>
	/// Manages the input/output of the console data
	/// </summary>
	public static class ConsoleManager
	{	//****************************************
		private static ConsoleOutput _Output;
		private static StringBuilder _InputLine;
		private static List<string> _CommandHistory;
		private static int _CommandHistoryIndex, _InputIndex, _InputOffset, _InputLength;
		private static string _PartialCommand;
		
		private static bool _IsActive, _IsCommandLine, _IsNotRedirected, _IsCursorVisible;
		private static object _LockObject = new object();
		private static string _ClearMask;
		//****************************************
		
		/// <summary>
		/// Starts the Console Manager
		/// </summary>
		/// <param name="isCommandLine">Sets whether to initialise the command line system</param>
		public static void Start(bool isCommandLine)
		{
			if (_IsActive)
				return;

			try
			{
				bool DummyBool = System.Console.CursorVisible;
				
				int TotalWidth = System.Console.BufferWidth;
				
				_IsNotRedirected = TotalWidth > 0;
			}
			catch (IOException)
			{
				_IsNotRedirected = false;
			}
			
			try
			{
				bool Dummy = System.Console.KeyAvailable;
				
				_IsCommandLine = isCommandLine;
			}
			catch (IOException)
			{
				// Likely running as a service
				_IsCommandLine = false;
				_IsNotRedirected = false;
			}
			catch (InvalidOperationException)
			{
				// Input redirected from a file, don't bother initing the command line
				_IsCommandLine = false;
				_IsNotRedirected = false;
			}
			
			if (_IsNotRedirected)
			{
				System.Console.Title = Assembly.GetEntryAssembly().GetName().Name;
			}
			
			_IsActive = true;
			
			_Output = new ConsoleOutput();
			_Output.OnWriteLine += OnConsoleWriteLine;
			_Output.OnClear += OnConsoleClear;
			
			AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
			
			if (_IsCommandLine)
			{
				if (_IsNotRedirected)
				{
					System.Console.CursorVisible = true;
					_IsCursorVisible = true;
				}
				
				_InputLine = new StringBuilder();
				_CommandHistory = new List<string>();
				_CommandHistoryIndex = -1;
				
				if (_IsNotRedirected)
				{
					_ClearMask = string.Empty.PadRight(Math.Max(System.Console.BufferWidth - 1, 79)); // Never write to the last character, as it causes the window to scroll
					_InputLength = System.Console.BufferWidth - 2;
				}
				else
				{
					// Even though we're redirected, we may still want to accept text input. Eg: running in the VS output window
					_ClearMask = string.Empty.PadRight(79);
					_InputLength = 78;
				}

				ShowInput();
			}
			else if (_IsNotRedirected)
				System.Console.CursorVisible = false;
			
			LogManager.Outputs.Add(_Output);
		}
	
		/// <summary>
		/// Processes command line input
		/// </summary>
		/// <remarks>Requires that <see cref="Start" /> was called passing True for isCommandLine</remarks>
		public static void ProcessInput()
		{	//****************************************
			ConsoleKeyInfo KeyData;
			string CurrentLine;
			//****************************************
			
			if (!_IsCommandLine)
				return;
			
			while (System.Console.KeyAvailable)
			{
				KeyData = System.Console.ReadKey(true);
				
				if (KeyData.Key == ConsoleKey.Enter)
				{
					if (_InputLine.Length == 0)
						continue;
					
					_CommandHistoryIndex = -1;
					
					CurrentLine = _InputLine.ToString();
					_CommandHistory.Insert(0, CurrentLine);
					
					lock (_LockObject)
					{
						ClearInput();
						IsCursorVisible = false;
					}
					
					_InputLine.Length = 0;
					_InputIndex = 0;
					_InputOffset = 0;
					
					ConsoleParser.Execute(CurrentLine);
					
					lock (_LockObject)
					{
						IsCursorVisible = true;
						ShowInput();
					}
					
					continue;
				}
				
				lock (_LockObject)
				{
					ClearInput();
					
					if (KeyData.Key != ConsoleKey.Tab)
						_PartialCommand = null;
						
					switch (KeyData.Key)
					{
					case ConsoleKey.UpArrow:
						if (_CommandHistoryIndex < _CommandHistory.Count - 1)
						{
							_CommandHistoryIndex++;
							
							_InputLine.Length = 0;
							_InputLine.Append(_CommandHistory[_CommandHistoryIndex]);
							_InputIndex = _InputLine.Length;
							_InputOffset = Math.Max(0, _InputIndex - _InputLength + 1);
						}
						break;
						
					case ConsoleKey.DownArrow:
						if (_CommandHistoryIndex >= 0)
						{
							_CommandHistoryIndex--;
						
							_InputLine.Length = 0;
							if (_CommandHistoryIndex != -1)
								_InputLine.Append(_CommandHistory[_CommandHistoryIndex]);
	
							_InputIndex = _InputLine.Length;
							_InputOffset = Math.Max(0, _InputIndex - _InputLength + 1);
						}
						break;
						
					case ConsoleKey.LeftArrow:
						if (_InputIndex > 0)
							_InputIndex--;
						
						if (_InputIndex <= _InputOffset && _InputOffset != 0)
							_InputOffset--;
						break;
						
					case ConsoleKey.RightArrow:
						if (_InputIndex < _InputLine.Length)
							_InputIndex++;
						
						if (_InputIndex >= _InputLength)
							_InputOffset = _InputIndex - _InputLength + 1;
						break;
						
					case ConsoleKey.Tab:
						if (_PartialCommand == null)
							_PartialCommand = _InputLine.ToString();
						
						string NewCommand = ConsoleParser.FindNextCommand(_PartialCommand, _InputLine.ToString());
						
						if (NewCommand == null) // No matching commands
							break;
						
						_InputLine.Length = 0;
						_InputLine.Append(NewCommand);

						_InputIndex = _InputLine.Length;
						_InputOffset = Math.Max(0, _InputIndex - _InputLength + 1);
						break;
						
					case ConsoleKey.Home:
						_InputIndex = 0;
						_InputOffset = 0;
						break;
						
					case ConsoleKey.End:
						_InputIndex = _InputLine.Length;
						_InputOffset = Math.Max(0, _InputIndex - _InputLength + 1);
						break;
						
					case ConsoleKey.Escape:
						_InputLine.Length = 0;
						_InputIndex = 0;
						_InputOffset = 0;
						break;
						
					case ConsoleKey.Backspace:
						if (_InputIndex > 0)
						{
							// Remove the previous character at the input point
							_InputLine.Remove(_InputIndex - 1, 1);
							
							_InputIndex--;
							
							if (_InputIndex <= _InputOffset && _InputOffset != 0)
								_InputOffset--;
						}
						break;
						
					case ConsoleKey.Delete:
						if (_InputIndex < _InputLine.Length)
						{
							_InputLine.Remove(_InputIndex, 1);
						}
						break;
						
					default:
						if (KeyData.KeyChar == '\0')
							break;
						
						_InputLine.Insert(_InputIndex, KeyData.KeyChar);
						_InputIndex++;
						
						if (_InputIndex >= _InputLength)
							_InputOffset = _InputIndex - _InputLength + 1;
						break;
					}
					
					ShowInput();
				}
			}
		}
		
		/// <summary>
		/// Ends the Console Manager
		/// </summary>
		public static void End()
		{
			if (!_IsActive)
				return;
			
			lock (_LockObject)
			{
				_IsActive = false;
				
				ClearInput();

				IsCursorVisible = true;
				
				if (_IsNotRedirected)
					System.Console.ResetColor();
				
				_InputLine = null;
				
				AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
			}
		}
	
		//****************************************
		
		private static void ClearInput()
		{	//****************************************
			int LineTop;
			//****************************************

			lock (_LockObject)
			{
				// Is our input visible?
				if (!IsCursorVisible || !_IsNotRedirected)
					return;

				LineTop = System.Console.CursorTop;
					
				System.Console.SetCursorPosition(0, LineTop);
				System.Console.Write(_ClearMask);
				System.Console.SetCursorPosition(0, LineTop);
			}
		}
		
		private static void ShowInput()
		{
			lock (_LockObject)
			{
				// Is our input visible?
				if (!IsCursorVisible || !_IsNotRedirected)
					return;
				
				System.Console.ForegroundColor = ConsoleColor.Green;
				
				System.Console.Write("> {0}", _InputLine.ToString(_InputOffset, Math.Min(_InputLine.Length - _InputOffset, _InputLength)));
				
				System.Console.SetCursorPosition(_InputIndex - _InputOffset + 2, System.Console.CursorTop);
				
				System.Console.ResetColor();
			}
		}
		
		//****************************************
		
		private static void OnConsoleWriteLine(ConsoleRecord newRecord)
		{
			lock (_LockObject)
			{
				ClearInput();
				
				if (_IsActive && _IsNotRedirected)
					System.Console.ForegroundColor = newRecord.ConsoleColour;
				System.Console.WriteLine(newRecord.Text);
				
				ShowInput();
			}
		}
				
		private static void OnConsoleClear()
		{
			lock (_LockObject)
			{
				if (_IsNotRedirected)
					System.Console.Clear();
				
				ShowInput();
			}
		}
		
		private static void OnProcessExit(object sender, EventArgs e)
		{
			if (!_IsNotRedirected)
				return;
			
			try
			{
				System.Console.CursorVisible = true;
				System.Console.ResetColor();
			}
			catch (IOException)
			{
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the log output manager
		/// </summary>
		/// <remarks>Provides output history for the console</remarks>
		public static ConsoleOutput Output
		{
			get { return _Output; }
		}
		
		/// <summary>
		/// Gets whether the Console Manager is active
		/// </summary>
		/// <remarks>When inactive, disables colouring</remarks>
		internal static bool IsActive
		{
			get { return _IsActive; }
		}
		
		internal static bool IsCommandLine
		{
			get { return _IsCommandLine; }
		}
		
		private static bool IsCursorVisible
		{
			get { return _IsCursorVisible; }
			set
			{
				_IsCursorVisible = value;
				
				if (_IsNotRedirected)
					System.Console.CursorVisible = value;
			}
		}
	}
}
