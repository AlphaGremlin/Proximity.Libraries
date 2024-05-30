using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paritech.Foundry.Control.Data
{
	internal sealed class Base64BinaryReader : IBufferReader<byte>, IDisposable
	{ //****************************************
		private IBufferReader<byte> _Reader;

		private byte[]? _Buffer;
		private int _Offset, _Length;
		//****************************************

		public Base64BinaryReader(IBufferReader<byte> reader)
		{
			_Reader = reader ?? throw new ArgumentNullException(nameof(reader));
		}

		//****************************************

		/// <summary>
		/// Restarts the reader, decoding a new block of Base64 from the current Reader
		/// </summary>
		public void Reset()
		{

		}

		/// <summary>
		/// Restarts the reader, decoding a new block of Base64 from a new Reader
		/// </summary>
		/// <param name="reader">The new Buffer Reader to decode Base64 from</param>
		public void Reset(IBufferReader<byte> reader)
		{
			_Reader = reader ?? throw new ArgumentNullException(nameof(reader));
		}

		public void Dispose()
		{
			if (_Buffer != null)
			{
				ArrayPool<byte>.Shared.Return(_Buffer);

				_Buffer = null;
			}
		}

		//****************************************

		public void Advance(int count)
		{
			if (count < 0 || count > _Length)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (count == 0)
				return;

			_Length -= count;

			if (_Length == 0)
				_Offset = 0;
			else
				_Offset += count;
		}

		public ReadOnlyMemory<byte> GetMemory(int minSize)
		{
			if (minSize < 0)
				throw new ArgumentOutOfRangeException(nameof(minSize));

			if (minSize == 0)
			{
				// If we have any buffered data, that's enough to satisfy the request
				if (_Length > 0)
					return _Buffer.AsMemory(_Offset, _Length);
			}
			else
			{
				// If we have any buffered data, that's enough to satisfy the request
				if (_Length >= minSize)
					return _Buffer.AsMemory(_Offset, _Length);
			}

			var ReadSize = Base64.GetMaxEncodedToUtf8Length(minSize);

			// Need to read more data
			var Input = _Reader.GetSpan(ReadSize);

			if (Input.Length < 4)
			{
				// We need at least four bytes to decode a block of Base64
				_Reader.Advance(0);

				Input = _Reader.GetSpan(4);
			}

			var WriteSize = Base64.GetMaxDecodedFromUtf8Length(Input.Length);

			var Pool = ArrayPool<byte>.Shared;

			if (_Buffer == null)
			{
				_Buffer = Pool.Rent(WriteSize);
			}
			else if (_Buffer.Length < WriteSize)
			{
				Pool.Return(_Buffer);
				_Buffer = Pool.Rent(WriteSize);
			}

			//Base64.DecodeFromUtf8(Input, 


			return _Buffer.AsMemory(_Offset, _Length);
		}

		public ReadOnlySpan<byte> GetSpan(int minSize) => GetMemory(minSize).Span;

		//****************************************

		/// <summary>
		/// Gets whether there is more Base64 data to read
		/// </summary>
		public bool CanRead { get; }
	}
}
