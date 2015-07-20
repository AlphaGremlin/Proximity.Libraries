/****************************************\
 TerminalManager.cs
 Created: 2014-02-28
\****************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Proximity.Utility;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Provides a Terminal interface on top of the Console
	/// </summary>
	/// <remarks>Static, as there is only one system console</remarks>
	public static class TerminalManager
	{	//****************************************
		private static object _LockObject = new object();
		private static int _IsDisposed = -1;
		
		private static StringBuilder _InputLine;
		private static List<string> _CommandHistory;
		private static int _CommandHistoryIndex, _InputIndex, _InputTop;
		private static int? _BufferWidth;
		private static string _PartialCommand;
		
		private static bool _HasCommandLine, _IsRedirected, _IsCursorVisible;
		private static TerminalRegistry[] _Registry;
		private static char[] _ClearMask = string.Empty.PadRight(16).ToCharArray();
		//****************************************
		
		/// <summary>
		/// Initialises the Terminal Manager
		/// </summary>
		/// <param name="hasCommandLine">True to try and enable command-line input, False to be an output-only interface</param>
		public static void Initialise(bool hasCommandLine)
		{
			Initialise(hasCommandLine, TerminalRegistry.Global);
		}
		
		/// <summary>
		/// Initialises the Terminal Manager
		/// </summary>
		/// <param name="hasCommandLine">True to try and enable command-line input, False to be an output-only interface</param>
		/// <param name="registry"></param>
		public static void Initialise(bool hasCommandLine, params TerminalRegistry[] registry)
		{
			if (Interlocked.Exchange(ref _IsDisposed, 0) == 0)
				throw new InvalidOperationException("Console is already initialised");
			
			_Registry = registry;
			
			//****************************************
			
			// Do we have output, or is it redirected?
			try
			{
				int TotalWidth = Console.BufferWidth;
				bool DummyBool = Console.CursorVisible;
				
				_IsRedirected = TotalWidth <= 0;
			}
			catch (IOException)
			{
				_IsRedirected = true;
			}
			
			// Do we have input, or is that redirected/unavailable?
			try
			{
				bool Dummy = Console.KeyAvailable;
				
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
			
			if (!_IsRedirected)
			{
				// If the process exits unexpectedly, we need to restore the cursor visibility and colour
				AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
				
				// Don't show the cursor by default
				IsCursorVisible = false;
			}
			
			if (_HasCommandLine)
			{
				_InputLine = new StringBuilder();
				_CommandHistory = new List<string>();
				_CommandHistoryIndex = -1;
				
				// It's possible to be redirected but still have a command line (ie: Visual Studio Output window)
				if (_IsRedirected)
					_BufferWidth = 80;
				else
					IsCursorVisible = true;

				ShowInputArea();
			}
		}
		
		/// <summary>
		/// Returns control of the Console to the caller
		/// </summary>
		public static void Terminate()
		{
			if (Interlocked.Exchange(ref _IsDisposed, 1) != 0)
				return;
			
			lock (_LockObject)
			{
				AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;

				// Clear the input area
				if (IsCursorVisible && !_IsRedirected)
				{
					Console.SetCursorPosition(0, _InputTop);
					
					Console.Write("  ");
					for (int Index = 0; Index > _InputLine.Length; Index++)
					{
						Console.Write(' ');
					}
					
					Console.SetCursorPosition(0, _InputTop);
				}
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Processes command-line input
		/// </summary>
		/// <remarks>Requires that <see cref="HasCommandLine" /> is True</remarks>
		public static void ProcessInput()
		{	//****************************************
			ConsoleKeyInfo KeyData;
			string CurrentLine;
			//****************************************
			
			if (!_HasCommandLine)
				throw new InvalidOperationException("Command-line is not available");
			
			while (Console.KeyAvailable)
			{
				KeyData = Console.ReadKey(true);
				
				// If the user has pressed entry, try and execute the current command
				if (KeyData.Key == ConsoleKey.Enter)
				{
					if (_InputLine.Length == 0)
						continue;
					
					_CommandHistoryIndex = -1;
					
					CurrentLine = _InputLine.ToString();
					
					lock (_LockObject)
					{
						HideInputArea();
						IsCursorVisible = false;
					}
					
					_InputLine.Length = 0;
					_InputIndex = 0;
					
					// Attempt to parse an execute the command
					if (TerminalParser.Execute(CurrentLine, _Registry).Result)
					{
						// Store the new line into the history, as long as it's not already the most recent entry
						if (_CommandHistory.Count == 0 || _CommandHistory[0] != CurrentLine)
							_CommandHistory.Insert(0, CurrentLine);
					}
					else
					{
						// True
						_InputLine.Append(CurrentLine);
						_InputIndex = _InputLine.Length;
					}
					
					lock (_LockObject)
					{
						IsCursorVisible = true;
						ShowInputArea();
					}
					
					continue;
				}
				
				//****************************************
				
				lock (_LockObject)
				{
					HideInputArea();
					
					if (KeyData.Key != ConsoleKey.Tab)
						_PartialCommand = null;
					
					var Width = BufferWidth;
					
					switch (KeyData.Key)
					{
					case ConsoleKey.UpArrow:
						if (KeyData.Modifiers.HasFlag(ConsoleModifiers.Control))
						{
							if (_InputIndex >= Width - 2)
								_InputIndex -= Width;
							else
								_InputIndex = 0;
						}
						else if (_CommandHistoryIndex < _CommandHistory.Count - 1)
						{
							_CommandHistoryIndex++;
							
							_InputLine.Length = 0;
							_InputLine.Append(_CommandHistory[_CommandHistoryIndex]);
							_InputIndex = _InputLine.Length;
						}
						break;
						
					case ConsoleKey.DownArrow:
						if (KeyData.Modifiers.HasFlag(ConsoleModifiers.Control))
						{
							if (_InputIndex < Width - 2)
								_InputIndex = Math.Min(_InputIndex + Width, _InputLine.Length);
							else
								_InputIndex = _InputLine.Length;
						}
						else if (_CommandHistoryIndex >= 0)
						{
							_CommandHistoryIndex--;
						
							_InputLine.Length = 0;
							if (_CommandHistoryIndex != -1)
								_InputLine.Append(_CommandHistory[_CommandHistoryIndex]);
	
							_InputIndex = _InputLine.Length;
						}
						break;
						
					case ConsoleKey.LeftArrow:
						if (_InputIndex > 0)
						{
							if (KeyData.Modifiers.HasFlag(ConsoleModifiers.Control))
								_InputIndex = Math.Max(_InputLine.ToString().LastIndexOf(' ', _InputIndex), 0);
							else
								_InputIndex--;
						}
						break;
						
					case ConsoleKey.RightArrow:
						if (_InputIndex < _InputLine.Length)
						{
							if (KeyData.Modifiers.HasFlag(ConsoleModifiers.Control))
							{
								_InputIndex = _InputLine.ToString().IndexOf(' ', _InputIndex);
								
								if (_InputIndex == -1)
									_InputIndex = _InputLine.Length;
							}
							else
								_InputIndex++;
						}
						break;
						
					case ConsoleKey.Tab:
						if (_PartialCommand == null)
							_PartialCommand = _InputLine.ToString();
						
						string NewCommand = TerminalParser.FindNextCommand(_PartialCommand, _InputLine.ToString(), _Registry);
						
						if (NewCommand == null) // No matching commands
							break;
						
						_InputLine.Length = 0;
						_InputLine.Append(NewCommand);

						_InputIndex = _InputLine.Length;
						break;
						
					case ConsoleKey.Home:
						_InputIndex = 0;
						break;
						
					case ConsoleKey.End:
						_InputIndex = _InputLine.Length;
						break;
						
					case ConsoleKey.Escape:
						_InputLine.Length = 0;
						_InputIndex = 0;
						break;
						
					case ConsoleKey.Backspace:
						if (_InputIndex > 0)
						{
							// Remove the previous character at the input point
							_InputLine.Remove(_InputIndex - 1, 1);
							
							_InputIndex--;
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
						break;
					}
					
					ShowInputArea();
				}
			}
		}
		
		/// <summary>
		/// Clears the console
		/// </summary>
		public static void Clear()
		{
			lock (_LockObject)
			{
				if (!_IsRedirected)
					Console.Clear();
				
				ShowInputArea();
			}
		}
		
		/// <summary>
		/// Writes a line of text to the console
		/// </summary>
		/// <param name="output">The text to write to the console</param>
		/// <param name="color">The text colour to apply</param>
		public static void WriteLine(string output, ConsoleColor color)
		{
			lock (_LockObject)
			{
				HideInputArea();
				
				if (_IsDisposed == 0 && !_IsRedirected)
					Console.ForegroundColor = color;
				
				Console.WriteLine(output);
				
				ShowInputArea();
			}
		}
		
		//****************************************
		
		private static void OnProcessExit(object sender, EventArgs e)
		{
			Console.CursorVisible = true;
		}
		
		private static void HideInputArea()
		{
			int Index;
			
			lock (_LockObject)
			{
				// Is our input visible?
				if (_IsDisposed != 0 || !IsCursorVisible || _IsRedirected)
					return;

				Console.SetCursorPosition(0, _InputTop);
				
				Console.Write("  ");
				for (Index = 16; Index < _InputLine.Length; Index += 16)
				{
					Console.Write(_ClearMask);
				}
				
				if (Index - 16 != _InputLine.Length)
					Console.Write(_ClearMask, 0, _InputLine.Length - (Index - 16));
				
				Console.SetCursorPosition(0, _InputTop);
			}
		}
		
		private static void ShowInputArea()
		{
			lock (_LockObject)
			{
				// Is our input visible?
				if (_IsDisposed != 0 || !IsCursorVisible || _IsRedirected)
					return;
				
				var Width = BufferWidth;
				
				Console.ForegroundColor = ConsoleColor.Green;
				
				Console.Write("> ");
				Console.Write(_InputLine.ToString());
				
				// This may have pushed the console down a line, so we need to calculate the start index afterwards
				_InputTop = Console.CursorTop - (_InputLine.Length + 2) / Width;
				
				// Now calculate where the cursor location is
				var RealPosition = (_InputIndex + 2);
				Console.SetCursorPosition(RealPosition - (RealPosition / Width) * Width, _InputTop + (RealPosition / Width));
				
				Console.ResetColor();
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Gets whether the command-line is available for input
		/// </summary>
		public static bool HasCommandLine
		{
			get { return _HasCommandLine; }
		}
		
		/// <summary>
		/// Gets the command registry used by the terminal
		/// </summary>
		public static TerminalRegistry[] Registry
		{
			get { return _Registry; }
		}
		
		private static bool IsCursorVisible
		{
			get { return _IsCursorVisible; }
			set
			{
				_IsCursorVisible = value;
				
				if (!_IsRedirected)
					Console.CursorVisible = value;
			}
		}
		
		private static int BufferWidth
		{
			get { return _BufferWidth ?? Console.BufferWidth; }
		}
	}
}