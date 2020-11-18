using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Buffers
{
	internal sealed class DummyPool<T> : ArrayPool<T>
	{
		internal static new readonly DummyPool<T> Shared = new DummyPool<T>();

		//****************************************

		private DummyPool()
		{
		}

		//****************************************

		public override T[] Rent(int minimumLength) => new T[Math.Max(minimumLength, 1024)];

		public override void Return(T[] array, bool clearArray = false)
		{
		}
	}
}
