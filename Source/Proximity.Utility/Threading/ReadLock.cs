using System;
using System.Threading;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Utility Structure to enable a Using block for <see cref="ReaderWriterLockSlim" />
	/// </summary>
	public readonly struct ReadLock : IDisposable
	{	//****************************************
		private readonly ReaderWriterLockSlim _ReadLock;
		//****************************************

		private ReadLock(ReaderWriterLockSlim readLock) => _ReadLock = readLock;

		//****************************************

		/// <summary>
		/// Open a Read Lock for the duration of the Using Block
		/// </summary>
		/// <param name="readLock">The <see cref="ReaderWriterLockSlim" /> to lock</param>
		/// <returns>An IDisposable structure to pass to a Using block</returns>
		public static ReadLock From(ReaderWriterLockSlim readLock)
		{
			readLock.EnterReadLock();

			return new ReadLock(readLock);
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

		void IDisposable.Dispose() => _ReadLock.ExitReadLock();
	}
}
