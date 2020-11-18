using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Proximity.Buffers.Tests
{
	[TestFixture]
	public class BufferWriterTests
	{
		[Test]
		public void Empty()
		{
			using (var Writer = new BufferWriter<byte>(ExactPool<byte>.Shared))
			{
				var Sequence = Writer.ToSequence();

				Assert.IsTrue(Sequence.IsEmpty);
			}
		}

		[Test]
		[TestCase(1)]
		[TestCase(16)]
		[TestCase(128)]
		[TestCase(1024)]
		public void SingleByte(int totalBytes)
		{
			using (var Writer = new BufferWriter<byte>(ExactPool<byte>.Shared))
			{
				for (var Index = 0; Index < totalBytes; Index++)
				{
					var Contents = Writer.GetSpan(1);

					Contents[0] = 1;

					Writer.Advance(1);
				}

				Assert.AreEqual(totalBytes, Writer.Length);

				var Sequence = Writer.ToSequence();

				Assert.AreEqual(totalBytes, Sequence.Length);
			}
		}

		[Test]
		[TestCase(1024, 32)]
		[TestCase(1024 * 4, 1024)]
		[TestCase(1024, 200)]
		public void Blocks(int totalBytes, int blockSize)
		{
			using (var Writer = new BufferWriter<byte>(ExactPool<byte>.Shared))
			{
				for (var Index = 0; Index < totalBytes; Index += blockSize)
				{
					var NextBlockSize = Math.Min(blockSize, totalBytes - Index);

					var Contents = Writer.GetSpan(NextBlockSize);

					Contents.Fill(1);

					Writer.Advance(NextBlockSize);
				}

				Assert.AreEqual(totalBytes, Writer.Length);

				var Sequence = Writer.ToSequence();

				Assert.AreEqual(totalBytes, Sequence.Length);
			}
		}
	}
}
