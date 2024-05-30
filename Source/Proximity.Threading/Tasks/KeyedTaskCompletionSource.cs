#if NET6_0_OR_GREATER
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading.Tasks
{
	/// <summary>
	/// Manages <see cref="TaskCompletionSource"/> objects based on a key
	/// </summary>
	/// <typeparam name="TKey">The key to reference each Task</typeparam>
	public sealed class KeyedTaskCompletionSource<TKey> : IDisposable where TKey : notnull
	{ //****************************************
		private readonly ConcurrentDictionary<TKey, KeyedItem> _Items = new();
		private readonly CancellationTokenSource _CancelSource = new();
		//****************************************

		/// <summary>
		/// Creates a new Keyed Task Completion Source
		/// </summary>
		public KeyedTaskCompletionSource()
		{
		}

		//****************************************

		/// <summary>
		/// Dispose of all pending keyed Tasks and prevent new ones from being added
		/// </summary>
		/// <remarks>Tasks will fault with an <see cref="ObjectDisposedException"/></remarks>
		public void Dispose()
		{
			_CancelSource.Cancel();

			foreach (var (_, Item) in _Items)
			{
				Item.Dispose();
			}
		}

		/// <summary>
		/// Dispose of all pending keyed Tasks and prevent new ones from being added
		/// </summary>
		public void DisposeWithResult()
		{
			_CancelSource.Cancel();

			foreach (var (Key, Item) in _Items)
			{
				if (Item.TrySetResult() && _Items.TryRemove(Key, out _))
					Item.Unregister();
			}
		}

		/// <summary>
		/// Tries to register a new keyed Task
		/// </summary>
		/// <param name="key">The key of the new Task</param>
		/// <param name="token">A cancellation token that will cause this Task to automatically cancel, if any</param>
		/// <param name="result">Receives the new keyed Task</param>
		/// <returns>True if the keyed Task was created, False if one already exists, or if the Keyed Task Completion Source has been disposed of</returns>
		public bool TryAdd(TKey key, CancellationToken token, [MaybeNullWhen(false)] out IKeyedItem result)
		{
			result = null;

			if (_CancelSource.IsCancellationRequested)
				return false;

			var Item = new KeyedItem(this, key, token);

			if (!_Items.TryAdd(key, Item))
				return false;

			if (_CancelSource.IsCancellationRequested)
			{
				_Items.TryRemove(key, out _);

				return false;
			}

			Item.Register();

			result = Item;

			return true;
		}

		/// <summary>
		/// Tries to retrieve a pending keyed Task
		/// </summary>
		/// <param name="key">The key of the pending Task to retrieve</param>
		/// <param name="result">Receives the keyed Task when successful</param>
		/// <returns>True if the keyed Task was found, otherwise False</returns>
		public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out IKeyedItem result)
		{
			if (_Items.TryGetValue(key, out var Item))
			{
				result = Item;

				return true;
			}

			result = null;

			return false;
		}

		//****************************************

		/// <summary>
		/// Gets whether there are any pending keyed Tasks
		/// </summary>
		public bool IsEmpty => _Items.IsEmpty;

		/// <summary>
		/// Gets the number of pending keyed Tasks
		/// </summary>
		public int Count => _Items.Count;

		/// <summary>
		/// Gets the keyed Task associated with a key
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		/// <exception cref="KeyNotFoundException">There is no Task with that key</exception>
		public IKeyedItem this[TKey key] => _Items[key];

		//****************************************

		private static void OnCancel(object? state) => ((KeyedItem)state!).Cancel();

		//****************************************

		private sealed class KeyedItem : TaskCompletionSource, IKeyedItem
		{ //****************************************
			private CancellationTokenRegistration _Registration;
			//****************************************

			public KeyedItem(KeyedTaskCompletionSource<TKey> parent, TKey key, CancellationToken token)
			{
				Parent = parent;
				Key = key;
				Token = token;
			}

			//****************************************

			public void Cancel()
			{
				if (TrySetCanceled(Token) && Parent._Items.TryRemove(Key, out _))
					Unregister();
			}

			public void Dispose()
			{
				if (Parent._Items.TryRemove(Key, out _))
				{
					Unregister();

					if (!Task.IsCompleted && TrySetException(new ObjectDisposedException(Key.ToString())))
						_ = Task.Exception; // Observe the exception
				}
			}

			public void Register()
			{
				_Registration = Token.Register(OnCancel, this);
			}

			public void Unregister() => _Registration.Dispose();

			bool IKeyedItem.TrySetResult()
			{
				if (!TrySetResult())
					return false;

				Dispose();

				return true;
			}

			bool IKeyedItem.TrySetCanceled()
			{
				if (!TrySetCanceled(Token))
					return false;

				Dispose();

				return true;
			}

			bool IKeyedItem.TrySetException(Exception exception)
			{
				if (!TrySetException(exception))
					return false;

				Dispose();

				return true;
			}

			//****************************************

			public KeyedTaskCompletionSource<TKey> Parent { get; }

			public TKey Key { get; }

			public CancellationToken Token { get; }
		}
	}

	/// <summary>
	/// Manages <see cref="TaskCompletionSource{TResult}"/> objects based on a key
	/// </summary>
	/// <typeparam name="TKey">The key to reference each Task</typeparam>
	/// <typeparam name="TResult">The result type of the Task</typeparam>
	public sealed class KeyedTaskCompletionSource<TKey, TResult> : IDisposable where TKey : notnull
	{ //****************************************
		private readonly ConcurrentDictionary<TKey, KeyedItem> _Items = new();
		private readonly CancellationTokenSource _CancelSource = new();
		//****************************************

		/// <summary>
		/// Creates a new Keyed Task Completion Source
		/// </summary>
		public KeyedTaskCompletionSource()
		{
		}

		//****************************************

		/// <summary>
		/// Dispose of all pending keyed Tasks and prevent new ones from being added
		/// </summary>
		/// <remarks>Tasks will fault with an <see cref="ObjectDisposedException"/></remarks>
		public void Dispose()
		{
			_CancelSource.Cancel();

			foreach (var (_, Item) in _Items)
			{
				Item.Dispose();
			}
		}

		/// <summary>
		/// Dispose of all pending keyed Tasks and prevent new ones from being added
		/// </summary>
		/// <param name="result">The result to return from all pending keyed Tasks</param>
		public void DisposeWith(TResult result)
		{
			_CancelSource.Cancel();

			foreach (var (Key, Item) in _Items)
			{
				if (Item.TrySetResult(result) && _Items.Remove(Key, out _))
					Item.Unregister();
			}
		}

		/// <summary>
		/// Tries to register a new keyed Task
		/// </summary>
		/// <param name="key">The key of the new Task</param>
		/// <param name="token">A cancellation token that will cause this Task to automatically cancel, if any</param>
		/// <param name="result">Receives the new keyed Task</param>
		/// <returns>True if the keyed Task was created, False if one already exists, or if the Keyed Task Completion Source has been disposed of</returns>
		public bool TryAdd(TKey key, CancellationToken token, [MaybeNullWhen(false)] out IKeyedItem<TResult> result)
		{
			result = null;

			if (_CancelSource.IsCancellationRequested)
				return false;

			var Item = new KeyedItem(this, key, token);

			if (!_Items.TryAdd(key, Item))
				return false;

			if (_CancelSource.IsCancellationRequested)
			{
				_Items.TryRemove(key, out _);

				return false;
			}

			Item.Register();

			result = Item;

			return true;
		}

		/// <summary>
		/// Tries to retrieve a pending keyed Task
		/// </summary>
		/// <param name="key">The key of the pending Task to retrieve</param>
		/// <param name="result">Receives the keyed Task when successful</param>
		/// <returns>True if the keyed Task was found, otherwise False</returns>
		public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out IKeyedItem<TResult> result)
		{
			if (_Items.TryGetValue(key, out var Item))
			{
				result = Item;

				return true;
			}

			result = null;

			return false;
		}

		//****************************************

		/// <summary>
		/// Gets whether there are any pending keyed Tasks
		/// </summary>
		public bool IsEmpty => _Items.IsEmpty;

		/// <summary>
		/// Gets the number of pending keyed Tasks
		/// </summary>
		public int Count => _Items.Count;

		/// <summary>
		/// Gets the keyed Task associated with a key
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		/// <exception cref="KeyNotFoundException">There is no Task with that key</exception>
		public IKeyedItem<TResult> this[TKey key] => _Items[key];

		//****************************************

		private static void OnCancel(object? state) => ((KeyedItem)state!).Cancel();

		//****************************************

		private sealed class KeyedItem : TaskCompletionSource<TResult>, IKeyedItem<TResult>
		{	//****************************************
			private CancellationTokenRegistration _Registration;
			//****************************************

			public KeyedItem(KeyedTaskCompletionSource<TKey, TResult> parent, TKey key, CancellationToken token)
			{
				Parent = parent;
				Key = key;
				Token = token;
			}

			//****************************************

			public void Cancel()
			{
				if (TrySetCanceled(Token) && Parent._Items.TryRemove(Key, out _))
					Unregister();
			}

			public void Dispose()
			{
				if (Parent._Items.TryRemove(Key, out _))
				{
					Unregister();

					if (!Task.IsCompleted && TrySetException(new ObjectDisposedException(Key.ToString())))
						_ = Task.Exception; // Observe the exception
				}
			}

			public void Register()
			{
				_Registration = Token.Register(OnCancel, this);
			}

			public void Unregister() => _Registration.Dispose();

			bool IKeyedItem<TResult>.TrySetResult(TResult result)
			{
				if (!TrySetResult(result))
					return false;

				Dispose();

				return true;
			}

			bool IKeyedItem<TResult>.TrySetCanceled()
			{
				if (!TrySetCanceled(Token))
					return false;

				Dispose();

				return true;
			}

			bool IKeyedItem<TResult>.TrySetException(Exception exception)
			{
				if (!TrySetException(exception))
					return false;

				Dispose();

				return true;
			}

			//****************************************

			public KeyedTaskCompletionSource<TKey, TResult> Parent { get; }

			public TKey Key { get; }

			public CancellationToken Token { get; }
		}
	}
}
#endif
