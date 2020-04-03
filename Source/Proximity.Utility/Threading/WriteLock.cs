using System;
using System.Threading;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Utility Structure to enable a Using block for <see cref="ReaderWriterLockSlim" />
	/// </summary>
	public readonly struct WriteLock : IDisposable
	{	//****************************************
		private readonly ReaderWriterLockSlim _WriteLock;
		//****************************************

		private WriteLock(ReaderWriterLockSlim writeLock) => _WriteLock = writeLock;

		//****************************************

		/// <summary>
		/// Open a Write Lock for the duration of the Using Block
		/// </summary>
		/// <param name="writeLock">The <see cref="ReaderWriterLockSlim" /> to lock</param>
		/// <returns>An IDisposable structure to pass to a Using block</returns>
		public static WriteLock From(ReaderWriterLockSlim writeLock)
		{
			writeLock.EnterWriteLock();

			return new WriteLock(writeLock);
		}

		//****************************************

		void IDisposable.Dispose() => _WriteLock.ExitWriteLock();
	}
}
