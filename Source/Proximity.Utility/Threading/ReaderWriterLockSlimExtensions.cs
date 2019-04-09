using System;
using System.Threading;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides <see cref="ReaderWriterLockSlim" /> extension methods
	/// </summary>
	public static class ReaderWriterLockSlimExtensions
	{
		/// <summary>
		/// Open a Read Lock for the duration of the Using Block
		/// </summary>
		/// <param name="readLock">The <see cref="ReaderWriterLockSlim" /> to lock</param>
		/// <returns>An IDisposable structure to pass to a Using block</returns>
		public static ReadLock UsingReader(this ReaderWriterLockSlim readLock) => ReadLock.From(readLock);

		/// <summary>
		/// Open a Write Lock for the duration of the Using Block
		/// </summary>
		/// <param name="writeLock">The <see cref="ReaderWriterLockSlim" /> to lock</param>
		/// <returns>An IDisposable structure to pass to a Using block</returns>
		public static WriteLock UsingWriter(this ReaderWriterLockSlim writeLock) => WriteLock.From(writeLock);
	}
}
