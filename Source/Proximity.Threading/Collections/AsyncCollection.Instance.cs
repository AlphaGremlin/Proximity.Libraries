using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Proximity.Threading;

namespace System.Collections.Concurrent
{
	public sealed partial class AsyncCollection<T>
	{
		private sealed class CollectionAddInstance : IValueTaskSource<bool>
		{ //****************************************
			private static readonly ConcurrentBag<CollectionAddInstance> Instances = new ConcurrentBag<CollectionAddInstance>();
			//**************************************** //****************************************
			private ManualResetValueTaskSourceCore<bool> _TaskSource = new ManualResetValueTaskSourceCore<bool>();

			private readonly Action _OnTookSlot;

			private T _Item = default!;
			private bool _Complete;
			private ConfiguredValueTaskAwaitable<bool>.ConfiguredValueTaskAwaiter _Awaiter;
			//****************************************

			internal CollectionAddInstance()
			{
				_TaskSource.RunContinuationsAsynchronously = true;
				_OnTookSlot = OnTookSlot;
			}

			//****************************************

			internal void Initialise(AsyncCollection<T> owner, T item, ValueTask<bool> waitForSlot, bool complete)
			{
				Owner = owner;

				_Item = item;
				_Complete = complete;
				_Awaiter = waitForSlot.ConfigureAwait(false).GetAwaiter();

				if (_Awaiter.IsCompleted)
					OnTookSlot();
				else
					_Awaiter.OnCompleted(_OnTookSlot);
			}

			//****************************************

			bool IValueTaskSource<bool>.GetResult(short token)
			{
				try
				{
					return _TaskSource.GetResult(token);
				}
				finally
				{
					Release();
				}
			}

			public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			public ValueTaskSourceStatus GetStatus(short token) => _TaskSource.GetStatus(token);

			//****************************************

			private void OnTookSlot()
			{
				try
				{
					var Result = _Awaiter.GetResult();

					if (Result)
						Result = Owner!.CompleteAdd(_Item, _Complete);

					_TaskSource.SetResult(Result);
				}
				catch (Exception e)
				{
					_TaskSource.SetException(e);
				}
			}

			private void Release()
			{
				_TaskSource.Reset();
				_Item = default!;
				_Complete = false;
				_Awaiter = default;

				Instances.Add(this);
			}

			//****************************************

			public AsyncCollection<T>? Owner { get; private set; }

			public short Version => _TaskSource.Version;

			//****************************************

			internal static CollectionAddInstance GetOrCreateFor(AsyncCollection<T> owner, T item, ValueTask<bool> waitForSlot, bool complete)
			{
				if (!Instances.TryTake(out var Instance))
					Instance = new CollectionAddInstance();

				Instance.Initialise(owner, item, waitForSlot, complete);

				return Instance;
			}
		}

		private sealed class CollectionAddManyInstance : BaseCancellable, IValueTaskSource<bool>
		{ //****************************************
			private static readonly ConcurrentBag<CollectionAddManyInstance> Instances = new ConcurrentBag<CollectionAddManyInstance>();
			//**************************************** //****************************************
			private ManualResetValueTaskSourceCore<bool> _TaskSource = new ManualResetValueTaskSourceCore<bool>();

			private readonly Action _OnTookSlot;

			private IEnumerator<T>? _Items;
			private int _RemainingItems;
			private bool _Complete;
			private ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter _Awaiter;
			//****************************************

			internal CollectionAddManyInstance()
			{
				_TaskSource.RunContinuationsAsynchronously = true;
				_OnTookSlot = OnTookSlot;
			}

			//****************************************

			internal void Initialise(AsyncCollection<T> owner, IReadOnlyCollection<T> items, int consumed, bool complete, TimeSpan timeout, CancellationToken token)
			{
				Owner = owner;

				_Items = items.GetEnumerator();

				if (consumed > 0)
				{
					if (!owner.CompleteAdd(_Items, consumed, false))
					{
						// Collection was completed before we could fulfil the add range
						_TaskSource.SetResult(false);

						return;
					}
				}

				_RemainingItems = items.Count - consumed;
				_Complete = complete;

				// Need to register cancellation before we can wait on the counter, since we need the Token to be valid
				RegisterCancellation(token, timeout);

				_Awaiter = owner._FreeSlots!.TryDecrementAsync(_RemainingItems, Token).ConfigureAwait(false).GetAwaiter();

				if (_Awaiter.IsCompleted)
					OnTookSlot();
				else
					_Awaiter.OnCompleted(_OnTookSlot);
			}

			//****************************************

			bool IValueTaskSource<bool>.GetResult(short token)
			{
				try
				{
					return _TaskSource.GetResult(token);
				}
				finally
				{
					Release();
				}
			}

