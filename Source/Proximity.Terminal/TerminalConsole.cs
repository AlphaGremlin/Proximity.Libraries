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

		/// <summary>
		/// Initialises the console to act as a Terminal, automatically determining whether to offer a commandline based on <see cref="Environment.UserInteractive"/>
		/// </summary>
		public static void Initialise() => Initialise(Environment.UserInteractive, new TerminalView(), TerminalTheme.Default);

		/// <summary>
		/// Initialises the console to act as a Terminal
		/// </summary>
		/// <param name="hasCommandLine">Whether to offer an interactive command line</param>
		public static void Initialise(bool hasCommandLine) => Initialise(hasCommandLine, new TerminalView(), TerminalTheme.Default);

		/// <summary>
		/// Initialises the system console to act as a Terminal
		/// </summary>
		/// <param name="hasCommandLine">Whether to offer an interactive command line</param>
		/// <param name="view">The Terminal View to bind to</param>
		/// <param name="theme">The Terminal Theme to use for the output</param>
		public static void Initialise(bool hasCommandLine, TerminalView view, TerminalTheme theme)
		{
			if (_Instance != null)
				throw new InvalidOperationException("Console is already initialised");

			var Instance = _Instance = new TerminalInstance(hasCommandLine, view, theme);

			Instance.Initialise();
		}

		/// <summary>
		/// Releases and restores the console
		/// </summary>
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
			private readonly AsyncCollection<ConsoleRecord?> _ConsoleOutput = new(128);

			private readonly CancellationTokenSource _TokenSource = new();
			private readonly ConsoleColor _InitialColour;
			private Thread? _TerminalThread;

			private readonly StringBuilder _Output = new();
			private readonly StringBuilder? _InputLine;
			private readonly List<string>? _CommandHistory;
			private int _CommandHistoryIndex, _InputIndex, _InputTop;
			private string? _PartialCommand;
			//****************************************

			internal TerminalInstance(bool hasCommandLine, TerminalView view, TerminalTheme theme)
			{
				View = view;
				Theme = theme;

				IsRedirected = Console.IsInputRedirected || Console.IsOutputRedirected || Console.BufferWidth == 0;
				HasCommandLine = hasCommandLine && !IsRedirected;

				if (!IsRedirected)
					_InitialColour = Console.ForegroundColor;

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

				var Thread = _TerminalThread = new Thread(TerminalIOLoop)
				{
					Name = "Terminal I/O",
					IsBackground = true
				};

				View.Attach(this);

				Thread.Start();
			}

			internal void Terminate()
			{
				_TokenSource.Cancel();

				Interlocked.Exchange(ref _TerminalThread, null)?.Join();

				View.Detach(this);

				Cleanup();

				AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
			}

			//****************************************

			void ITerminalListener.Clear() => _ConsoleOutput.Add(null);

			void ITerminalListener.Log(ConsoleRecord record) => _ConsoleOutput.Add(record ?? throw new ArgumentNullException(nameof(record)));

			//****************************************

			private void Cleanup()
			{
				if (!IsRedirected)
				{
					try
					{
						Console.ResetColor();
					}
					catch
					{
						// Fallback, just set it to what we saw initially
						Console.ForegroundColor = _InitialColour;
					}
				}
			}

			private void OnProcessExit(object sender, EventArgs e) => Cleanup();

			private void TerminalIOLoop()
			{ //****************************************
				var InputVisible = false;
				int CountSinceKeyPress = 0, CountSinceOutput = 0;
				//****************************************

				while (!_TokenSource.IsCancellationRequested || _ConsoleOutput.Count != 0)
				{
					while (_ConsoleOutput.TryTake(out var Record))
					{
						if (InputVisible)
						{
							// Clear the input line
							HideInputArea();

							InputVisible = false;
							CountSinceOutput = 0;
						}

						// Write the output
						if (Record == null)
						{
							Console.Clear();
						}
						else
						{
							// If we're echoing a command, ignore it if this is disabled
							if (Record.Highlight == TerminalHighlight.ConsoleCommand && !Theme.EchoCommands)
								continue;

							if (!IsRedirected)
								Console.ForegroundColor = Theme.GetColour(Record.Severity, Record.Highlight);

							if (Record.Highlight == TerminalHighlight.ConsoleCommand)
							{
								// We're echoing a command, put the prompt before it (no indentation)
								Console.WriteLine("{0}{1}", Theme.Prompt, Record.Text);
							}
							else
							{
								ConsoleWrite(Record.Text, Record.Indentation);

								if (Record.Exception != null)
									ConsoleWrite(Record.Exception.ToString(), Record.Indentation);
							}
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
							_ConsoleOutput.Peek(_TokenSource.Token).AsTask().GetAwaiter().GetResult();
						}
						catch (OperationCanceledException)
						{
						}
					}
				}

				if (InputVisible)
					// Clear the input line
					HideInputArea();
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

					_InputLine.Clear();
					_InputIndex = 0;

					// Attempt to parse and execute the command
					ThreadPool.QueueUserWorkItem((state) =>
					{
						var (View, CurrentLine, Token) = (Tuple<TerminalView, string, CancellationToken>)state;

						TerminalParser.Execute(View, CurrentLine, Token);
					}, Tuple.Create(View, CurrentLine, _TokenSource.Token));

					// Store the new line into the history, as long as it's not already the most recent entry
					if (_CommandHistory.Count == 0 || _CommandHistory[0] != CurrentLine)
						_CommandHistory.Insert(0, CurrentLine);

					return;
				}

				//****************************************

				if (keyData.Key != ConsoleKey.Tab)
					_PartialCommand = null;

				var BufferWidth = Console.BufferWidth;

				switch (keyData.Key)
				{
				case ConsoleKey.LeftArrow:
					if (_InputIndex > 0)
					{
						if (keyData.Modifiers.HasFlag(ConsoleModifiers.Control))
							_InputIndex = Math.Max(_InputLine.ToString().LastIndexOf(' ', Math.Max(_InputIndex - 1, 0)), 0);
						else
							_InputIndex--;
					}

					if (inputVisible)
						RepositionCursor(BufferWidth);
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

					if (inputVisible)
						RepositionCursor(BufferWidth);
					break;

				case ConsoleKey.Home:
					_InputIndex = 0;

					if (inputVisible)
						RepositionCursor(BufferWidth);
					break;

				case ConsoleKey.End:
					_InputIndex = _InputLine.Length;

					if (inputVisible)
						RepositionCursor(BufferWidth);
					break;

				case ConsoleKey.UpArrow:
					if (keyData.Modifiers.HasFlag(ConsoleModifiers.Control))
					{
						if (_InputIndex >= BufferWidth - 2)
							_InputIndex -= BufferWidth;
						else
							_InputIndex = 0;

						if (inputVisible)
							RepositionCursor(BufferWidth);
					}
					else if (_CommandHistoryIndex < _CommandHistory.Count - 1)
					{
						if (inputVisible)
						{
							HideInputArea();
							inputVisible = false;
						}

						_CommandHistoryIndex++;

						_InputLine.Length = 0;
						_InputLine.Append(_CommandHistory[_CommandHistoryIndex]);
						_InputIndex = _InputLine.Length;
					}
					break;

				case ConsoleKey.DownArrow:
					if (keyData.Modifiers.HasFlag(ConsoleModifiers.Control))
					{
						if (_InputIndex < BufferWidth - 2)
							_InputIndex = Math.Min(_InputIndex + BufferWidth, _InputLine.Length);
						else
							_InputIndex = _InputLine.Length;

						if (inputVisible)
							RepositionCursor(BufferWidth);
					}
					else if (_CommandHistoryIndex >= 0)
					{
						if (inputVisible)
						{
							HideInputArea();
							inputVisible = false;
						}

						_CommandHistoryIndex--;

						_InputLine.Length = 0;
						if (_CommandHistoryIndex != -1)
							_InputLine.Append(_CommandHistory[_CommandHistoryIndex]);

						_InputIndex = _InputLine.Length;
					}
					break;

				case ConsoleKey.Tab:
					if (_PartialCommand == null)
						_PartialCommand = _InputLine.ToString();

					var NewCommand = TerminalParser.FindNextCommand(_PartialCommand, _InputLine.ToString(), View.Registries);

					if (string.IsNullOrEmpty(NewCommand)) // No matching commands
						break;

					if (inputVisible)
					{
						HideInputArea();
						inputVisible = false;
					}

					_InputLine.Length = 0;
					_InputLine.Append(NewCommand);

					_InputIndex = _InputLine.Length;
					break;

				case ConsoleKey.Escape:
					if (inputVisible)
					{
						HideInputArea();
						inputVisible = false;
					}

					_InputLine.Length = 0;
					_InputIndex = 0;
					break;

				case ConsoleKey.Backspace:
					if (_InputIndex > 0)
					{
						// Remove the previous character at the input point
						_InputLine.Remove(_InputIndex - 1, 1);

						_InputIndex--;

						if (inputVisible)
						{
							// Clear the character at the input point
							RepositionCursor(BufferWidth);

							Console.Write(" ");

							RepositionCursor(BufferWidth);
						}
					}
					break;

				case ConsoleKey.Delete:
					if (_InputIndex < _InputLine.Length)
					{
						_InputLine.Remove(_InputIndex, 1);

						if (inputVisible)
						{
							// Rewrite the remainder of the line
							Console.Write(_InputLine.ToString(_InputIndex, _InputLine.Length - _InputIndex));
							Console.Write(" "); // Clear the last character

							RepositionCursor(BufferWidth);
						}
					}
					break;

				default:
					if (keyData.KeyChar == '\0')
						break;

					_InputLine.Insert(_InputIndex, keyData.KeyChar);
					_InputIndex++;

					if (inputVisible)
					{
						if (_InputIndex == _InputLine.Length)
						{
							// Write to the end of the line
							Console.Write(keyData.KeyChar);
						}
						else
						{
							// Replace the remaining line
							Console.Write(_InputLine.ToString(_InputIndex - 1, _InputLine.Length - _InputIndex + 1));

							RepositionCursor(BufferWidth);
						}
					}
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

			private void ConsoleWrite(string content, int indentation)
			{
				if (indentation == 0)
				{
					Console.WriteLine(content);

					return;
				}

				if (indentation == 1 && !content.Contains(Environment.NewLine))
				{
					Console.WriteLine("{0}{1}", Theme.Indentation, content);

					return;
				}

				var IndentText = Theme.Indentation;

				if (IndentText.Length == 1)
					_Output.Append(IndentText[0], indentation);
				else
				{
					for (var Index = 0; Index < indentation; Index++)
						_Output.Append(IndentText);
				}

				if (content.Contains(Environment.NewLine))
				{
					// Indent every line
					var StartLength = _Output.Length;

					foreach (var Segment in content.AsSpan().Split(Environment.NewLine.AsSpan()))
					{
						if (Segment.IsEmpty)
						{
							// Write an empty line
							Console.WriteLine();

							continue;
						}

						// Trim any previous line
						_Output.Length = StartLength;

						_Output.Append(Segment);

						Console.WriteLine(_Output.ToString());
					}
				}
				else
				{
					_Output.Append(content);

					Console.WriteLine(_Output.ToString());
				}

				_Output.Clear();
			}

			private void ShowInputArea()
			{
				var BufferWidth = Console.BufferWidth;

				Console.ForegroundColor = Theme.PromptColour;

				Console.Write(Theme.Prompt);
				Console.Write(_InputLine!.ToString());

				// This may have pushed the console down a line, so we need to calculate the start index afterwards
				_InputTop = Console.CursorTop - (_InputLine.Length + 2) / BufferWidth;

				RepositionCursor(BufferWidth);
			}

			private void RepositionCursor(int bufferWidth)
			{
				// Recalculate where the cursor location is
				var Row = Math.DivRem(_InputIndex + 2, bufferWidth, out var Column);
				Console.SetCursorPosition(Column, _InputTop + Row);
			}

			//****************************************

			public TerminalView View { get; }

			public TerminalTheme Theme { get; }

			public bool IsRedirected { get; }

			public bool HasCommandLine { get; }
		}
	}
}
