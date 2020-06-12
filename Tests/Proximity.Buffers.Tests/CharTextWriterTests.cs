using System;
using System.Buffers;
using System.IO;
using System.Text;
using NUnit.Framework;
//****************************************

namespace Proximity.Buffers.Tests
{
	/// <summary>
	/// Tests the functionality of the <see cref="CharTextWriter"/> class
	/// </summary>
	[TestFixture()]
	public class CharTextWriterTests
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
			using (var Writer = new CharTextWriter(Buffer))
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
			using (var Writer = new CharTextWriter(Buffer, capacity))
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
			using (var Writer = new CharTextWriter(Buffer, capacity))
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
