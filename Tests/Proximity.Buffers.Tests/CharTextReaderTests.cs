using System;
using System.IO;
using System.Text;
using NUnit.Framework;
//****************************************

namespace Proximity.Buffers.Tests
{
	/// <summary>
	/// Tests the functionality of the <see cref="CharTextReader"/> class
	/// </summary>
	[TestFixture()]
	public class CharTextReaderTests
	{ //****************************************
		private string _TestOutput = @"ABC!DEF@GHI#JKL$123%456^789&0*MNO(PQR)STU-VWX=YZ abcdefghijklmnopqrstuvwxyz ♥ ";
		private char[] _TestInput;
		//****************************************

		[OneTimeSetUp()]
		public void Setup()
		{
			_TestInput = _TestOutput.ToCharArray();
		}

		//****************************************

		[Test()]
		public void ReadToEnd()
		{
			string MyResult;

			using (var Reader = new CharTextReader(_TestInput))
			{
				MyResult = Reader.ReadToEnd();
			}

			Assert.AreEqual(_TestOutput, MyResult);
		}

		[Test()]
		public void ReadBlock([Values(8, 16, 32, 64, 128)] int capacity)
		{
			var MyResult = new StringBuilder();
			var MyBuffer = new char[capacity];

			using (var Reader = new CharTextReader(_TestInput))
			{
				for (; ; )
				{
					int CharsRead = Reader.ReadBlock(MyBuffer, 0, capacity);

					if (CharsRead == 0)
						break;

					MyResult.Append(MyBuffer, 0, CharsRead);
				}
			}

			Assert.AreEqual(_TestOutput, MyResult.ToString());
		}

		[Test()]
		public void Read()
		{
			var MyResult = new StringBuilder();

			using (var Reader = new CharTextReader(_TestInput))
			{
				for (; ; )
				{
					int MyChar = Reader.Read();

					if (MyChar == -1)
						break;

					MyResult.Append(char.ConvertFromUtf32(MyChar));
				}
			}

			Assert.AreEqual(_TestOutput, MyResult.ToString());
		}

		[Test()]
		public void ReadAndReset()
		{ //****************************************
			string MyResult1, MyResult2;
			//****************************************

			using (var Reader = new CharTextReader(_TestInput))
			{
				MyResult1 = Reader.ReadToEnd();

				Reader.Reset();

				MyResult2 = Reader.ReadToEnd();
			}

			Assert.AreEqual(_TestOutput, MyResult1);
			Assert.AreEqual(_TestOutput, MyResult2);
		}
	}
}
