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
		private readonly Func<LogEvent, Exception?, string> _MessageFormatter;

		private readonly ITextFormatter _Formatter;
		//****************************************

		internal TerminalSink(ITerminal terminal, ITextFormatter formatter)
		{
			Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
			_Formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
			_MessageFormatter = MessageFormatter;
		}

		//****************************************

		void ILogEventSink.Emit(LogEvent logEvent)
		{
			// Restore any Logging or Terminal properties passed through Serilog
			EventId LogEventId = 0;
			TerminalHighlight? Highlight = null;
			var Indent = 0;

			if (logEvent.Properties.TryGetValue("EventId", out var EventIdProperty) && EventIdProperty is StructureValue StructuredEventId)
			{
				string? Name = null;
				int? ID = null;

				foreach (var Property in StructuredEventId.Properties)
				{
					if (Property.Value is not ScalarValue ScalarProperty)
						continue;

					switch (Property.Name)
					{
					case "Name":
						Name = (string?)ScalarProperty.Value;
						break;

					case "Id":
						ID = (int?)ScalarProperty.Value;
						break;
					}
				}

				LogEventId = new EventId(ID ?? 0, Name);
			}

			if (logEvent.Properties.TryGetValue(TerminalHighlight.ScopeProperty, out var HighlightProperty) && HighlightProperty is ScalarValue ScalarHighlight)
			{
				if (ScalarHighlight.Value is string HighlightName)
					TerminalHighlight.FromName(HighlightName, out Highlight);
			}

			// Serilog properties override based on the highest scope, so the indent value saved will be the innermost one
			if (logEvent.Properties.TryGetValue(TerminalIndent.ScopeProperty, out var IndentProperty) && IndentProperty is ScalarValue ScalarIndent)
			{
				if (ScalarIndent.Value is int IndentValue)
					Indent = IndentValue;
			}

			IDisposable? HighlightDisposable = null, IndentDisposable = null;

			// Log the entry
			try
			{
				if (Highlight != null)
					HighlightDisposable = Terminal.BeginScope(Highlight);

				if (Indent > 0)
					IndentDisposable = Terminal.BeginScope(TerminalIndent.Replace(Indent));

				Terminal.Log(Translate(logEvent.Level), LogEventId, logEvent, logEvent.Exception, _MessageFormatter);
			}
			finally
			{
				IndentDisposable?.Dispose();
				HighlightDisposable?.Dispose();
			}
		}

		private string MessageFormatter(LogEvent logEvent, Exception? exception)
		{
			using var Target = EmitterTarget.GetOrCreate();

			_Formatter.Format(logEvent, Target.Writer);

			Target.Writer.Flush();

			// Console automatically appends a newline, so ensure we don't write one
			var NewLineSpan = Environment.NewLine.AsSpan();
			var Builder = Target.Buffer.Builder;

			if (Builder.Length >= NewLineSpan.Length)
			{
				Span<char> TempSpan = stackalloc char[NewLineSpan.Length];

				var Offset = Builder.Length - TempSpan.Length;

				for (var Index = 0; Index < TempSpan.Length; Index++)
					TempSpan[Index] = Builder[Offset + Index];

				if (NewLineSpan.Equals(TempSpan, StringComparison.Ordinal))
					Builder.Remove(Offset, NewLineSpan.Length);
			}

			return Target.Buffer.ToString();
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

		private readonly struct EmitterTarget : IDisposable
		{ //****************************************
			private static readonly ConcurrentBag<EmitterTarget> Instances = new();
			//****************************************

			private EmitterTarget(StringBuilderWriter buffer, CharTextWriter writer)
			{
				Buffer = buffer;
				Writer = writer;
			}

			//****************************************

			public void Dispose()
			{
				Writer.Flush();
				Buffer.Reset();

				Instances.Add(this);
			}

			//****************************************

			public StringBuilderWriter Buffer { get; }

			public CharTextWriter Writer { get; }

			//****************************************

			internal static EmitterTarget GetOrCreate()
			{
				if (!Instances.TryTake(out var Target))
				{
					var Buffer = new StringBuilderWriter();

					Target = new EmitterTarget(Buffer, new CharTextWriter(Buffer));
				}

				return Target;
			}
		}
	}
}
