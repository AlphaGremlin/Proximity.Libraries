/****************************************\
 BinaryTextWriterTests.cs
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
	public class BinaryTextWriterTests
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
		public void Write([Values(8, 16, 32, 64, 128)] int capacity)
		{
			byte[] MyResult;

			using (var Writer = new BinaryTextWriter(Encoding.UTF8, capacity))
			{
				Writer.Write(_TestInput);

				MyResult = Writer.ToArray();
			}

			CollectionAssert.AreEqual(_TestOutput, MyResult);
		}

		[Test()]
		public void WriteChar([Values(8, 16, 32, 64, 128)] int capacity)
		{
			byte[] MyResult;

			using (var Writer = new BinaryTextWriter(Encoding.UTF8, capacity))
			{
				foreach (var MyChar in _TestInput)
				{
					Writer.Write(MyChar);
				}

				MyResult = Writer.ToArray();
			}

			CollectionAssert.AreEqual(_TestOutput, MyResult);
		}
	}
}
