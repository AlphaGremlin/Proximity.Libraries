/****************************************\
 LinkedCancellationToken.cs
 Created: 2014-12-05
\****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides a framework for linking cancellation tokens without constructing unnecessary <see cref="CancellationTokenSource" /> objects
	/// </summary>
	public struct LinkedCancellationToken : IDisposable
	{	//****************************************
		private readonly CancellationTokenSource _LinkedSource;
		private readonly CancellationToken _Token;
		//****************************************
		
		private LinkedCancellationToken(CancellationTokenSource linkedSource, CancellationToken token)
		{
			_LinkedSource = linkedSource;
			_Token = token;
		}
		
		//****************************************
		
		/// <summary>
		/// Disposes of the linked cancellation token
		/// </summary>
		public void Dispose()
		{
			if (_LinkedSource != null)
				_LinkedSource.Dispose();
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the <see cref="CancellationToken" /> associated with this linked cancellation token
		/// </summary>
		public CancellationToken Token
		{
			get { return _Token; }
		}
		
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
			bool CanCancel = false;
			CancellationToken TargetToken;
			//****************************************
			
			if (tokens == null || tokens.Length == 0)
				return new LinkedCancellationToken(null, CancellationToken.None);
			
			for (int Index = 0; Index < tokens.Length; Index++)
			{
				if (tokens[Index].CanBeCanceled)
				{
					if (CanCancel)
					{
						var MyTokenSource = CancellationTokenSource.CreateLinkedTokenSource(tokens);
						
						return new LinkedCancellationToken(MyTokenSource, MyTokenSource.Token);
					}
					
					CanCancel = true;
					TargetToken = tokens[Index];
				}
			}
			
			if (CanCancel)
			{
				return new LinkedCancellationToken(null, TargetToken);
			}

			return new LinkedCancellationToken(null, CancellationToken.None);
		}
	}
}
