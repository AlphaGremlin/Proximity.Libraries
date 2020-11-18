using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Proximity.Terminal
{
	internal sealed class TerminalScopeProvider : IExternalScopeProvider
	{
		private readonly AsyncLocal<Scope?> _CurrentScope;

		/// <inheritdoc />
		public void ForEachScope<TState>(Action<object, TState> callback, TState state)
		{
			void Report(Scope current)
			{
				if (current.Parent != null)
					Report(current.Parent);

				if (current.StateObject != null)
					callback(current.StateObject, state);
			}

			if (_CurrentScope.Value != null)
				Report(_CurrentScope.Value);
		}

		/// <inheritdoc />
		public void ForEachScope<T, TState>(Action<T, TState> callback, TState state)
		{
			void Report(Scope current)
			{
				if (current.Parent != null)
					Report(current.Parent);

				if (current is Scope<T> TypedScope)
					callback(TypedScope.State, state);
				else if (current is ObjectScope ObjectScope && ObjectScope.StateObject is T TypedObject)
					callback(TypedObject, state);
			}

			if (_CurrentScope.Value != null)
				Report(_CurrentScope.Value);
		}

		public IDisposable Push(object state)
		{
			var Parent = _CurrentScope.Value;

			var NewScope = new ObjectScope(this, Parent, state);

			_CurrentScope.Value = NewScope;

			return NewScope;
		}

		/// <inheritdoc />
		public IDisposable Push<T>(T state)
		{
			var Parent = _CurrentScope.Value;

			var NewScope = new Scope<T>(this, Parent, state);

			_CurrentScope.Value = NewScope;

			return NewScope;
		}

		private abstract class Scope : IDisposable
		{
			private readonly TerminalScopeProvider _Provider;
			private bool _IsDisposed;

			internal Scope(TerminalScopeProvider provider, Scope? parent)
			{
				_Provider = provider;
				Parent = parent;
			}

			public void Dispose()
			{
				if (!_IsDisposed)
				{
					_Provider._CurrentScope.Value = Parent;
					_IsDisposed = true;
				}
			}

			public abstract object StateObject { get; }

			public Scope? Parent { get; }
		}

		private sealed class ObjectScope : Scope
		{
			public ObjectScope(TerminalScopeProvider provider, Scope? parent, object state) : base(provider, parent)
			{
				StateObject = state;
			}

			public override string ToString() => StateObject.ToString() ?? "";

			public override object StateObject { get; }
		}

		private sealed class Scope<T> : Scope
		{
			public Scope(TerminalScopeProvider provider, Scope? parent, T state) : base(provider, parent)
			{
				State = state;
			}

			public override string ToString() => State.ToString() ?? "";

			public override object StateObject => State;

			public T State { get; }
		}
	}
}
