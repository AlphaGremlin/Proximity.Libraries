/****************************************\
 ParallelEvent.cs
 Created: 2011-09-16
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Events
{
	/// <summary>
	/// Provides the functionality for raising a multiple delegate asynchronously and waiting until the threads complete
	/// </summary>
	public static class ParallelEvent
	{
		/// <summary>
		/// Raises each delegate within a multicast delegate asynchronously
		/// </summary>
		/// <param name="eventHandler">The multicast delegate to target</param>
		/// <param name="sender">The sender parameter to pass</param>
		/// <param name="e">The event arguments to pass</param>
		/// <remarks>Each delegate will be raised on the ThreadPool, and this method will wait until they all complete</remarks>
		public static void Raise<TEventArgs>(EventHandler<TEventArgs> eventHandler, object sender, TEventArgs e) where TEventArgs : EventArgs
		{	//****************************************
			Delegate[] MyReceivers;
			//****************************************
			
			if (eventHandler == null)
				return;
			
			MyReceivers = eventHandler.GetInvocationList();
			
			// Don't bother doing async if there's only one receiver
			if (MyReceivers.Length == 1)
			{
				eventHandler(sender, e);
				
				return;
			}
			
			//****************************************
			
			Parallel.ForEach(MyReceivers, new RaiseInstance<TEventArgs>(sender, e).Raise);
		}
		
		/// <summary>
		/// Raises each delegate within multiple multicast delegates asynchronously
		/// </summary>
		/// <param name="sender">The sender parameter to pass</param>
		/// <param name="e">The event arguments to pass</param>
		/// <param name="eventHandlers">An array of multicast delegates to target</param>
		/// <remarks>
		/// <para>Each delegate will be raised on the ThreadPool, and this method will wait until they all complete</para>
		/// </remarks>
		public static void RaiseAll<TEventArgs>(object sender, TEventArgs e, params EventHandler<TEventArgs>[] eventHandlers) where TEventArgs : EventArgs
		{
			Parallel.ForEach(eventHandlers, new RaiseInstance<TEventArgs>(sender, e).RaiseAll);
		}
		
		/// <summary>
		/// Raises each delegate within a multicast delegate asynchronously
		/// </summary>
		/// <param name="eventHandler">The multicast delegate to target</param>
		/// <param name="sender">The sender parameter to pass</param>
		/// <param name="e">The event arguments to pass</param>
		/// <remarks>Each delegate will be raised on the ThreadPool, and this method will wait until they all complete</remarks>
		public static void RaiseAll<TEventArgs>(this EventHandler<TEventArgs> eventHandler, object sender, TEventArgs e) where TEventArgs : EventArgs
		{
			Raise(eventHandler, sender, e);
		}
		
		//****************************************
		
		private class RaiseInstance<TEventArgs> where TEventArgs : EventArgs
		{	//****************************************
			private readonly object _Sender;
			private readonly TEventArgs _Args;
			//****************************************
			
			public RaiseInstance(object sender, TEventArgs args)
			{
				_Sender = sender;
				_Args = args;
			}
			
			//****************************************
			
			public void Raise(Delegate handler)
			{
				((EventHandler<TEventArgs>)handler)(_Sender, _Args);
			}
			
			public void RaiseAll(EventHandler<TEventArgs> handler)
			{
				handler(_Sender, _Args);
			}
		}
	}
}
#endif