			public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			public ValueTaskSourceStatus GetStatus(short token) => _TaskSource.GetStatus(token);

			protected override void SwitchToCancelled()
			{
				// Handled by the OnTookSlot callback
			}

			protected override void UnregisteredCancellation()
			{
				// The entire collection has been added
				_TaskSource.SetResult(true);
			}

			//****************************************

			private void OnTookSlot()
			{
				try
				{
					for (; ; )
					{
						var Slots = _Awaiter.GetResult();

						if (Slots == -1)
						{
							// Collection was completed before we could fulfil the add-range
							_TaskSource.SetResult(false);

							return;
						}

						do
						{
							if (!Owner!.CompleteAdd(_Items!, Slots, _Complete && Slots == _RemainingItems))
							{
								// Collection was completed before we could fulfil the add-range
								_TaskSource.SetResult(false);

								return;
							}

							_RemainingItems -= Slots;

							if (_RemainingItems == 0)
								break;
						}
						while (_RemainingItems > 0 && Owner._FreeSlots!.TryDecrement(_RemainingItems, out Slots));

						if (_RemainingItems == 0)
							break;

						// More items to add
						_Awaiter = Owner!._FreeSlots!.TryDecrementAsync(_RemainingItems, Token).ConfigureAwait(false).GetAwaiter();

						if (!_Awaiter.IsCompleted)
						{
							_Awaiter.OnCompleted(_OnTookSlot);

							return;
						}
					}

					// No more items, cleanup cancellation and mark the add-range as complete
					UnregisterCancellation();
				}
				catch (Exception e)
				{
					// Will handle Timeout and OperationCancelled
					_TaskSource.SetException(e);
				}
			}

			private void Release()
			{
				_TaskSource.Reset();
				_Items = null!;
				_RemainingItems = 0;
				_Complete = false;
				_Awaiter = default;

				Instances.Add(this);
			}

			//****************************************

			public AsyncCollection<T>? Owner { get; private set; }

			public short Version => _TaskSource.Version;

			//****************************************

			internal static CollectionAddManyInstance GetOrCreateFor(AsyncCollection<T> owner, IReadOnlyCollection<T> items, int consumed, bool complete, TimeSpan timeout, CancellationToken token)
			{
				if (!Instances.TryTake(out var Instance))
					Instance = new CollectionAddManyInstance();

				Instance.Initialise(owner, items, consumed, complete, timeout, token);

				return Instance;
			}
		}

		private sealed class CollectionTakeInstance : IValueTaskSource<T>
		{ //****************************************
			private static readonly ConcurrentBag<CollectionTakeInstance> Instances = new ConcurrentBag<CollectionTakeInstance>();
			//****************************************
			private ManualResetValueTaskSourceCore<T> _TaskSource = new ManualResetValueTaskSourceCore<T>();
			private readonly Action _OnTookItem;

			private ConfiguredValueTaskAwaitable<bool>.ConfiguredValueTaskAwaiter _Awaiter;
			//****************************************

			internal CollectionTakeInstance()
			{
				_TaskSource.RunContinuationsAsynchronously = true;
				_OnTookItem = OnTookItem;
			}

			//****************************************

			internal void Initialise(AsyncCollection<T> owner, ValueTask<bool> waitForItem)
			{
				Owner = owner;
				_Awaiter = waitForItem.ConfigureAwait(false).GetAwaiter();

				if (_Awaiter.IsCompleted)
					OnTookItem();
				else
					_Awaiter.OnCompleted(_OnTookItem);
			}

			//****************************************

			T IValueTaskSource<T>.GetResult(short token)
			{
				try
				{
					return _TaskSource.GetResult(token);
				}
				finally
				{
					Release();
				}
			}

			public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			public ValueTaskSourceStatus GetStatus(short token) => _TaskSource.GetStatus(token);

			//****************************************

			private void OnTookItem()
			{
				try
				{
					if (_Awaiter.GetResult())
						_TaskSource.SetResult(Owner!.CompleteTake());
					else
						_TaskSource.SetException(new InvalidOperationException("Adding was completed while waiting for a slot"));
				}
				catch (Exception e)
				{
					_TaskSource.SetException(e);
				}
			}

			private void Release()
			{
				_TaskSource.Reset();
				_Awaiter = default;

				Instances.Add(this);
			}

			//****************************************

			public AsyncCollection<T>? Owner { get; private set; }

			public short Version => _TaskSource.Version;

			//****************************************

			internal static CollectionTakeInstance GetOrCreateFor(AsyncCollection<T> owner, ValueTask<bool> waitForSlot)
			{
				if (!Instances.TryTake(out var Instance))
					Instance = new CollectionTakeInstance();

				Instance.Initialise(owner, waitForSlot);

				return Instance;
			}
		}
	}
}
