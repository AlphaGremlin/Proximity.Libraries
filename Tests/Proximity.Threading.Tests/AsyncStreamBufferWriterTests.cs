using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Proximity.Threading.Tests.Mocks;

namespace Proximity.Threading.Tests
{
	[TestFixture]
	public sealed class AsyncStreamBufferWriterTests
	{
		[Test]
		public async Task SingleWrite()
		{
			using var Verifier = new VerifyingAsyncWriteStream(new TimeSpan(0, 0, 0, 0, 10));
			var InputLength = 0;

			await using (var Writer = new AsyncStreamBufferWriter(Verifier))
			{
				var Content = Encoding.ASCII.GetBytes("Write Some Text");

				InputLength = Content.Length;

				Writer.Write(Content);
			}

			Assert.AreEqual(InputLength, Verifier.Length, "Operation did not finish");
		}
	}
}
