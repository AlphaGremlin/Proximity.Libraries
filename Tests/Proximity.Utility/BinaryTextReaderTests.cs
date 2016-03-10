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
		private string _TestInput = @"ABC!DEF@GHI#JKL$123%456^789&0*MNO(PQR)STU-VWX=YZ abcdefghijklmnopqrstuvwxyz ♥ ";
		private byte[] _TestOutput;
		//****************************************

		[TestFixtureSetUp()]
		public void Setup()
		{
			_TestOutput = Encoding.UTF8.GetBytes(_TestInput);
		}

		//****************************************

		[Test()]
		public void ReadToEnd()
		{
			string MyResult;

			using (var Reader = new BinaryTextReader(_TestOutput, Encoding.UTF8))
			{
				MyResult = Reader.ReadToEnd();
			}

			Assert.AreEqual(_TestInput, MyResult);
		}

		[Test()]
		public void ReadBlock([Values(8, 16, 32, 64, 128)] int capacity)
		{
			var MyResult = new StringBuilder();
			var MyBuffer = new char[capacity];

			using (var Reader = new BinaryTextReader(_TestOutput, Encoding.UTF8))
			{
				for (; ; )
				{
					int CharsRead = Reader.ReadBlock(MyBuffer, 0, capacity);

					if (CharsRead == 0)
						break;

					MyResult.Append(MyBuffer, 0, CharsRead);
				}
			}

			Assert.AreEqual(_TestInput, MyResult.ToString());
		}

		[Test()]
		public void Read()
		{
			var MyResult = new StringBuilder();

			using (var Reader = new BinaryTextReader(_TestOutput, Encoding.UTF8))
			{
				for (; ; )
				{
					int MyChar = Reader.Read();

					if (MyChar == -1)
						break;

					MyResult.Append(char.ConvertFromUtf32(MyChar));
				}
			}

			Assert.AreEqual(_TestInput, MyResult.ToString());
		}
	}
}
