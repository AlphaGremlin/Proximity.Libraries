using System;
using System.Buffers;
using System.IO;
using System.Text;
using NUnit.Framework;
//****************************************

namespace Proximity.Buffers.Tests
{
	/// <summary>
	/// Tests the functionality of the <see cref="BufferWriter"/> class
	/// </summary>
	[TestFixture()]
	public class BufferTextWriterTests
	{ //****************************************
		private string _TestInput = @"ABC!DEF@GHI#JKL$123%456^789&0*MNO(PQR)STU-VWX=YZ abcdefghijklmnopqrstuvwxyz ♥ ";
		private char[] _TestOutput;
		//****************************************

		[OneTimeSetUp()]
		public void Setup()
		{
			_TestOutput = _TestInput.ToCharArray();
		}

		//****************************************

		[Test()]
		public void WriteDefault()
		{ //****************************************
			char[] MyResult;
			char[] TempOutput;
			//****************************************

			TempOutput = _TestInput.ToCharArray();

			//****************************************

			using (var Buffer = new BufferWriter<char>())
			using (var Writer = new BufferWriter(Buffer))
			{
				Writer.Write(_TestInput);

				MyResult = Buffer.ToSequence().ToArray();
			}

			//****************************************

			CollectionAssert.AreEqual(TempOutput, MyResult);
		}

		[Test()]
		public void Write([Values(8, 16, 32, 64, 128)] int capacity)
		{ //****************************************
			char[] MyResult;
			//****************************************

			using (var Buffer = new BufferWriter<char>())
			using (var Writer = new BufferWriter(Buffer, capacity))
			{
				Writer.Write(_TestInput);

				MyResult = Buffer.ToSequence().ToArray();
			}

			//****************************************

			CollectionAssert.AreEqual(_TestOutput, MyResult);
		}

		[Test()]
		public void WriteChar([Values(8, 16, 32, 64, 128)] int capacity)
		{ //****************************************
			char[] MyResult;
			//****************************************

			using (var Buffer = new BufferWriter<char>())
			using (var Writer = new BufferWriter(Buffer, capacity))
			{
				foreach (var MyChar in _TestInput)
				{
					Writer.Write(MyChar);
				}

				MyResult = Buffer.ToSequence().ToArray();
			}

			//****************************************

			CollectionAssert.AreEqual(_TestOutput, MyResult);
		}
	}
}
