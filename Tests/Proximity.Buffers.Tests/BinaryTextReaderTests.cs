using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Proximity.Buffers.Tests;

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

		Assert.That(MyResult, Is.EqualTo(_TestOutput));
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

		Assert.That(MyResult.ToString(), Is.EqualTo(_TestOutput));
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

		Assert.That(MyResult.ToString(), Is.EqualTo(_TestOutput));
	}

	[Test()]
	public void ReadDetectEncodingUTF8()
	{
		string MyResult;

		using (var Reader = new BinaryTextReader(_TestInput, Encoding.ASCII, true))
		{
			MyResult = Reader.ReadToEnd();
		}

		Assert.That(MyResult, Is.EqualTo(_TestOutput));
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

		Assert.That(MyResult, Is.EqualTo(_TestOutput));
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

		Assert.That(MyResult, Is.EqualTo(_TestOutput));
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

		Assert.That(MyResult, Is.EqualTo(_TestOutput));
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

		Assert.That(MyResult, Is.EqualTo(_TestOutput));
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

		Assert.That(MyResult, Is.EqualTo(TempOutput));
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

		Assert.That(MyResult1, Is.EqualTo(_TestOutput));
		Assert.That(MyResult2, Is.EqualTo(_TestOutput));
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

		Assert.That(MyResult1, Is.EqualTo(_TestOutput));
		Assert.That(MyResult2, Is.EqualTo(_TestOutput));
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
		Assert.That(string.Join("\r\n", Lines), Is.EqualTo(_TestMultilineOutput));
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
		Assert.That(string.Join("\r\n", Lines), Is.EqualTo(_TestMultilineOutput));
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
}
