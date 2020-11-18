using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace System.Threading
{
	/// <summary>
	/// Allows linking cancellation tokens without constructing unnecessary <see cref="CancellationTokenSource" /> objects
	/// </summary>
	public readonly struct LinkedCancellationToken : IDisposable
	{	//****************************************
		private readonly CancellationTokenSource? _LinkedSource;
		//****************************************

		private LinkedCancellationToken(CancellationTokenSource? linkedSource, CancellationToken token)
		{
			_LinkedSource = linkedSource;
			Token = token;
		}

		//****************************************

		/// <summary>
		/// Disposes of the linked cancellation token
		/// </summary>
		public void Dispose() => _LinkedSource?.Dispose();

		//****************************************

		/// <summary>
		/// Gets the <see cref="CancellationToken" /> associated with this linked cancellation token
		/// </summary>
		public CancellationToken Token { get; }

		//****************************************

		/// <summary>
		/// Creates a linked cancellation token
		/// </summary>
		/// <param name="token1">The first token to link</param>
		/// <param name="token2">The second token to link</param>
		/// <returns>A linked cancellation token that may or may not wrap a combined cancellation token</returns>
		/// <remarks>If more than one passed token is cancellable, creates a combined token, otherwise provides that token</remarks>
		public static LinkedCancellationToken Create(CancellationToken token1, CancellationToken token2)
		{
			if (token1.CanBeCanceled)
			{
				if (token2.CanBeCanceled)
				{
					var MyTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token1, token2);
					
					return new LinkedCancellationToken(MyTokenSource, MyTokenSource.Token);
				}
				
				return new LinkedCancellationToken(null, token1);
			}
			
			if (token2.CanBeCanceled)
			{
				return new LinkedCancellationToken(null, token2);
			}
			
			return new LinkedCancellationToken(null, CancellationToken.None);
		}
		
		/// <summary>
		/// Creates a linked cancellation token
		/// </summary>
		/// <param name="tokens">The tokens to link</param>
		/// <returns>A linked cancellation token that may or may not wrap a combined cancellation token</returns>
		/// <remarks>If more than one passed token is cancellable, creates a combined token, otherwise provides that token</remarks>
		public static LinkedCancellationToken Create(params CancellationToken[] tokens)
		{	//****************************************
			var CanCancel = false;
			CancellationToken TargetToken;
			//****************************************
			
			if (tokens == null || tokens.Length == 0)
				return new LinkedCancellationToken(null, CancellationToken.None);
			
			for (var Index = 0; Index < tokens.Length; Index++)
			{
				if (tokens[Index].CanBeCanceled)
				{
					if (CanCancel)
					{
						var TokenSource = CancellationTokenSource.CreateLinkedTokenSource(tokens);
						
						return new LinkedCancellationToken(TokenSource, TokenSource.Token);
					}
					
					CanCancel = true;
					TargetToken = tokens[Index];
				}
			}
			
			if (CanCancel)
				return new LinkedCancellationToken(null, TargetToken);

			return new LinkedCancellationToken(null, CancellationToken.None);
		}
	}
}
