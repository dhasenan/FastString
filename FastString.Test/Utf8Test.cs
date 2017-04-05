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
			Assert.That(new utf8("hello world").Length, Is.EqualTo(11));
		}

		[Test]
		public void MultibyteCharsLength()
		{
			Assert.That(new utf8("“hello world”").Length, Is.EqualTo(17));
		}

		[Test]
		public void MultibyteCharsBytes()
		{
			var list = new utf8("“hello”").Bytes;
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
			var it = new utf8("hello").GetEnumerator();
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
			var str = new utf8("“hat”");
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
			Assert.That(new utf8("hello") == "hello", Is.True);
			Assert.That(new utf8("hello") == "hellp", Is.False);
		}

		[Test]
		public void StringEqualsMultibyte()
		{
			Assert.That(new utf8("☃hello") == "☃hello", Is.True);
			Assert.That(new utf8("☃hello") == "☄hello", Is.False);
		}

		[Test]
		public void StringEqualsEmoji()
		{
			Assert.That(new utf8("\ud83d\udca9hello") == "\ud83d\udca9hello", Is.True);
			Assert.That(new utf8("\ud83d\udca9hello") == "\ud83d\udcaahello", Is.False);
		}

		[Test]
		public void Utf8Equals()
		{
			Assert.That(new utf8("\ud83d\udca9hello☂") == new utf8("\ud83d\udca9hello☂"), Is.True);
			Assert.That(new utf8("\ud83d\udca9hello") == new utf8("\ud83d\udcaahello"), Is.False);
			Assert.That(new utf8("\ud83d\udca9hello☂") == new utf8("\ud83d\udca9hello☄"), Is.False);
		}
		#endregion Equality

		[Test]
		public void FromInt()
		{
			Assert.That(utf8.FromInt(0), Is.EqualTo(new utf8("0")));
			Assert.That(utf8.FromInt(1), Is.EqualTo(new utf8("1")));
			Assert.That(utf8.FromInt(10), Is.EqualTo(new utf8("10")));
			Assert.That(utf8.FromInt(12), Is.EqualTo(new utf8("12")));
			Assert.That(utf8.FromInt(-12), Is.EqualTo(new utf8("-12")));
		}

		[Test]
		public void ParseLong()
		{
			Assert.That(utf8.ParseLong(new utf8("-1")), Is.EqualTo(-1));
			Assert.That(utf8.ParseLong(new utf8("1587")), Is.EqualTo(1587));
			Assert.That(utf8.ParseLong(new utf8("777777777777")), Is.EqualTo(777777777777L));
		}

		[Test]
		public void ParseLongRadix()
		{
			Assert.That(utf8.ParseLong(new utf8("-1"), 16), Is.EqualTo(-0x1));
			Assert.That(utf8.ParseLong(new utf8("1587"), 16), Is.EqualTo(0x1587));
			Assert.That(utf8.ParseLong(new utf8("777777777777"), 16), Is.EqualTo(0x777777777777L));
		}
	}
}
