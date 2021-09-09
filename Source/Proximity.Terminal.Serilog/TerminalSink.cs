using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Proximity.Terminal.Serilog
{
	/// <summary>
	/// Emits Serilog events to a Terminal
	/// </summary>
	internal sealed class TerminalSink : ILogEventSink
	{ //****************************************
		private readonly ITextFormatter _Formatter;
		//****************************************

		internal TerminalSink(ITerminal terminal, ITextFormatter formatter)
		{
			Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
			_Formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
		}

		//****************************************

		void ILogEventSink.Emit(LogEvent logEvent)
		{
			var Target = EmitterTarget.GetOrCreate();

			try
			{
				_Formatter.Format(logEvent, Target.Writer);

				Target.Writer.Flush();

				var Message = Target.Buffer.ToSequence();

				// Console automatically appends a newline, so ensure we don't write one
				if (Message.EndsWith(Environment.NewLine.AsSpan(), StringComparison.Ordinal))
					Message = Message.Slice(0, Message.Length - Environment.NewLine.Length);

				Terminal.Log(Translate(logEvent.Level), logEvent.Exception, Message.AsString(), Array.Empty<object>());
			}
			finally
			{
				Target.Release();
			}
		}

		//****************************************

		public ITerminal Terminal { get; }

		//****************************************

		private static LogLevel Translate(LogEventLevel level)
		{
			return level switch
			{
				LogEventLevel.Debug => LogLevel.Debug,
				LogEventLevel.Error => LogLevel.Error,
				LogEventLevel.Fatal => LogLevel.Critical,
				LogEventLevel.Information => LogLevel.Information,
				LogEventLevel.Verbose => LogLevel.Trace,
				LogEventLevel.Warning => LogLevel.Warning,
				_ => LogLevel.None
			};
		}

		//****************************************

		private readonly struct EmitterTarget
		{ //****************************************
			private static readonly ConcurrentBag<EmitterTarget> Instances = new();
			//****************************************

			private EmitterTarget(BufferWriter<char> buffer, CharTextWriter writer)
			{
				Buffer = buffer;
				Writer = writer;
			}

			//****************************************

			public void Release()
			{
				Writer.Flush();
				Buffer.Reset();

				Instances.Add(this);
			}

			//****************************************

			public BufferWriter<char> Buffer { get; }

			public CharTextWriter Writer { get; }

			//****************************************

			internal static EmitterTarget GetOrCreate()
			{
				if (!Instances.TryTake(out var Target))
				{
					var Buffer = new BufferWriter<char>();

					Target = new EmitterTarget(Buffer, new CharTextWriter(Buffer));
				}

				return Target;
			}
		}
	}
}
