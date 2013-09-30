/****************************************\
 WeakEvent.cs
 Created: 2010-07-28
\****************************************/
using System;
//****************************************

namespace Proximity.Utility.Events
{
	/// <summary>
	/// Provides weakly bound, fast and type-safe events
	/// </summary>
	public sealed class WeakEvent<TEventArgs> where TEventArgs : EventArgs
	{	//****************************************
		private EventHandler<TEventArgs> _WeakEventHandler;
		private UnregisterCallback<TEventArgs> _UnregisterCallback;
		//****************************************
		
		/// <summary>
		/// Creates a new Weak Event manager
		/// </summary>
		public WeakEvent()
		{
			_UnregisterCallback = OnUnregister;
		}
		
		//****************************************
		
		/// <summary>
		/// Adds a new Weak Event binding
		/// </summary>
		/// <param name="eventHandler">The event handler to call weakly</param>
		public void Add(EventHandler<TEventArgs> eventHandler)
		{
			//_WeakEventHandler += new WeakDelegate<TEventArgs>(eventHandler, _UnregisterCallback).GetDelegate();
			_WeakEventHandler += WeakEventDelegate.Create(eventHandler, _UnregisterCallback);
		}
		
		/// <summary>
		/// Removes a Weak Event binding
		/// </summary>
		/// <param name="eventHandler">The event handler we not longer wish to call</param>
		public void Remove(EventHandler<TEventArgs> eventHandler)
		{	//****************************************
			 EventHandler<TEventArgs> MyWeakHandler = WeakEventDelegate.FindHandler(_WeakEventHandler, eventHandler);
			 //****************************************
			 
			 if (MyWeakHandler != null)
				 _WeakEventHandler -= MyWeakHandler;
		}
		
		/// <summary>
		/// Invokes the Weak Event Handler
		/// </summary>
		/// <param name="sender">The sending object</param>
		/// <param name="e">The event arguments</param>
		public void Invoke(object sender, TEventArgs e)
		{
			if (_WeakEventHandler != null)
				_WeakEventHandler(sender, e);
		}
		
		//****************************************
		
		private void OnUnregister(EventHandler<TEventArgs> eventHandler)
		{
			_WeakEventHandler -= eventHandler;
		}
	}
}
