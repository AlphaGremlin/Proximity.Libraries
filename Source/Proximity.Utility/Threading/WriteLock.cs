/****************************************\
 WriteLock.cs
 Created: 2011-08-02
\****************************************/
using System;
using System.Threading;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Utility Structure to enable a Using block for <see cref="ReaderWriterLockSlim" />
	/// </summary>
	public struct WriteLock : IDisposable
	{	//****************************************
		private ReaderWriterLockSlim _WriteLock;
		//****************************************
		
		/// <summary>
		/// Open a Write Lock for the duration of the Using Block
		/// </summary>
		/// <param name="writeLock">The <see cref="ReaderWriterLockSlim" /> to lock</param>
		/// <returns>An IDisposable structure to pass to a Using block</returns>
		public static WriteLock From(ReaderWriterLockSlim writeLock)
		{	//****************************************
			WriteLock MyWriter;
			//****************************************
			
			MyWriter._WriteLock = writeLock;
			
			writeLock.EnterWriteLock();
			
			//****************************************
			
			return MyWriter;
		}
		
		//****************************************
		
		void IDisposable.Dispose()
		{
			_WriteLock.ExitWriteLock();
		}
	}
}
