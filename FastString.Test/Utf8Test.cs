using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NUnit.Framework;

namespace FastString.Test
{
	[TestFixture]
	public class Utf8Test
	{
		#region Length
		[Test]
		public void AsciiLength()
		{
			Assert.That(new Utf8String("hello world").Length, Is.EqualTo(11));
		}

		[Test]
		public void MultibyteCharsLength()
		{
			Assert.That(new Utf8String("“hello world”").Length, Is.EqualTo(17));
		}

		[Test]
		public void MultibyteCharsBytes()
		{
			var list = new Utf8String("“hello”").Bytes;
			Assert.That(list, Is.EqualTo(new List<byte> {
				0xe2, 0x80, 0x9c,
				0x68,
				0x65,
				0x6c,
				0x6c,
				0x6f,
				0xe2, 0x80, 0x9d
			}));
		}
		#endregion Length

		#region Iteration
		[Test]
		public void IterateAsciiRange()
		{
			uint[] expected = { 0x68, 0x65, 0x6c, 0x6c, 0x6f };
			var it = new Utf8String("hello").GetEnumerator();
			for (int i = 0; i < 5; i++)
			{
				Assert.IsTrue(it.MoveNext());
				Assert.That(it.Current.Index, Is.EqualTo(i));
				Assert.That(it.Current.Value, Is.EqualTo(expected[i]));
			}
		}

		[Test]
		public void IterateMultibyteUtf8ButSingleByteUtf16()
		{
			var str = new Utf8String("“hat”");
			var it = str.GetEnumerator();
			Assert.IsTrue(it.MoveNext());
			Assert.That(it.Current.Value, Is.EqualTo(0x201c));
			Assert.IsTrue(it.MoveNext());
			Assert.That(it.Current.Value, Is.EqualTo(0x68));
			Assert.IsTrue(it.MoveNext());
			Assert.That(it.Current.Value, Is.EqualTo(0x61));
			Assert.IsTrue(it.MoveNext());
			Assert.That(it.Current.Value, Is.EqualTo(0x74));
			Assert.IsTrue(it.MoveNext());
			Assert.That(it.Current.Value, Is.EqualTo(0x201d));
			Assert.IsFalse(it.MoveNext());
		}
		#endregion Iteration

		#region Equality
		[Test]
		public void StringEqualsAscii()
		{
			Assert.That(new Utf8String("hello") == "hello", Is.True);
			Assert.That(new Utf8String("hello") == "hellp", Is.False);
		}

		[Test]
		public void StringEqualsMultibyte()
		{
			Assert.That(new Utf8String("☃hello") == "☃hello", Is.True);
			Assert.That(new Utf8String("☃hello") == "☄hello", Is.False);
		}

		[Test]
		public void StringEqualsEmoji()
		{
			Assert.That(new Utf8String("\ud83d\udca9hello") == "\ud83d\udca9hello", Is.True);
			Assert.That(new Utf8String("\ud83d\udca9hello") == "\ud83d\udcaahello", Is.False);
		}

		[Test]
		public void Utf8Equals()
		{
			Assert.That(new Utf8String("\ud83d\udca9hello☂") == new Utf8String("\ud83d\udca9hello☂"), Is.True);
			Assert.That(new Utf8String("\ud83d\udca9hello") == new Utf8String("\ud83d\udcaahello"), Is.False);
			Assert.That(new Utf8String("\ud83d\udca9hello☂") == new Utf8String("\ud83d\udca9hello☄"), Is.False);
		}
		#endregion Equality

		[Test]
		public void FromInt()
		{
			Assert.That(Utf8String.FromInt(0), Is.EqualTo(new Utf8String("0")));
			Assert.That(Utf8String.FromInt(1), Is.EqualTo(new Utf8String("1")));
			Assert.That(Utf8String.FromInt(10), Is.EqualTo(new Utf8String("10")));
			Assert.That(Utf8String.FromInt(12), Is.EqualTo(new Utf8String("12")));
			Assert.That(Utf8String.FromInt(-12), Is.EqualTo(new Utf8String("-12")));
		}

		[Test]
		public void ParseLong()
		{
			Assert.That(Utf8String.ParseLong(new Utf8String("-1")), Is.EqualTo(-1));
			Assert.That(Utf8String.ParseLong(new Utf8String("1587")), Is.EqualTo(1587));
			Assert.That(Utf8String.ParseLong(new Utf8String("777777777777")), Is.EqualTo(777777777777L));
			Assert.That(Utf8String.ParseLong(new Utf8String("1")), Is.EqualTo(1L));
			Assert.That(Utf8String.ParseLong(new Utf8String("4")), Is.EqualTo(4L));
		}

		[Test]
		public void ParseLongRadix()
		{
			Assert.That(Utf8String.ParseLong(new Utf8String("-1"), 16), Is.EqualTo(-0x1));
			Assert.That(Utf8String.ParseLong(new Utf8String("1587"), 16), Is.EqualTo(0x1587));
			Assert.That(Utf8String.ParseLong(new Utf8String("777777777777"), 16), Is.EqualTo(0x777777777777L));
		}

		[Test]
		public void TrimSingleChar()
		{
			Console.WriteLine("trimmed: [{0}]", new Utf8String("4").TrimEnd());
			Assert.That(new Utf8String("4").Trim(), Is.EqualTo(new Utf8String("4")));
		}

		[Test]
		public void Split()
		{
			var str = new Utf8String("0000;<control>;Cc;0;BN;;;;;N;NULL;;;;");
			var list = str.Split(new char[] { ';' });
			Assert.That(list, Is.EqualTo(new Utf8String[] {
				new Utf8String("0000"),
				new Utf8String("<control>"),
				new Utf8String("Cc"),
				new Utf8String("0"),
				new Utf8String("BN"),
				Utf8String.Empty,
				Utf8String.Empty,
				Utf8String.Empty,
				Utf8String.Empty,
				new Utf8String("N"),
				new Utf8String("NULL"),
				Utf8String.Empty,
				Utf8String.Empty,
				Utf8String.Empty,
				Utf8String.Empty,
			}));
		}
	}
}
