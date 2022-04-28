using System;
using System.Collections.Generic;
using System.Globalization;
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
	public class BinaryTextReaderTests
	{	//****************************************
		private readonly string _TestOutput = @"ABC!DEF@GHI#JKL$123%456^789&0*MNO(PQR)STU-VWX=YZ abcdefghijklmnopqrstuvwxyz ♥ ";
		private readonly string _TestMultilineOutput = @"ABC!DEF@GHI#J
KL$123%456^789&0*MNO

(PQR)STU-VWX=YZ abcdefghi
jk
lmnopqrstuvwxyz ♥ ";
		private byte[] _TestInput, _TestMultilineInput;
		//****************************************

		[OneTimeSetUp()]
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

			using (var RawStream = new MemoryStream())
			{
				using (var Writer = new StreamWriter(RawStream, Encoding.UTF8))
				{
					Writer.Write(_TestMultilineOutput);
				}

				_TestMultilineInput = RawStream.ToArray();
			}
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
					var CharsRead = Reader.ReadBlock(MyBuffer, 0, capacity);

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
					var MyChar = Reader.Read();

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
			using (var Reader = new StreamReader(RawStream, Encoding.ASCII))
			{
				TempOutput = Reader.ReadToEnd();
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
			using (var Reader = new StreamReader(RawStream, Encoding.ASCII, false))
			{
				TempOutput = Reader.ReadToEnd();
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

		[Test]
		public void ReadMultiline()
		{
			var Lines = new List<string>();

			using (var Reader = new BinaryTextReader(_TestMultilineInput, Encoding.UTF8))
			{
				while (!Reader.EndOfReader)
				{
					Lines.Add(Reader.ReadLine());
				}
			}

			// TODO: Will this fail on Linux? Maybe if the file is changed from CRLF
			Assert.AreEqual(_TestMultilineOutput, string.Join("\r\n", Lines));
		}

		[Test]
		public void ReadMultilineBuffered()
		{
			var Lines = new List<string>();
			var Buffer = new char[128];

			using (var Reader = new BinaryTextReader(_TestMultilineInput, Encoding.UTF8))
			{
				while (!Reader.EndOfReader)
				{
					var Offset = 0;
					bool Success;

					do
					{
						Success = Reader.TryReadLine(Buffer.AsSpan(Offset, 4), out var CharsWritten);

						Offset += CharsWritten;
					}
					while (!Success);

					Lines.Add(Buffer.AsSpan(0, Offset).AsString());
				}
			}

			// TODO: Will this fail on Linux? Maybe if the file is changed from CRLF
			Assert.AreEqual(_TestMultilineOutput, string.Join("\r\n", Lines));
		}

		[Test]
		public void ReadMultilineBufferedSkip()
		{
			var Buffer = new char[4];

			using (var Reader = new BinaryTextReader(_TestMultilineInput, Encoding.UTF8, false, 4))
			{
				while (!Reader.EndOfReader)
				{
					if (!Reader.TryReadLine(Buffer, out var CharsWritten))
						Reader.SkipLine();
				}
			}
		}

		[Test]
		public void TempTest()
		{
			using var RawEventFile = File.Open(@"C:\Development\Resources\Zenith\Events 134.log", FileMode.Open);

			using var RawReader = new StreamBufferReader(RawEventFile);
			using var EventReader = new BinaryTextReader(RawReader);

			var EventBuffer = new char[128];
			var TotalRecords = 0;
			var LastSequenceNumber = 1L;
			var TimeStamp = DateTimeOffset.MinValue;
			var IsStatus = false;

			while (!EventReader.EndOfReader)
			{
				var Offset = 0;

				// Decode the start of the next line into the buffer. It should be more than sufficiently large for the header
				if (!EventReader.TryReadLine(EventBuffer.AsSpan(Offset), out var CharsWritten))
					EventReader.SkipLine(); // We don't need the rest of the line data, no sense copying it

				var Line = EventBuffer.AsSpan(0, CharsWritten);

				if (Line.IsEmpty)
					continue;

				// Find the first SequenceNumber:DateTimeUtc:Type:Data divider
				var Divider = Line.IndexOf(':');

				if (Divider == -1)
					continue;

				// Process the sequence number
				if (!long.TryParse(Line.Slice(0, Divider), NumberStyles.None, CultureInfo.InvariantCulture, out var SequenceNumber))
					continue;

				TotalRecords++;
				LastSequenceNumber = SequenceNumber;
				IsStatus = false;

				// Find the next divider (end of DateTimeUtc)
				var NextDivider = Line.Slice(++Divider).IndexOf(':');

				if (NextDivider == -1)
					continue;

				if (!DateTimeOffset.TryParseExact(Line.Slice(Divider, NextDivider), "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var NewTimeStamp))
					continue;

				TimeStamp = NewTimeStamp;

				Divider += NextDivider + 1;

				// Next divider (end of Type)
				NextDivider = Line.Slice(Divider).IndexOf(':');

				var Type = Line.Slice(Divider, NextDivider);

				// Check if it's a Status event
				IsStatus = Type.SequenceEqual("Status");
			}
		}
	}
}
