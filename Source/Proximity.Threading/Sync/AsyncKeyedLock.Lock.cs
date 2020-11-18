using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace System.Threading
{
	public sealed partial class AsyncKeyedLock<TKey>
	{
		internal sealed class AsyncKeyedLockInstance
		{ //****************************************
			private readonly AsyncKeyedLock<TKey> _Source;
			private readonly TKey _Key;
			private int _Released;
			//****************************************

			internal AsyncKeyedLockInstance(AsyncKeyedLock<TKey> source, TKey key)
			{
				_Source = source;
				_Key = key;
			}

			//****************************************

			public void Dispose()
			{
				if (_Source != null && Interlocked.Exchange(ref _Released, 1) == 0)
				{
					_Source.Release(_Key);
				}
			}
		}
	}
}
