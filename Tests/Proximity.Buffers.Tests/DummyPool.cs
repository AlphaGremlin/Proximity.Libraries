using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Buffers.Tests
{
	internal sealed class ExactPool<T> : ArrayPool<T>
	{
		internal static new readonly ExactPool<T> Shared = new ExactPool<T>();

		//****************************************

		private ExactPool()
		{
		}

		//****************************************

		public override T[] Rent(int minimumLength) => new T[minimumLength];

		public override void Return(T[] array, bool clearArray = false)
		{
		}
	}
}
