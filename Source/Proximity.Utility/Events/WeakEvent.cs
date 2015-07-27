﻿/****************************************\
 WeakEvent.cs
 Created: 2010-07-28
\****************************************/
#if !MOBILE && !PORTABLE
using System;
//****************************************

namespace Proximity.Utility.Events
{
	/// <summary>
	/// Provides weakly bound, fast and type-safe events
	/// </summary>
	/// <remarks>This class is threadsafe, however handlers have the potential to be called a short time after removal</remarks>
	public sealed class WeakEvent<TEventArgs> where TEventArgs : EventArgs
	{	//****************************************
		private event EventHandler<TEventArgs> _WeakEventHandler;
		private Action<EventHandler<TEventArgs>> _UnregisterCallback;
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
			WeakEventDelegate.Cleanup(_WeakEventHandler, _UnregisterCallback);
			
			_WeakEventHandler += WeakEventDelegate.Create(eventHandler);
		}
		
		/// <summary>
		/// Removes a Weak Event binding
		/// </summary>
		/// <param name="eventHandler">The event handler we not longer wish to call</param>
		public void Remove(EventHandler<TEventArgs> eventHandler)
		{
			_WeakEventHandler -= WeakEventDelegate.FindHandler(_WeakEventHandler, eventHandler);
		}
		
		/// <summary>
		/// Invokes the Weak Event Handler
		/// </summary>
		/// <param name="sender">The sending object</param>
		/// <param name="e">The event arguments</param>
		public void Invoke(object sender, TEventArgs e)
		{	//****************************************
			var MyHandler = _WeakEventHandler;
			//****************************************
			
			if (MyHandler != null)
				MyHandler(sender, e);
		}
		
		//****************************************
		
		private void OnUnregister(EventHandler<TEventArgs> eventHandler)
		{
			_WeakEventHandler -= eventHandler;
		}
	}
}
#endif