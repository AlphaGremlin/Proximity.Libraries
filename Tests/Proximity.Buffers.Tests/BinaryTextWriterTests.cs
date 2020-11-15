using System;
using System.Buffers;
using System.IO;
using System.Text;
using NUnit.Framework;
//****************************************

namespace Proximity.Buffers.Tests
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

		[OneTimeSetUp()]
		public void Setup()
		{
			using (var RawStream = new MemoryStream())
			{
				using (var Writer = new StreamWriter(RawStream, Encoding.UTF8))
				{
					Writer.Write(_TestInput);
				}

				_TestOutput = RawStream.ToArray();
			}
		}

		//****************************************

		[Test()]
		public void WriteDefault()
		{	//****************************************
			byte[] MyResult;
			byte[] TempOutput;
			//****************************************

			using (var RawStream = new MemoryStream())
			{
				using (var Writer = new StreamWriter(RawStream))
				{
					Writer.Write(_TestInput);
				}

				TempOutput = RawStream.ToArray();
			}

			//****************************************

			using (var Buffer = new BufferWriter<byte>())
			using (var Writer = new BinaryTextWriter(Buffer))
			{
				Writer.Write(_TestInput);

				Writer.Flush();

				MyResult = Buffer.ToSequence().ToArray();
			}

			//****************************************

			CollectionAssert.AreEqual(TempOutput, MyResult);
		}

		[Test()]
		public void Write([Values(8, 16, 32, 64, 128)] int capacity)
		{	//****************************************
			byte[] MyResult;
			//****************************************

			using (var Buffer = new BufferWriter<byte>())
			using (var Writer = new BinaryTextWriter(Buffer, Encoding.UTF8, capacity))
			{
				Writer.Write(_TestInput);

				Writer.Flush();

				MyResult = Buffer.ToSequence().ToArray();
			}

			//****************************************

			CollectionAssert.AreEqual(_TestOutput, MyResult);
		}

		[Test()]
		public void WriteChar([Values(8, 16, 32, 64, 128)] int capacity)
		{	//****************************************
			byte[] MyResult;
			//****************************************

			using (var Buffer = new BufferWriter<byte>())
			using (var Writer = new BinaryTextWriter(Buffer, Encoding.UTF8, capacity))
			{
				foreach (var MyChar in _TestInput)
				{
					Writer.Write(MyChar);
				}

				Writer.Flush();

				MyResult = Buffer.ToSequence().ToArray();
			}

			//****************************************

			CollectionAssert.AreEqual(_TestOutput, MyResult);
		}

		[Test()]
		public void WriteUTF8()
		{	//****************************************
			byte[] MyResult;
			//****************************************

			using (var Buffer = new BufferWriter<byte>())
			using (var Writer = new BinaryTextWriter(Buffer, Encoding.UTF8))
			{
				Writer.Write(_TestInput);

				Writer.Flush();

				MyResult = Buffer.ToSequence().ToArray();
			}

			//****************************************

			CollectionAssert.AreEqual(_TestOutput, MyResult);
		}

		[Test()]
		public void WriteUTF32([Values(false, true)] bool bigEndian)
		{	//****************************************
			byte[] MyResult;
			byte[] TempOutput;
			var MyEncoding = new UTF32Encoding(bigEndian, true);
			//****************************************

			using (var RawStream = new MemoryStream())
			{
				using (var Writer = new StreamWriter(RawStream, MyEncoding))
				{
					Writer.Write(_TestInput);
				}

				TempOutput = RawStream.ToArray();
			}

			//****************************************

			using (var Buffer = new BufferWriter<byte>())
			using (var Writer = new BinaryTextWriter(Buffer, MyEncoding))
			{
				Writer.Write(_TestInput);

				Writer.Flush();

				MyResult = Buffer.ToSequence().ToArray();
			}

			//****************************************

			CollectionAssert.AreEqual(TempOutput, MyResult);
		}

		[Test()]
		public void WriteUnicode([Values(false, true)] bool bigEndian)
		{	//****************************************
			byte[] MyResult;
			byte[] TempOutput;
			var MyEncoding = new UnicodeEncoding(bigEndian, true);
			//****************************************

			using (var RawStream = new MemoryStream())
			{
				using (var Writer = new StreamWriter(RawStream, MyEncoding))
				{
					Writer.Write(_TestInput);
				}

				TempOutput = RawStream.ToArray();
			}

			//****************************************

			using (var Buffer = new BufferWriter<byte>())
			using (var Writer = new BinaryTextWriter(Buffer, MyEncoding))
			{
				Writer.Write(_TestInput);

				Writer.Flush();

				MyResult = Buffer.ToSequence().ToArray();
			}

			//****************************************

			CollectionAssert.AreEqual(TempOutput, MyResult);
		}

		[Test()]
		public void WriteASCII()
		{	//****************************************
			byte[] MyResult;
			byte[] TempOutput;
			//****************************************

			using (var RawStream = new MemoryStream())
			{
				using (var Writer = new StreamWriter(RawStream, Encoding.ASCII))
				{
					Writer.Write(_TestInput);
				}

				TempOutput = RawStream.ToArray();
			}

			//****************************************

			using (var Buffer = new BufferWriter<byte>())
			using (var Writer = new BinaryTextWriter(Buffer, Encoding.ASCII))
			{
				Writer.Write(_TestInput);

				Writer.Flush();

				MyResult = Buffer.ToSequence().ToArray();
			}

			//****************************************

			CollectionAssert.AreEqual(TempOutput, MyResult);
		}
	}
}
