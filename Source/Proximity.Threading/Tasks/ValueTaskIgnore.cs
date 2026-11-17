using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

internal sealed class ValueTaskIgnore
{ //****************************************
	private static readonly ConcurrentBag<ValueTaskIgnore> Instances = new();
	//****************************************
	private readonly Action _Continue;

	private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _Awaiter;
	//****************************************

	internal ValueTaskIgnore() => _Continue = OnContinue;

	//****************************************

	internal void Attach(ValueTask task)
	{
		var Awaiter = task.ConfigureAwait(false).GetAwaiter();

		if (Awaiter.IsCompleted)
		{
			Swallow(Awaiter);

			Instances.Add(this);
		}
		else
			_Awaiter.OnCompleted(_Continue);
	}

	//****************************************

	private void OnContinue()
	{
		Swallow(_Awaiter);

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

	private static void Swallow(ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter)
	{
		try
		{
			awaiter.GetResult();
		}
		catch // Swallow exceptions, we're ignoring the result of this ValueTask
		{
		}
	}
}

internal sealed class ValueTaskIgnore<T>
{ //****************************************
	private static readonly ConcurrentBag<ValueTaskIgnore<T>> Instances = new();
	//****************************************
	private readonly Action _Continue;

	private ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter _Awaiter;
	//****************************************

	internal ValueTaskIgnore() => _Continue = OnContinue;

	//****************************************

	internal void Attach(ValueTask<T> task)
	{
		var Awaiter = task.ConfigureAwait(false).GetAwaiter();

		if (Awaiter.IsCompleted)
		{
			Swallow(Awaiter);

			Instances.Add(this);
		}
		else
			_Awaiter.OnCompleted(_Continue);
	}

	//****************************************

	private void OnContinue()
	{
		Swallow(_Awaiter);

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

	private static void Swallow(ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter awaiter)
	{
		try
		{
			awaiter.GetResult();
		}
		catch // Swallow exceptions, we're ignoring the result of this ValueTask
		{
		}
	}
}
