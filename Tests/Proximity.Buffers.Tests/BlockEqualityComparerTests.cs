using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Proximity.Buffers.Tests
{
	[TestFixture]
	public class BlockEqualityComparerTests
	{
		[Test]
		[TestCase(100, 16)]
		[TestCase(101, 32)]
		[TestCase(102, 128)]
		[TestCase(103, 512)]
		[TestCase(104, 2048)]
		[TestCase(105, 16384)]
		public void AdlerHashCode(int seed, int length)
		{
			var Random = new Random(seed);

			var Buffer = new byte[length];

			Random.NextBytes(Buffer);

			var Expected = CalculateChecksum(Buffer);

			var Actual = BlockEqualityComparer<byte>.Default.GetHashCode(Buffer);

			Assert.AreEqual(Expected, Actual);
		}

		//****************************************

		private int CalculateChecksum(Span<byte> source)
		{
			const int AdlerModulus = 65521;

			// Adler-32 Checksum
			var A = 1u;
			var B = 0u;

			for (var Index = 0; Index < source.Length; Index++)
			{
				A = (A + source[Index]) % AdlerModulus;
				B = (B + A) % AdlerModulus;
			}

			return (int)((B << 16) | A);
		}
	}
}
