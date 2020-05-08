using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Proximity.Terminal
{
	/// <summary>
	/// Provides a Terminal interface on top of the Console
	/// </summary>
	/// <remarks>Static, as there is only one system console</remarks>
	public static class TerminalConsole
	{ //****************************************
		private static readonly char[] ClearMask = new[] { ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ' };

		private static TerminalInstance? _Instance;
		//****************************************

		public static void Initialise(bool hasCommandLine) => Initialise(hasCommandLine, new TerminalView(), TerminalTheme.Default);

		[CLSCompliant(false)]
		public static void Initialise(bool hasCommandLine, TerminalView view, TerminalTheme theme)
		{
			if (_Instance != null)
				throw new InvalidOperationException("Console is already initialised");

			var Instance = _Instance = new TerminalInstance(hasCommandLine, view, theme);

			Instance.Initialise();
		}

		public static void Terminate()
		{
			var Instance = Interlocked.Exchange(ref _Instance, null);

			if (Instance == null)
				throw new InvalidOperationException("Console is not initialised");

			Instance.Terminate();
		}

		//****************************************

		/// <summary>
		/// Gets the Terminal View attached to the Console, if initialised
		/// </summary>
		public static TerminalView? View => _Instance?.View;

		/// <summary>
		/// Gets whether the local command line is active
		/// </summary>
		public static bool HasCommandLine => _Instance?.HasCommandLine ?? false;

		//****************************************

		private sealed class TerminalInstance : ITerminalListener
		{ //****************************************
			private readonly AsyncCollection<ConsoleRecord?> _ConsoleOutput = new AsyncCollection<ConsoleRecord?>(128);

			private readonly CancellationTokenSource _TokenSource = new CancellationTokenSource();
			private Thread? _ConsoleThread;

			private StringBuilder? _InputLine;
			private List<string>? _CommandHistory;
			private int _CommandHistoryIndex, _InputIndex, _InputTop;
			private string? _PartialCommand;
			//****************************************

			internal TerminalInstance(bool hasCommandLine, TerminalView view, TerminalTheme theme)
			{
				View = view;
				Theme = theme;

				IsRedirected = Console.IsInputRedirected || Console.IsOutputRedirected || Console.BufferWidth == 0;
				HasCommandLine = hasCommandLine && !IsRedirected;

				if (HasCommandLine)
				{
					_InputLine = new StringBuilder();
					_CommandHistory = new List<string>();
					_CommandHistoryIndex = -1;
				}
			}

			//****************************************

			internal void Initialise()
			{
				// If the process exits unexpectedly, we need to restore the cursor visibility and colour
				AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

				var Thread = _ConsoleThread = new Thread(TerminalConsoleLoop)
				{
					Name = "Terminal I/O",
					IsBackground = true
				};

				Thread.Start();
			}

			internal void Terminate()
			{
				_TokenSource.Cancel();

				var ConsoleThread = Interlocked.Exchange(ref _ConsoleThread, null);

				if (ConsoleThread != null)
					ConsoleThread.Join();

				AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
			}

			//****************************************

			void ITerminalListener.Clear() => _ConsoleOutput.Add(null);

			void ITerminalListener.Log(ConsoleRecord record) => _ConsoleOutput.Add(record ?? throw new ArgumentNullException(nameof(record)));

			//****************************************

			private void OnProcessExit(object sender, EventArgs e)
			{
				throw new NotImplementedException();
			}

			private void TerminalConsoleLoop()
			{ //****************************************
				var InputVisible = false;
				int CountSinceKeyPress = 0, CountSinceOutput = 0;
				//****************************************

				while (!_TokenSource.IsCancellationRequested || _ConsoleOutput.Count != 0)
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
							if (!IsRedirected)
								Console.ForegroundColor = Theme.GetColour(MyRecord.Severity);

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
							_ConsoleOutput.Peek(_TokenSource.Token).GetAwaiter().GetResult();
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

			private void HandleConsoleKey(ConsoleKeyInfo keyData, ref bool inputVisible)
			{
				if (_InputLine == null || _CommandHistory == null)
					throw new InvalidOperationException("Command line not enabled");

				// If the user has pressed entry, try and execute the current command
				if (keyData.Key == ConsoleKey.Enter)
				{
					if (_InputLine.Length == 0)
						return;

					_CommandHistoryIndex = -1;

					var CurrentLine = _InputLine.ToString();

					if (inputVisible)
					{
						HideInputArea();
						inputVisible = false;
					}

					_InputLine.Length = 0;
					_InputIndex = 0;

					// Attempt to parse and execute the command
					ThreadPool.QueueUserWorkItem((state) => { var (View, CurrentLine) = (Tuple<TerminalView, string>)state; TerminalParser.Execute(View, CurrentLine); }, Tuple.Create(View, CurrentLine));

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

					var NewCommand = TerminalParser.FindNextCommand(_PartialCommand, _InputLine.ToString(), View.Registries);

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

			private void HideInputArea()
			{
				Console.SetCursorPosition(0, _InputTop);

				var ClearLength = Theme.Prompt.Length + _InputLine!.Length;

				while (ClearLength > 16)
				{
					Console.Write(ClearMask);
					ClearLength -= 16;
				}

				Console.Write(ClearMask, 0, ClearLength);

				Console.SetCursorPosition(0, _InputTop);
			}

			private void ShowInputArea()
			{
				var BufferWidth = Console.BufferWidth;

				Console.ForegroundColor = Theme.PromptColour;

				Console.Write(Theme.Prompt);
				Console.Write(_InputLine!.ToString());

				// This may have pushed the console down a line, so we need to calculate the start index afterwards
				_InputTop = Console.CursorTop - (_InputLine.Length + 2) / BufferWidth;

				// Now calculate where the cursor location is
				var RealPosition = (_InputIndex + 2);
				Console.SetCursorPosition(RealPosition - (RealPosition / BufferWidth) * BufferWidth, _InputTop + (RealPosition / BufferWidth));

				Console.ResetColor();
			}

			//****************************************

			public TerminalView View { get; }

			public TerminalTheme Theme { get; }

			public bool IsRedirected { get; }

			public bool HasCommandLine { get; }
		}
	}
}
