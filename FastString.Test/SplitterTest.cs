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
			var str = new utf8(";a;b;cdef;;k;");
			var target = new Splitter(';', str);
			var parts = target.ToList();

			Assert.That(parts, Is.EqualTo(new List<utf8> {
				utf8.Empty,
				new utf8("a"),
				new utf8("b"),
				new utf8("cdef"),
				utf8.Empty,
				new utf8("k"),
				utf8.Empty
			}));
		}

		[Test]
		public void OnlySeparator()
		{
			var str = new utf8(";;;;;");
			Assert.That(new Splitter(';', str).ToList(), Is.EqualTo(new List<utf8> {
				utf8.Empty,
				utf8.Empty,
				utf8.Empty,
				utf8.Empty,
				utf8.Empty,
				utf8.Empty,
			}));
		}

		[Test]
		public void Vexing()
		{
			var str = new utf8("0000;<control>;Cc;0;BN;;;;;N;NULL;;;;");
			var list = new Splitter(';', str).ToList();
			foreach (var c in list)
			{
				System.Console.WriteLine(c);
			}
			Assert.That(list, Is.EqualTo(new List<utf8> {
				new utf8("0000"),
				new utf8("<control>"),
				new utf8("Cc"),
				new utf8("0"),
				new utf8("BN"),
				utf8.Empty,
				utf8.Empty,
				utf8.Empty,
				utf8.Empty,
				new utf8("N"),
				new utf8("NULL"),
				utf8.Empty,
				utf8.Empty,
				utf8.Empty,
				utf8.Empty,
			}));
		}
	}
}
