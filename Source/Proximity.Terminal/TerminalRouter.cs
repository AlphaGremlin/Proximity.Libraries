using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Proximity.Terminal
{
	/// <summary>
	/// Manages a list of terminal listeners
	/// </summary>
	public class TerminalRouter
	{
		private ImmutableHashSet<ITerminalListener> _Listeners = ImmutableHashSet<ITerminalListener>.Empty;

		//****************************************

		/// <summary>
		/// Attach a listener to this Terminal
		/// </summary>
		/// <param name="listener">The listener that will receive events from this Terminal</param>
		/// <remarks>This keeps a strong reference to the listener, so care must be taken to detach</remarks>
		public void Attach(ITerminalListener listener) => ImmutableInterlockedEx.Add(ref _Listeners, listener);

		/// <summary>
		/// Removes a listener from this Terminal
		/// </summary>
		/// <param name="listener">The listener to remove</param>
		/// <returns>True if the listener was found and removed</returns>
		public bool Detach(ITerminalListener listener) => ImmutableInterlockedEx.Remove(ref _Listeners, listener);

		//****************************************

		/// <summary>
		/// Gets the listeners attached to this router
		/// </summary>
		public IReadOnlyCollection<ITerminalListener> Listeners => _Listeners;
	}
}
