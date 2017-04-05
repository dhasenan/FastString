using System;
using NUnit.Framework;

namespace FastString.Test
{
	[TestFixture]
	public class ReverseUtf8EnumeratorTest
	{
		[Test]
		public void Simple()
		{
			var str = new utf8("Hi!\n");
			var it = new ReverseUtf8Enumerator(str);
			it.MoveNext();
			Assert.That(it.Current.Value, Is.EqualTo((int)'\n'));
			it.MoveNext();
			Assert.That(it.Current.Value, Is.EqualTo((int)'!'));
			it.MoveNext();
			Assert.That(it.Current.Value, Is.EqualTo((int)'i'));
			it.MoveNext();
			Assert.That(it.Current.Value, Is.EqualTo((int)'H'));
		}
	}
}
