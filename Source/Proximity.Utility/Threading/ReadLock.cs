/****************************************\
 ReadLock.cs
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
	public struct ReadLock : IDisposable
	{	//****************************************
		private ReaderWriterLockSlim _ReadLock;
		//****************************************
		
		/// <summary>
		/// Open a Read Lock for the duration of the Using Block
		/// </summary>
		/// <param name="readLock">The <see cref="ReaderWriterLockSlim" /> to lock</param>
		/// <returns>An IDisposable structure to pass to a Using block</returns>
		public static ReadLock From(ReaderWriterLockSlim readLock)
		{	//****************************************
			ReadLock MyReader;
			//****************************************
			
			MyReader._ReadLock = readLock;
			
			readLock.EnterReadLock();
			
			//****************************************
			
			return MyReader;
		}

		//****************************************
		
		/// <summary>
		/// If there are any writers waiting, releases the lock and blocks until the writers are complete
		/// </summary>
		public void YieldForWriters()
		{
			// Technically only a debugging property, but we only need it to skip the unnecessary exit/enter
			if (_ReadLock.WaitingWriteCount == 0)
				return;
			
			_ReadLock.ExitReadLock();
			
			// Will block if there are writers, until the writing is all done
			_ReadLock.EnterReadLock();
		}
		
		//****************************************
		
		void IDisposable.Dispose()
		{
			_ReadLock.ExitReadLock();
		}
	}
}
