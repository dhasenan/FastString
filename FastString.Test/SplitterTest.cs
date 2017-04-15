using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FastString.Test
{
	[TestFixture]
	public class SplitterTest
	{
		[Test]
		public void Split()
		{
			var str = new Utf8String(";a;b;cdef;;k;");
			var target = new Splitter(';', str);
			var parts = target.ToList();

			Assert.That(parts, Is.EqualTo(new List<Utf8String> {
				Utf8String.Empty,
				new Utf8String("a"),
				new Utf8String("b"),
				new Utf8String("cdef"),
				Utf8String.Empty,
				new Utf8String("k"),
				Utf8String.Empty
			}));
		}

		[Test]
		public void OnlySeparator()
		{
			var str = new Utf8String(";;;;;");
			Assert.That(new Splitter(';', str).ToList(), Is.EqualTo(new List<Utf8String> {
				Utf8String.Empty,
				Utf8String.Empty,
				Utf8String.Empty,
				Utf8String.Empty,
				Utf8String.Empty,
				Utf8String.Empty,
			}));
		}

		[Test]
		public void Vexing()
		{
			var str = new Utf8String("0000;<control>;Cc;0;BN;;;;;N;NULL;;;;");
			var list = new Splitter(';', str).ToList();
			foreach (var c in list)
			{
				System.Console.WriteLine(c);
			}
			Assert.That(list, Is.EqualTo(new List<Utf8String> {
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
