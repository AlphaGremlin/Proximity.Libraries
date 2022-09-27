using System.Buffers;
using System.Data.Common;
using System.Threading;

namespace System.Data
{
	/// <summary>
	/// Provides an <see cref="IBufferReader{Char}"/> over a text column in a <see cref="DbDataReader" />
	/// </summary>
	/// <remarks>Once the column is read, call <see cref="NextRow"/> to reset for the next row</remarks>
	public sealed class DbTextReader : IBufferReader<char>, IDisposable
	{ //****************************************
		private const int MinimumBufferSize = 1024;
		//****************************************
		private readonly DbDataReader _Reader;
		private readonly int _Ordinal;

		private char[]? _OutputBuffer;
		private int _Offset, _Index, _Count;
		private bool _CanRead;
		//****************************************

		/// <summary>
		/// Creates a new Text Reader
		/// </summary>
		/// <param name="reader">The <see cref="DbDataReader" /></param>
		/// <param name="ordinal">The column ordinal to read from</param>
		public DbTextReader(DbDataReader reader, int ordinal)
		{
			_Reader = reader;
			_Ordinal = ordinal;
		}

		/// <summary>
		/// Creates a new Text Reader
		/// </summary>
		/// <param name="reader">The <see cref="DbDataReader" /></param>
		/// <param name="columnName">The column name to read from</param>
		public DbTextReader(DbDataReader reader, string columnName)
		{
			_Reader = reader;
			_Ordinal = reader.GetOrdinal(columnName);
		}

		//****************************************

		/// <summary>
		/// Resets the reader for another row
		/// </summary>
		public void NextRow()
		{
			_Offset = 0;
			_Index = 0;
			_Count = 0;

			_CanRead = true;
		}

		/// <summary>
		/// Advances the reader
		/// </summary>
		/// <param name="count">The number of characters to advance</param>
		public void Advance(int count)
		{
			_Offset += count;

			_Count = Math.Max(0, _Count - count);

			if (_Count == 0)
			{
				_Index = 0;
			}
			else
			{
				_Index += count;
			}
		}

		/// <summary>
		/// Reads the next section of the buffer
		/// </summary>
		/// <param name="minSize">The minimum desired size</param>
		/// <returns>A buffer representing the available characters</returns>
		/// <remarks>May be less than <paramref name="minSize"/> if there are not enough characters available</remarks>
		public ReadOnlyMemory<char> GetMemory(int minSize)
		{
			if (minSize > _Count)
			{
				if (_OutputBuffer == null)
				{
					_OutputBuffer = ArrayPool<char>.Shared.Rent(Math.Max(minSize, MinimumBufferSize));
				}
				else if (_OutputBuffer.Length < minSize)
				{
					var OldBuffer = Interlocked.Exchange(ref _OutputBuffer, (char[]?)null);

					try
					{
						// If we get OOM here, we at least return the rented buffer
						_OutputBuffer = ArrayPool<char>.Shared.Rent(Math.Max(minSize, MinimumBufferSize));

						// No need to read more from the DbDataReader than necessary, copy what we already grabbed
						if (_Count > 0)
						{
							Array.Copy(OldBuffer, _Index, _OutputBuffer, 0, _Count);

							_Index = 0;
						}
					}
					finally
					{
						ArrayPool<char>.Shared.Return(OldBuffer);
					}
				}
				else
				{
					// No need to read more from the DbDataReader than necessary, shift the remaining data down
					if (_Count > 0)
					{
						Array.Copy(_OutputBuffer, _Index, _OutputBuffer, 0, _Count);

						_Index = 0;
					}
				}

				// Read what we need to fill the buffer
				var StartFrom = _Index + _Count;
				var BytesRead = (int)_Reader.GetChars(_Ordinal, _Offset + _Count, _OutputBuffer, StartFrom, _OutputBuffer.Length - StartFrom);

				_CanRead = BytesRead > 0;

				_Count += BytesRead;
			}

			return _OutputBuffer.AsMemory(_Index, _Count);
		}

		/// <summary>
		/// Reads the next section of the buffer
		/// </summary>
		/// <param name="minSize">The minimum desired size</param>
		/// <returns>A buffer representing the available characters</returns>
		/// <remarks>May be less than <paramref name="minSize"/> if there are not enough bytes available</remarks>
		public ReadOnlySpan<char> GetSpan(int minSize) => GetMemory(minSize).Span;

		/// <summary>
		/// Cleans up any buffers rented by the Reader
		/// </summary>
		public void Dispose()
		{
			var Buffer = Interlocked.Exchange(ref _OutputBuffer, null);

			if (Buffer != null)
				ArrayPool<char>.Shared.Return(Buffer);
		}

		//****************************************

		/// <summary>
		/// Gets whether there are more characters to read
		/// </summary>
		public bool CanRead => _CanRead || _Count > 0;
	}
}
