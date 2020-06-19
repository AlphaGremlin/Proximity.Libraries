using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Collections.Concurrent
{
	public sealed partial class AsyncCollection<TItem>
	{
		private abstract class CollectionInstance
		{ //****************************************
			private ManualResetValueTaskSourceCore<TItem> _TaskSource = new ManualResetValueTaskSourceCore<TItem>();
			//****************************************

			internal CollectionInstance() => _TaskSource.RunContinuationsAsynchronously = true;

			//****************************************

			protected void Initialise(AsyncCollection<TItem> owner)
			{
				Owner = owner;
			}

			protected void SwitchToFailed(Exception e) => _TaskSource.SetException(e);

			protected void SwitchToCompleted(TItem item) => _TaskSource.SetResult(item);

			protected TItem GetResult(short token)
			{
				try
				{
					return _TaskSource.GetResult(token);
				}
				finally
				{
					_TaskSource.Reset();

					Release();
				}
			}

			protected abstract void Release();

			//****************************************

			public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			public ValueTaskSourceStatus GetStatus(short token) => _TaskSource.GetStatus(token);

			//****************************************

			public AsyncCollection<TItem>? Owner { get; private set; }

			public short Version => _TaskSource.Version;
		}

		private sealed class CollectionAddInstance : CollectionInstance, IValueTaskSource
		{ //****************************************
			private static readonly ConcurrentBag<CollectionAddInstance> Instances = new ConcurrentBag<CollectionAddInstance>();
			//****************************************
			private readonly Action _OnTookSlot;

			private TItem _Item = default!;
			private bool _Complete;
			private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _Awaiter;
			//****************************************

			internal CollectionAddInstance() => _OnTookSlot = OnTookSlot;

			//****************************************

			internal void Initialise(AsyncCollection<TItem> owner, TItem item, ValueTask waitForSlot, bool complete)
			{
				Initialise(owner);

				_Item = item;
				_Complete = complete;
				_Awaiter = waitForSlot.ConfigureAwait(false).GetAwaiter();

				if (_Awaiter.IsCompleted)
					OnTookSlot();
				else
					_Awaiter.OnCompleted(_OnTookSlot);
			}

			//****************************************

			void IValueTaskSource.GetResult(short token) => GetResult(token);

			protected override void Release()
			{
				_Item = default!;
				_Complete = false;
				_Awaiter = default;

				Instances.Add(this);
			}

			//****************************************

			private void OnTookSlot()
			{
				try
				{
					_Awaiter.GetResult();

					Owner!.InternalAdd(_Item, _Complete);

					SwitchToCompleted(default!);
				}
				catch (ObjectDisposedException)
				{
					SwitchToFailed(new InvalidOperationException("Adding was completed while waiting for a slot"));
				}
				catch (Exception e)
				{
					SwitchToFailed(e);
				}
			}

			//****************************************

			internal static CollectionAddInstance GetOrCreateFor(AsyncCollection<TItem> owner, TItem item, ValueTask waitForSlot, bool complete)
			{
				if (!Instances.TryTake(out var Instance))
					Instance = new CollectionAddInstance();

				Instance.Initialise(owner, item, waitForSlot, complete);

				return Instance;
			}
		}

		private sealed class CollectionTakeInstance : CollectionInstance, IValueTaskSource<TItem>
		{ //****************************************
			private static readonly ConcurrentBag<CollectionTakeInstance> Instances = new ConcurrentBag<CollectionTakeInstance>();
			//****************************************
			private readonly Action _OnTookItem;

			private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _Awaiter;
			//****************************************

			internal CollectionTakeInstance() => _OnTookItem = OnTookItem;

			//****************************************

			internal void Initialise(AsyncCollection<TItem> owner, ValueTask waitForItem)
			{
				Initialise(owner);

				_Awaiter = waitForItem.ConfigureAwait(false).GetAwaiter();

				if (_Awaiter.IsCompleted)
					OnTookItem();
				else
					_Awaiter.OnCompleted(_OnTookItem);
			}

			//****************************************

			TItem IValueTaskSource<TItem>.GetResult(short token) => GetResult(token);

			protected override void Release()
			{
				_Awaiter = default;

				Instances.Add(this);
			}

			//****************************************

			private void OnTookItem()
			{
				try
				{
					_Awaiter.GetResult();

					SwitchToCompleted(Owner!.CompleteTake());
				}
				catch (ObjectDisposedException)
				{
					SwitchToFailed(new InvalidOperationException("Adding was completed while waiting for a slot"));
				}
				catch (Exception e)
				{
					SwitchToFailed(e);
				}
			}

			//****************************************

			internal static CollectionTakeInstance GetOrCreateFor(AsyncCollection<TItem> owner, ValueTask waitForSlot)
			{
				if (!Instances.TryTake(out var Instance))
					Instance = new CollectionTakeInstance();

				Instance.Initialise(owner, waitForSlot);

				return Instance;
			}
		}
	}
}
