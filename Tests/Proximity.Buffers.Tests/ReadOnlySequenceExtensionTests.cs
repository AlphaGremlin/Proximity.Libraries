using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Proximity.Buffers.Tests
{
	[TestFixture]
	public sealed class ReadOnlySequenceExtensionTests
	{
		[Test]
		public void ByteSequenceStartsWithSequence([Values(1, 2, 3)] int inputSize, [Values(1, 2, 3)] int compareSize, [Values(1, 2, 3)] int offset)
		{
			var Random = new Random();

			var Input = new byte[4];

			Random.NextBytes(Input);

			var Compare = Input.AsSpan(0, Input.Length - offset).ToArray();

			//Compare[Compare.Length - offset] ^= 0xFF;

			var InputSequence = SequenceFrom(Input, inputSize);

			var CompareSequence = SequenceFrom(Compare, compareSize);

			Assert.IsTrue(InputSequence.StartsWith(CompareSequence));
		}

		[Test]
		public void ByteSequenceStartsWithSequenceNoMatch([Values(1, 2, 3)] int inputSize, [Values(1, 2, 3)] int compareSize, [Values(1, 2, 3)] int offset)
		{
			var Random = new Random();

			var Input = new byte[32];

			Random.NextBytes(Input);

			var Compare = Input.AsSpan(0, Input.Length - offset).ToArray();

			Compare[Compare.Length - offset] ^= 0x55;

			var InputSequence = SequenceFrom(Input, inputSize);

			var CompareSequence = SequenceFrom(Compare, compareSize);

			Assert.IsFalse(InputSequence.StartsWith(CompareSequence));
		}

		//****************************************

		private ReadOnlySequence<byte> SequenceFrom(ReadOnlyMemory<byte> source, int size)
		{
			var Segments = Math.DivRem(source.Length, size, out var Remainder);

			if (Remainder > 0)
			{
				return (from segment in Enumerable.Range(0, Segments + 1) select source.Slice(segment * size, segment == Segments ? Remainder : size)).Combine();
			}

			return (from segment in Enumerable.Range(0, Segments) select source.Slice(segment * size, size)).Combine();
		}
	}
}
