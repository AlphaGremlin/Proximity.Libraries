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
			using var Writer = new BufferWriter<byte>(ExactPool<byte>.Shared);

			var Sequence = Writer.ToSequence();

			Assert.IsTrue(Sequence.IsEmpty);
		}

		[Test]
		[TestCase(1)]
		[TestCase(16)]
		[TestCase(128)]
		[TestCase(1024)]
		public void SingleByte(int totalBytes)
		{
			using var Writer = new BufferWriter<byte>(ExactPool<byte>.Shared);

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

		[Test]
		[TestCase(1024, 30)]
		[TestCase(3000, 30)]
		[TestCase(3000, 31)]
		[TestCase(1024, 32)]
		[TestCase(1024 * 4, 1024)]
		[TestCase(1024, 200)]
		public void Blocks(int totalBytes, int blockSize)
		{
			using var Writer = new BufferWriter<byte>(ExactPool<byte>.Shared);

			for (var Index = 0; Index < totalBytes; Index += blockSize)
			{
				var NextBlockSize = Math.Min(blockSize, totalBytes - Index);

				var Contents = Writer.GetSpan(NextBlockSize);

				Contents.Slice(0, NextBlockSize).Fill(1);

				Writer.Advance(NextBlockSize);
			}

			Assert.AreEqual(totalBytes, Writer.Length);

			var Sequence = Writer.ToSequence();

			Assert.AreEqual(totalBytes, Sequence.Length);
		}

		[Test]
		[TestCase(1024, 30)]
		[TestCase(3000, 30)]
		[TestCase(3000, 31)]
		[TestCase(1024, 32)]
		[TestCase(1024 * 4, 1024)]
		[TestCase(1024, 200)]
		public void BlocksApproximate(int totalBytes, int blockSize)
		{
			using var Writer = new BufferWriter<byte>();

			for (var Index = 0; Index < totalBytes; Index += blockSize)
			{
				var NextBlockSize = Math.Min(blockSize, totalBytes - Index);

				var Contents = Writer.GetSpan(NextBlockSize);

				Contents.Slice(0, NextBlockSize).Fill(1);

				Writer.Advance(NextBlockSize);
			}

			Assert.AreEqual(totalBytes, Writer.Length);

			var Sequence = Writer.ToSequence();

			Assert.AreEqual(totalBytes, Sequence.Length);
		}

		[Test]
		[TestCase(1024, 30)]
		[TestCase(3000, 30)]
		[TestCase(3000, 31)]
		[TestCase(1024, 32)]
		[TestCase(1024 * 4, 1024)]
		[TestCase(1024, 200)]
		public void BlocksSmallMinimum(int totalBytes, int blockSize)
		{
			using var Writer = new BufferWriter<byte>(ExactPool<byte>.Shared, 1);

			for (var Index = 0; Index < totalBytes; Index += blockSize)
			{
				var NextBlockSize = Math.Min(blockSize, totalBytes - Index);

				var Contents = Writer.GetSpan(NextBlockSize);

				Contents.Slice(0, NextBlockSize).Fill(1);

				Writer.Advance(NextBlockSize);
			}

			Assert.AreEqual(totalBytes, Writer.Length);

			var Sequence = Writer.ToSequence();

			Assert.AreEqual(totalBytes, Sequence.Length);
		}

		[Test]
		[TestCase(1024, 30)]
		[TestCase(3000, 30)]
		[TestCase(3000, 31)]
		[TestCase(1024, 32)]
		[TestCase(1024 * 4, 1024)]
		[TestCase(1024, 200)]
		public void BlocksSmallMinimumApproximate(int totalBytes, int blockSize)
		{
			using var Writer = new BufferWriter<byte>(ArrayPool<byte>.Shared, 1);

			for (var Index = 0; Index < totalBytes; Index += blockSize)
			{
				var NextBlockSize = Math.Min(blockSize, totalBytes - Index);

				var Contents = Writer.GetSpan(NextBlockSize);

				Contents.Slice(0, NextBlockSize).Fill(1);

				Writer.Advance(NextBlockSize);
			}

			Assert.AreEqual(totalBytes, Writer.Length);

			var Sequence = Writer.ToSequence();

			Assert.AreEqual(totalBytes, Sequence.Length);
		}

		[Test]
		[TestCase(1024, 30, 7)]
		[TestCase(3000, 30, 7)]
		[TestCase(3000, 31, 7)]
		[TestCase(1024, 32, 7)]
		[TestCase(1024 * 4, 1024, 7)]
		[TestCase(1024, 200, 7)]
		public void Trim(int totalBytes, int blockSize, int trim)
		{
			var Random = new Random();

			var Source = new byte[totalBytes];
			Random.NextBytes(Source);

			using var Writer = new BufferWriter<byte>(ArrayPool<byte>.Shared, 1);

			for (var Index = 0; Index < totalBytes; Index += blockSize)
			{
				var NextBlockSize = Math.Min(blockSize, totalBytes - Index);

				var Contents = Writer.GetSpan(NextBlockSize);

				Source.AsSpan(Index, NextBlockSize).CopyTo(Contents);

				Writer.Advance(NextBlockSize);
			}

			Assert.AreEqual(totalBytes, Writer.Length);

			for (var Index = trim; Index < totalBytes; Index += trim)
			{
				Writer.TrimStart(trim);

				Assert.AreEqual(Writer.Length, Source.Length - Index);

				Assert.IsTrue(Writer.ToSequence().SequenceEqual(Source.AsSpan(Index)));
			}
		}

		[Test]
		[TestCase(1024, 30, 7)]
		[TestCase(3000, 30, 7)]
		[TestCase(3000, 31, 7)]
		[TestCase(1024, 32, 7)]
		[TestCase(1024 * 4, 1024, 7)]
		[TestCase(1024, 200, 7)]
		public void TrimAdd(int totalBytes, int blockSize, int trim)
		{
			var Random = new Random();

			var Source = new byte[totalBytes];
			Random.NextBytes(Source);

			using var Writer = new BufferWriter<byte>(ArrayPool<byte>.Shared, 1);

			var TotalTrimmed = 0;

			for (var Index = 0; Index < totalBytes; Index += blockSize)
			{
				var NextBlockSize = Math.Min(blockSize, totalBytes - Index);

				var Contents = Writer.GetSpan(NextBlockSize);

				Source.AsSpan(Index, NextBlockSize).CopyTo(Contents);

				Writer.Advance(NextBlockSize);

				Writer.TrimStart(trim);

				TotalTrimmed += trim;
			}

			Assert.AreEqual(totalBytes - TotalTrimmed, Writer.Length);

			Assert.IsTrue(Writer.ToSequence().SequenceEqual(Source.AsSpan(TotalTrimmed)));
		}
	}
}
