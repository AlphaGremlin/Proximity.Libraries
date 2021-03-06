using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
//****************************************

namespace Proximity.Collections
{
	/// <summary>
	/// Provides a WeakReference-style self-cleanup class that also accepts a GCHandleType
	/// </summary>
	internal sealed class GCReference : IDisposable
	{	//****************************************
		private GCHandle _Handle;
		private int _IsDisposed;
		//****************************************

		/// <summary>
		/// Creates a new Weak GC Reference
		/// </summary>
		/// <param name="target">The target object</param>
		public GCReference(object target)
		{
			_Handle = GCHandle.Alloc(target, GCHandleType.Weak);
		}

		/// <summary>
		/// Creates a new GC Reference
		/// </summary>
		/// <param name="target">The target object</param>
		/// <param name="handleType">The type of GC Handle</param>
		public GCReference(object target, GCHandleType handleType)
		{
			_Handle = GCHandle.Alloc(target, handleType);
		}

		//****************************************

		/// <summary>
		/// Finalise the GC Reference, disposing of the GCHandle if it has not been disposed of
		/// </summary>
		~GCReference()
		{
			if (Interlocked.Exchange(ref _IsDisposed, 1) == 0)
				_Handle.Free();
		}

		//****************************************

		/// <summary>
		/// Disposes of the GC Reference in a deterministic manner
		/// </summary>
		public void Dispose()
		{
			if (Interlocked.Exchange(ref _IsDisposed, 1) == 0)
				_Handle.Free();

			GC.SuppressFinalize(this);
		}

		//****************************************

		/// <summary>
		/// Gets the target object
		/// </summary>
		public object? Target
		{
			get => _Handle.Target;
		}

		/// <summary>
		/// Gets whether the target object is alive or has been garbage collected
		/// </summary>
		public bool IsAlive
		{
			get => _Handle.IsAllocated && _Handle.Target != null;
		}
	}
}
