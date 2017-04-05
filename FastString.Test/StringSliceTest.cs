using System;
using System.Collections;
using NUnit.Framework;

namespace FastString.Test
{
	[TestFixture]
	public class StringSliceTest
	{
		[Test]
		public void HasValueNoEmptyString()
		{
			Assert.That(new StringSlice("").HasValue, Is.False);
		}

		StringSlice notInitialized;
		[Test]
		public void HasValueNotInitialized()
		{
			Assert.That(notInitialized.HasValue, Is.False);
		}

		[Test]
		public void HasValueNoEmptySliceOfString()
		{
			Assert.That(new StringSlice("hi there", 5, 5).HasValue, Is.False);
		}

		[Test]
		[Ignore]
		public void CreateLongerThanString()
		{
			bool success;
			try
			{
				new StringSlice("hi there", 5, 12);
				success = true;
			}
			catch (Exception e)
			{
				success = false;
			}
			Assert.IsFalse(success);
		}

		[Test]
		public void Length()
		{
			var raw = "hello world";
			var str = new StringSlice(raw);
			Assert.That(str.Length, Is.EqualTo(raw.Length));
		}

		[Test]
		public void StartsWithFullString()
		{
			var raw = "hello world";
			var str = new StringSlice(raw);
			Assert.That(str.StartsWith("hello"), Is.True);
			Assert.That(str.StartsWith("world"), Is.False);
		}

		[Test]
		public void StartsWithPartialString()
		{
			var raw = "hello world";
			var str = new StringSlice(raw, 6, raw.Length);
			Assert.That(str.StartsWith("world"), Is.True);
			Assert.That(str.StartsWith("hello"), Is.False);
		}

		[Test]
		public void StartsWithBeyondEnd()
		{
			var raw = "hello world";
			var str = new StringSlice(raw, 6, 9);
			Assert.That(str.StartsWith("world"), Is.False);
		}

		[Test]
		public void Substring()
		{
			var str = new StringSlice("hello cruel world!");
			var sub = str.Substring(6, 5);
			Assert.That(sub.ToString(), Is.EqualTo("cruel"));
		}

		[Test]
		public void IndexOfAny()
		{
			var str = new StringSlice("hello cruel world!");
			Assert.That(str.IndexOfAny(new[] { 'r', 'w', 'd' }), Is.EqualTo(7));
			str = str.Substring(5);
			Assert.That(str.IndexOfAny(new[] { 'o', 'w', 'd' }), Is.EqualTo(7));
		}

		[Test]
		public void IndexOfChar()
		{
			var str = new StringSlice("hello cruel world!");
			Assert.That(str.IndexOf('r'), Is.EqualTo(7));
			str = str.Substring(5);
			Assert.That(str.IndexOf('o'), Is.EqualTo(8));
		}

		[Test]
		public void IndexOfString()
		{
			var str = new StringSlice("hello cruel world!");
			Assert.That(str.IndexOf("cru"), Is.EqualTo(6));
			str = str.Substring(5);
			Assert.That(str.IndexOf("rld"), Is.EqualTo(9));
		}

		[Test]
		public void SplitOnCharacter()
		{
			var str = new StringSlice("hello cruel world!");
			var parts = str.Split(' ', 5);
			Assert.That(parts, Is.EqualTo(new[]{
					new StringSlice("hello"),
					new StringSlice("cruel"),
					new StringSlice("world!")
			}));
			parts = str.Split(' ', 2);
			Assert.That(parts, Is.EqualTo(new[]{
					new StringSlice("hello"),
					new StringSlice("cruel world!")
			}));
		}
	}
}
