using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Interleave
{
	internal interface IInterleaveAsyncEnumerator
	{
		void PrepareCurrent();

		void Cancel();
	}

	internal interface IInterleaveAsyncEnumerator<T> : IInterleaveAsyncEnumerator
	{
		void CompleteTask(T task, int index);
	}
}
