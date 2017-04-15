using System;
using System.IO;

using NUnit.Framework;

namespace FastString.Test
{
	[TestFixture]
	public class Utf8WriterTest
	{
		Utf8Writer writer;
		MemoryStream stream;

		[SetUp]
		public void Setup()
		{
			stream = new MemoryStream();
			writer = new Utf8Writer(stream);
		}

		[Test]
		public void AppendFormatSimple()
		{
			writer.AppendFormat(new Utf8String("Hello {0}!"), "world");
			Assert.That(Bake(), Is.EqualTo(new Utf8String("Hello world!")));
		}

		[Test]
		public void AppendFormatNumber()
		{
			writer.AppendFormat(new Utf8String("Hello {0}!"), 12);
			Assert.That(Bake(), Is.EqualTo(new Utf8String("Hello 12!")));
		}

		[Test]
		public void AppendFormatNumberWithOptions()
		{
			writer.AppendFormat(new Utf8String("Hello {0:X4}!"), 12);
			Assert.That(Bake(), Is.EqualTo(new Utf8String("Hello 000C!")));
		}

		[Test]
		public void AppendFormatSeveralArgs()
		{
			writer.AppendFormat(new Utf8String("Hello {1}, {0:X4} times {2}!"), 12, "world", new MockToString("over"));
			Assert.That(Bake(), Is.EqualTo(new Utf8String("Hello world, 000C times over!")));
		}

		[Test]
		public void AppendFormatEscape()
		{
			writer.AppendFormat(new Utf8String("Hello {{0}{0}!"), "world");
			Assert.That(Bake(), Is.EqualTo(new Utf8String("Hello {0}world!")));
		}

		Utf8String Bake()
		{
			var b = new Utf8String(
				new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Position));
			Console.WriteLine(b.Length);
			Console.WriteLine("[{0}]", b);
			return b;
		}
	}

	class MockToString
	{
		private string str;
		public MockToString(string str)
		{
			this.str = str;
		}
		public override string ToString()
		{
			return str;
		}
	}
}
