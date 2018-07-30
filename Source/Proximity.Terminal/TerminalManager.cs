using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using Proximity.Utility;
using Proximity.Utility.Collections;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Provides a Terminal interface on top of the Console
	/// </summary>
	/// <remarks>Static, as there is only one system console</remarks>
	[SecurityCritical]
	public static class TerminalManager
	{ //****************************************
		private static readonly AsyncCollection<ConsoleRecord> _ConsoleOutput = new AsyncCollection<ConsoleRecord>(128);
		private static CancellationTokenSource _ConsoleToken;
		private static Thread _ConsoleThread;

		private static StringBuilder _InputLine;
		private static List<string> _CommandHistory;
		private static int _CommandHistoryIndex, _InputIndex, _InputTop;
		private static string _PartialCommand;

		private static bool _IsRedirected;
		private static readonly char[] _ClearMask = string.Empty.PadRight(16).ToCharArray();
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
			if (_ConsoleToken != null && !_ConsoleToken.IsCancellationRequested)
				throw new InvalidOperationException("Console is already initialised");

			Registry = registry;

			//****************************************

			// No console if the input/output is redirected
			_IsRedirected = Console.IsInputRedirected || Console.IsOutputRedirected;

			if (_IsRedirected)
				HasCommandLine = false;
			else
				HasCommandLine = hasCommandLine;

			//****************************************

			// If the process exits unexpectedly, we need to restore the cursor visibility and colour
			AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

			if (HasCommandLine)
			{
				_InputLine = new StringBuilder();
				_CommandHistory = new List<string>();
				_CommandHistoryIndex = -1;
			}

			_ConsoleToken = new CancellationTokenSource();
			_ConsoleThread = new Thread(TerminalConsoleEntry)
			{
				Name = "Terminal I/O",
				IsBackground = true
			};
			_ConsoleThread.Start();
		}

		/// <summary>
		/// Returns control of the Console to the caller
		/// </summary>
		public static void Terminate()
		{
			if (_ConsoleToken == null || _ConsoleToken.IsCancellationRequested)
				return;

			_ConsoleToken.Cancel();

			var ConsoleThread = Interlocked.Exchange(ref _ConsoleThread, null);

			if (ConsoleThread != null)
				ConsoleThread.Join();

			AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
		}
		
		//****************************************

		/// <summary>
		/// Clears the console
		/// </summary>
		public static void Clear()
		{
			_ConsoleOutput.Add(null);
		}

		//****************************************

		internal static void WriteLine(ConsoleRecord record)
		{
			_ConsoleOutput.Add(record);
		}

		//****************************************

		private static void OnProcessExit(object sender, EventArgs e)
		{
			try
			{
				Console.ResetColor();
			}
			catch
			{
			}
		}

		private static void TerminalConsoleEntry()
		{ //****************************************
			var InputVisible = false;
			int CountSinceKeyPress = 0, CountSinceOutput = 0;
			//****************************************

			while (!_ConsoleToken.IsCancellationRequested || _ConsoleOutput.Count != 0)
			{
				while (_ConsoleOutput.TryTake(out var MyRecord))
				{
					if (InputVisible)
					{
						// Clear the input line
						HideInputArea();

						InputVisible = false;
						CountSinceOutput = 0;
					}

					// Write the output
					if (MyRecord == null)
					{
						Console.Clear();
					}
					else
					{
						if (!_IsRedirected)
							Console.ForegroundColor = MyRecord.ConsoleColour;

						Console.WriteLine(MyRecord.Text);
					}

					// Handle any input
					if (HasCommandLine && Console.KeyAvailable)
					{
						CountSinceKeyPress = 0;
						HandleConsoleKey(Console.ReadKey(true), ref InputVisible);
					}
				}

				if (HasCommandLine)
				{
					while (Console.KeyAvailable)
					{
						CountSinceKeyPress = 0;
						HandleConsoleKey(Console.ReadKey(true), ref InputVisible);
					}

					if (!InputVisible)
					{
						// Show the input line
						ShowInputArea();
						InputVisible = true;
					}

					if (CountSinceKeyPress < 128) // ~= 640ms
						Thread.Sleep(5);
					else if (CountSinceKeyPress < 256 && CountSinceOutput < 256) // ~= 1,280ms
						Thread.Sleep(10);
					else
						Thread.Sleep(250);

					CountSinceKeyPress++;
					CountSinceOutput++;
				}
				else
				{
					try
					{
						_ConsoleOutput.Peek(_ConsoleToken.Token).Wait();
					}
					catch (OperationCanceledException)
					{
					}
				}
			}

			if (InputVisible)
				// Clear the input line
				HideInputArea();

			Console.ResetColor();
		}

		private static void HandleConsoleKey(ConsoleKeyInfo keyData, ref bool inputVisible)
		{	//****************************************
			string CurrentLine;
			//****************************************

			// If the user has pressed entry, try and execute the current command
			if (keyData.Key == ConsoleKey.Enter)
			{
				if (_InputLine.Length == 0)
					return;

				_CommandHistoryIndex = -1;

				CurrentLine = _InputLine.ToString();

				if (inputVisible)
				{
					HideInputArea();
					inputVisible = false;
				}

				_InputLine.Length = 0;
				_InputIndex = 0;

				// Attempt to parse and execute the command
				ThreadPool.QueueUserWorkItem((state) => TerminalParser.Execute((string)state, Registry), CurrentLine);

				// Store the new line into the history, as long as it's not already the most recent entry
				if (_CommandHistory.Count == 0 || _CommandHistory[0] != CurrentLine)
					_CommandHistory.Insert(0, CurrentLine);

				return;
			}

			//****************************************

			if (inputVisible)
			{
				HideInputArea();
				inputVisible = false;
			}

			if (keyData.Key != ConsoleKey.Tab)
				_PartialCommand = null;

			var Width = Console.BufferWidth;

			switch (keyData.Key)
			{
			case ConsoleKey.UpArrow:
				if (keyData.Modifiers.HasFlag(ConsoleModifiers.Control))
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
				if (keyData.Modifiers.HasFlag(ConsoleModifiers.Control))
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
					if (keyData.Modifiers.HasFlag(ConsoleModifiers.Control))
						_InputIndex = Math.Max(_InputLine.ToString().LastIndexOf(' ', Math.Max(_InputIndex - 1, 0)), 0);
					else
						_InputIndex--;
				}
				break;

			case ConsoleKey.RightArrow:
				if (_InputIndex < _InputLine.Length)
				{
					if (keyData.Modifiers.HasFlag(ConsoleModifiers.Control))
					{
						_InputIndex = _InputLine.ToString().IndexOf(' ', Math.Min(_InputIndex + 1, _InputLine.Length - 1));

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

				string NewCommand = TerminalParser.FindNextCommand(_PartialCommand, _InputLine.ToString(), Registry);

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
				if (keyData.KeyChar == '\0')
					break;

				_InputLine.Insert(_InputIndex, keyData.KeyChar);
				_InputIndex++;
				break;
			}
		}

		private static void HideInputArea()
		{
			Console.SetCursorPosition(0, _InputTop);

			Console.Write("  ");
			for (var Index = 0; Index < _InputLine.Length; Index += 16)
			{
				Console.Write(_ClearMask, 0, Math.Min(16, _InputLine.Length - Index));
			}

			Console.SetCursorPosition(0, _InputTop);
		}
		
		private static void ShowInputArea()
		{
			var BufferWidth = Console.BufferWidth;

			Console.ForegroundColor = ConsoleColor.Green;

			Console.Write("> ");
			Console.Write(_InputLine.ToString());

			// This may have pushed the console down a line, so we need to calculate the start index afterwards
			_InputTop = Console.CursorTop - (_InputLine.Length + 2) / BufferWidth;

			// Now calculate where the cursor location is
			var RealPosition = (_InputIndex + 2);
			Console.SetCursorPosition(RealPosition - (RealPosition / BufferWidth) * BufferWidth, _InputTop + (RealPosition / BufferWidth));

			Console.ResetColor();
		}

		/*
		private static int FindLastWord(StringBuilder source, int index)
		{
			bool IsDivider = false;

			while (index > 0)
			{
				switch (char.GetUnicodeCategory(source[index]))
				{
				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.UppercaseLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.OtherLetter:
				case UnicodeCategory.ModifierLetter:
				case UnicodeCategory.DecimalDigitNumber:
					if (IsDivider)
						return index + 1;

					index--;
					break;

				default:
					IsDivider = true;
					index--;
					break;
				}
			}

			return index;
		}
		*/
		//****************************************

		/// <summary>
		/// Gets whether the command-line is available for input
		/// </summary>
		public static bool HasCommandLine { get; private set; }

		/// <summary>
		/// Gets the command registry used by the terminal
		/// </summary>
		public static TerminalRegistry[] Registry { get; private set; }
	}
}