/****************************************\
 BinaryTextReaderTests.cs
 Created: 2016-03-09
\****************************************/
using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Proximity.Utility.IO;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Tests the functionality of the BinaryTextReader class
	/// </summary>
	[TestFixture()]
	public class BinaryTextReaderTests
	{	//****************************************
		private string _TestOutput = @"ABC!DEF@GHI#JKL$123%456^789&0*MNO(PQR)STU-VWX=YZ abcdefghijklmnopqrstuvwxyz ♥ ";
		private byte[] _TestInput;
		//****************************************

		[TestFixtureSetUp()]
		public void Setup()
		{
			using (var RawStream = new MemoryStream())
			{
				using (var Writer = new StreamWriter(RawStream, Encoding.UTF8))
				{
					Writer.Write(_TestOutput);
				}

				_TestInput = RawStream.ToArray();
			}

//			_TestOutput = Encoding.UTF8.GetBytes(_TestInput);
		}

		//****************************************

		[Test()]
		public void ReadToEnd()
		{
			string MyResult;

			using (var Reader = new BinaryTextReader(_TestInput, Encoding.UTF8, false))
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

			using (var Reader = new BinaryTextReader(_TestInput, Encoding.UTF8, false))
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

			using (var Reader = new BinaryTextReader(_TestInput, Encoding.UTF8, false))
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
		public void ReadDetectEncodingUTF8()
		{
			string MyResult;

			using (var Reader = new BinaryTextReader(_TestInput, Encoding.ASCII, true))
			{
				MyResult = Reader.ReadToEnd();
			}

			Assert.AreEqual(_TestOutput, MyResult);
		}

		[Test()]
		public void ReadDetectEncodingUTF32([Values(false, true)] bool bigEndian)
		{
			string MyResult;
			byte[] TempInput;

			using (var RawStream = new MemoryStream())
			{
				using (var Writer = new StreamWriter(RawStream, new UTF32Encoding(bigEndian, true)))
				{
					Writer.Write(_TestOutput);
				}

				TempInput = RawStream.ToArray();
			}

			using (var Reader = new BinaryTextReader(TempInput, Encoding.ASCII, true))
			{
				MyResult = Reader.ReadToEnd();
			}

			Assert.AreEqual(_TestOutput, MyResult);
		}

		[Test()]
		public void ReadDetectEncodingUnicode([Values(false, true)] bool bigEndian)
		{
			string MyResult;
			byte[] TempInput;

			using (var RawStream = new MemoryStream())
			{
				using (var Writer = new StreamWriter(RawStream, new UnicodeEncoding(bigEndian, true)))
				{
					Writer.Write(_TestOutput);
				}

				TempInput = RawStream.ToArray();
			}

			using (var Reader = new BinaryTextReader(TempInput, Encoding.ASCII, true))
			{
				MyResult = Reader.ReadToEnd();
			}

			Assert.AreEqual(_TestOutput, MyResult);
		}

		[Test()]
		public void ReadDetectEncodingUTF8NoBOM()
		{
			string MyResult;
			byte[] TempInput;

			using (var RawStream = new MemoryStream())
			{
				using (var Writer = new StreamWriter(RawStream, new UTF8Encoding(false)))
				{
					Writer.Write(_TestOutput);
				}

				TempInput = RawStream.ToArray();
			}

			using (var Reader = new BinaryTextReader(TempInput, Encoding.UTF8, true))
			{
				MyResult = Reader.ReadToEnd();
			}

			Assert.AreEqual(_TestOutput, MyResult);
		}

		[Test()]
		public void ReadDetectEncodingASCII()
		{
			string MyResult;
			string TempOutput;

			using (var RawStream = new MemoryStream(_TestInput))
			{
				using (var Reader = new StreamReader(RawStream, Encoding.ASCII))
				{
					TempOutput = Reader.ReadToEnd();
				}
			}

			using (var Reader = new BinaryTextReader(_TestInput, Encoding.ASCII, true))
			{
				MyResult = Reader.ReadToEnd();
			}

			Assert.AreEqual(_TestOutput, MyResult);
		}

		[Test()]
		public void ReadASCII()
		{
			string MyResult;
			string TempOutput;

			using (var RawStream = new MemoryStream(_TestInput))
			{
				using (var Reader = new StreamReader(RawStream, Encoding.ASCII, false))
				{
					TempOutput = Reader.ReadToEnd();
				}
			}

			using (var Reader = new BinaryTextReader(_TestInput, Encoding.ASCII, false))
			{
				MyResult = Reader.ReadToEnd();
			}

			Assert.AreEqual(TempOutput, MyResult);
		}

		[Test()]
		public void ReadAndReset()
		{	//****************************************
			string MyResult1, MyResult2;
			//****************************************

			using (var Reader = new BinaryTextReader(_TestInput, Encoding.UTF8, false))
			{
				MyResult1 = Reader.ReadToEnd();

				Reader.Reset();

				MyResult2 = Reader.ReadToEnd();
			}

			Assert.AreEqual(_TestOutput, MyResult1);
			Assert.AreEqual(_TestOutput, MyResult2);
		}

		[Test()]
		public void ReadAndResetUTF8()
		{	//****************************************
			string MyResult1, MyResult2;
			//****************************************

			using (var Reader = new BinaryTextReader(_TestInput, Encoding.ASCII, true))
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
