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
			writer.AppendFormat(new utf8("Hello {0}!"), "world");
			Assert.That(Bake(), Is.EqualTo(new utf8("Hello world!")));
		}

		[Test]
		public void AppendFormatNumber()
		{
			writer.AppendFormat(new utf8("Hello {0}!"), 12);
			Assert.That(Bake(), Is.EqualTo(new utf8("Hello 12!")));
		}

		[Test]
		public void AppendFormatNumberWithOptions()
		{
			writer.AppendFormat(new utf8("Hello {0:X4}!"), 12);
			Assert.That(Bake(), Is.EqualTo(new utf8("Hello 000C!")));
		}

		[Test]
		public void AppendFormatSeveralArgs()
		{
			writer.AppendFormat(new utf8("Hello {1}, {0:X4} times {2}!"), 12, "world", new MockToString("over"));
			Assert.That(Bake(), Is.EqualTo(new utf8("Hello world, 000C times over!")));
		}

		[Test]
		public void AppendFormatEscape()
		{
			writer.AppendFormat(new utf8("Hello {{0}{0}!"), "world");
			Assert.That(Bake(), Is.EqualTo(new utf8("Hello {0}world!")));
		}

		utf8 Bake()
		{
			var b = new utf8(
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
