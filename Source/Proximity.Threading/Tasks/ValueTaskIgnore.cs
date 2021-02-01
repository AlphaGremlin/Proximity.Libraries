using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Threading.Tasks
{
	internal sealed class ValueTaskIgnore
	{ //****************************************
		private static readonly ConcurrentBag<ValueTaskIgnore> Instances = new ConcurrentBag<ValueTaskIgnore>();
		//****************************************
		private readonly Action _Continue;

		private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _Awaiter;
		//****************************************

		internal ValueTaskIgnore() => _Continue = OnContinue;

		//****************************************

		internal void Attach(ValueTask task)
		{
			_Awaiter = task.ConfigureAwait(false).GetAwaiter();

			if (_Awaiter.IsCompleted)
				OnContinue();
			else
				_Awaiter.OnCompleted(_Continue);
		}

		//****************************************

		private void OnContinue()
		{
			try
			{
				_Awaiter.GetResult();
			}
			catch // Swallow exceptions, we're ignoring the result of this ValueTask
			{
			}

			// Return to the object pool
			_Awaiter = default;
			Instances.Add(this);
		}

		//****************************************

		internal static void Ignore(ValueTask task)
		{
			if (!Instances.TryTake(out var NewInstance))
				NewInstance = new ValueTaskIgnore();

			NewInstance.Attach(task);
		}
	}

	internal sealed class ValueTaskIgnore<T>
	{ //****************************************
		private static readonly ConcurrentBag<ValueTaskIgnore<T>> Instances = new ConcurrentBag<ValueTaskIgnore<T>>();
		//****************************************
		private readonly Action _Continue;

		private ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter _Awaiter;
		//****************************************

		internal ValueTaskIgnore() => _Continue = OnContinue;

		//****************************************

		internal void Attach(ValueTask<T> task)
		{
			_Awaiter = task.ConfigureAwait(false).GetAwaiter();

			if (_Awaiter.IsCompleted)
				OnContinue();
			else
				_Awaiter.OnCompleted(_Continue);
		}

		//****************************************

		private void OnContinue()
		{
			try
			{
				_Awaiter.GetResult();
			}
			catch // Swallow exceptions, we're ignoring the result of this ValueTask
			{
			}

			// Return to the object pool
			_Awaiter = default;
			Instances.Add(this);
		}

		//****************************************

		internal static void Ignore(ValueTask<T> task)
		{
			if (!Instances.TryTake(out var NewInstance))
				NewInstance = new ValueTaskIgnore<T>();

			NewInstance.Attach(task);
		}
	}
}
