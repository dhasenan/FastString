using System;
using FastString.Unicode;
using NUnit.Framework;

namespace FastString.Test
{
	[TestFixture]
	public class CharInfoSmokeTest
	{
		[SetUp]
		public void Setup()
		{
			CharInfo.LoadCharacterData();
			CharInfo.LoadCharacterNames();
		}

		[Test]
		public void AsciiTab()
		{
			var ci = CharInfo.For('\t').Value;
			Assert.That(ci.Name, Is.EqualTo(new utf8("<control>")), ci.Name.ToString());
		}

		[Test]
		public void AsciiDollar()
		{
			var ci = CharInfo.For('$').Value;
			Assert.That(ci.Codepoint, Is.EqualTo((uint)0x24));
			Assert.That(ci.Name, Is.EqualTo(new utf8("DOLLAR SIGN")), ci.Name.ToString());
			Assert.That(ci.Category, Is.EqualTo(UnicodeCategory.SymbolCurrency));
		}

		[Test]
		public void NearTop()
		{
			Assert.NotNull(CharInfo.For(0x100000));
			Assert.NotNull(CharInfo.For(0x10FFFD));
		}
	}
}
