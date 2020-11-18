using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Proximity.Threading
{
	internal abstract class BaseCancellable
	{ //****************************************
#if !NETSTANDARD2_0
		private readonly Action _CompletedCleanup;
		private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _Awaiter;
#endif

		private CancellationToken _Token;
		private CancellationTokenRegistration _Registration;
		private CancellationTokenSource? _TokenSource;
		//****************************************

		protected BaseCancellable()
		{
#if !NETSTANDARD2_0
			_CompletedCleanup = OnCompletedCleanup;
#endif
		}

		//****************************************

		protected abstract void UnregisteredCancellation();

		protected abstract void SwitchToCancelled();

		protected Exception CreateCancellationException()
		{
			// This token is the one we were given
			// If it's not cancelled, that means the timeout was triggered
			if (_Token.IsCancellationRequested)
				// Either we have no timeout set, or it was the underlying token (if any) that cancelled
				return new OperationCanceledException(_Token);

			// The timeout was triggered. Use TimeoutException because we don't want to expose our inner Cancellation Token
			return new TimeoutException();
		}

		//****************************************

		protected void RegisterCancellation(CancellationToken token, TimeSpan timeout)
		{
			_Token = token;

			if (timeout > TimeSpan.Zero)
			{
				// If the token can cancel, wrap it with one that times out as well
				if (token.CanBeCanceled)
					_TokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
				else
					// Can't cancel, just create a token source that times out
					_TokenSource = new CancellationTokenSource();

				_TokenSource.CancelAfter(timeout);

				token = _TokenSource.Token;
			}
			else if (timeout != Timeout.InfiniteTimeSpan)
				throw new ArgumentOutOfRangeException(nameof(timeout));

			if (token.CanBeCanceled)
			{
				if (token.IsCancellationRequested)
					SwitchToCancelled();
				else
					_Registration = token.Register((state) => ((BaseCancellable)state).SwitchToCancelled(), this, false);
			}
		}

		protected void UnregisterCancellation()
		{
			_TokenSource?.Dispose();
			_TokenSource = null;
			_Token = default;

#if NETSTANDARD2_0
			// This may block if SwitchToCancelled is running
			_Registration.Dispose();

			OnCompletedCleanup();
#else
			// This will not block if SwitchToCancelled is running, but may continue later
			_Awaiter = _Registration.DisposeAsync().ConfigureAwait(false).GetAwaiter();

			if (_Awaiter.IsCompleted)
				OnCompletedCleanup();
			else
				_Awaiter.OnCompleted(_CompletedCleanup);
#endif
		}

		protected void ResetCancellation()
		{
			_TokenSource?.Dispose();
			_TokenSource = null;
			_Token = default;
		}

		//****************************************

		private void OnCompletedCleanup()
		{
#if !NETSTANDARD2_0
			_Awaiter.GetResult();

			_Awaiter = default;
#endif

			UnregisteredCancellation();
		}

		//****************************************

		protected CancellationToken Token => _TokenSource?.Token ?? _Token;

		/// <summary>
		/// Gets whether the supplied Token has been cancelled
		/// </summary>
		/// <remarks>If <see cref="Token"/> is not cancelled, this can mean a timeout occurred instead</remarks>
		protected bool IsCancelled => _Token.IsCancellationRequested;
	}
}
