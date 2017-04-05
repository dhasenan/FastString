using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Text;

namespace FastString
{
	// There are some duplicate implementations and almost duplicate implementations in here.
	// This is mainly to make things a little faster.
	// For instance, ToUpper and ToLower are *almost* identical. I could pass a delegate into a shared
	// implementation, but that results in the allocation of a delegate and an indirect call.
	// On the other hand, we do keep as much as possible using Utf8Enumerator.


	/// <summary>
	/// A UTF8-encoded string.
	/// </summary>
	/// <remarks>
	/// System.String is a class wrapping a UTF16 encoded character array. Since UTF8 is widespread,
	/// this often incurs a transcoding cost and additional memory usage. Furthermore, System.String
	/// copies data frequently.
	///
	/// FastString.utf8 is a struct wrapping a UTF8 encoded array segment. This allows it to operate
	/// without copying memory or incurring any garbage collection under normal operation and should
	/// in general be more memory efficient.
	///
	/// By default, utf8 keeps a reference to the input array. If you need a small amount of data
	/// extracted from a much larger array, use the Clone method. Likewise, if the underlying buffer
	/// changes, use the Clone method.
	///
	/// utf8 uses the UTF8 encoding. This means there is no one-to-one correspondence between bytes
	/// and characters. If you need to iterate by codepoint, use the iterator explicitly. If you need
	/// to index by UTF coedpoints, use UTF32.
	/// </remarks>
	public struct utf8 : IEnumerable<UtfIndex>
	{
		/// <summary>
		/// The empty string.
		/// </summary>
		public static readonly utf8 Empty = new utf8("");

		internal readonly ArraySegment<byte> _bytes;

		/// <summary>
		/// Create a utf8 string from a System.String.
		/// </summary>
		public utf8(string str)
		{
			_bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(str));
		}

		/// <summary>
		/// Create a utf8 string from a series of bytes representing UTF8 text.
		/// </summary>
		/// <remarks>
		/// If the underlying byte array changes, the data held in this utf8 will change. You can use
		/// the CloneFrom static method or call Clone on a created utf8 for safety.
		/// </remarks>
		public utf8(ArraySegment<byte> utf8Bytes)
		{
			// TODO validate
			// The CLR System.Encoding classes don't have a Validate method.
			// We need to do that manually.
			_bytes = utf8Bytes;
		}

		/// <summary>
		/// Create a utf8 string from a series of bytes representing UTF8 text.
		/// </summary>
		/// <remarks>
		/// If the underlying byte array changes, the data held in this utf8 will change. You can use
		/// the CloneFrom static method or call Clone on a created utf8 for safety. You know best.
		/// </remarks>
		public utf8(byte[] utf8Bytes) : this(new ArraySegment<byte>(utf8Bytes))
		{
		}

		/// <summary>
		/// Get the length in bytes of this utf8 string.
		/// </summary>
		public int Length
		{
			get { return _bytes.Count; }
		}

		/// <summary>
		/// Get the underlying UTF-8 data for this string.
		/// </summary>
		/// <remarks>
		/// It's awkward to work with UTF-8 as bytes, but this might be handy.
		/// </remarks>
		public IReadOnlyList<byte> Bytes
		{
			get { return _bytes; }
		}

		public void CopyTo(byte[] target)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(byte[] target, int offset, int count)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Create a copy of this utf8 string.
		/// </summary>
		/// <remarks>
		/// You may wish to use this to reduce memory usage by allowing a large buffer to be garbage
		/// collected. You may wish to use this method if you created a utf8 struct from a buffer that
		/// will be mutated.
		///
		/// This method allocates memory.
		/// </remarks>
		public utf8 Clone()
		{
			var b = new byte[_bytes.Count];
			Array.Copy(_bytes.Array, _bytes.Offset, b, 0, _bytes.Count);
			return new utf8(b);
		}

		/// <summary>
		/// Create a string representing the portion of the current string in the given range.
		/// </summary>
		/// <remarks>
		/// This method does not allocate memory.
		///
		/// The range is given as byte offsets. A codepoint will often correspond to multiple bytes.
		/// This method makes some effort to detect and throw if you attempt to take a substring in the
		/// middle of a character.
		/// </remarks>
		public utf8 Substring(int start, int length)
		{
			Contract.Assert(_bytes.Count >= start + length);
			Contract.Assert(start >= 0);
			Contract.Assert(length >= 0);
			if (length > 0)
			{
				Contract.Assert(
					(this[start] & 0xC0) == 0xC0,
						"tried to take a substring in the middle of a character");
			}

			return new utf8(
					new ArraySegment<byte>(
						_bytes.Array,
						_bytes.Offset + start,
						length));
		}

		/// <summary>
		/// Create a string representing the portion of the current string, starting at the given
		/// offset.
		/// </summary>
		/// <remarks>
		/// The start is given as a byte offset. A codepoint will often correspond to multiple bytes.
		/// This method makes some effort to detect and throw if you attempt to take a substring in the
		/// middle of a character.
		///
		/// This method does not allocate memory.
		/// </remarks>
		public utf8 Substring(int start)
		{
			Contract.Assert(_bytes.Count >= start);
			Contract.Assert(start >= 0);
			if (_bytes.Count < start)
			{
				Contract.Assert(
					(this[start] & 0xC0) == 0xC0,
						"tried to take a substring in the middle of a character");
			}

			return new utf8(
					new ArraySegment<byte>(
						_bytes.Array,
						_bytes.Offset + start,
						_bytes.Count - start));
		}

		/// <summary>
		/// Count the number of Unicode codepoints in this string.
		/// </summary>
		/// <remarks>
		/// This is primarily useful if you need to transcode to UTF32.
		///
		/// By traversing the underlying data, this method counts the total number of codepoints it
		/// contains. Its time complexity is linear in the length of the string.
		///
		/// It does not validate the data.
		///
		/// A codepoint does not map directly to a spacing character. For instance, the string "a\u0301"
		/// contains two codepoints but prints as one spacing character -- á.
		///
		/// This method does not allocate memory.
		/// </remarks>
		public int CountCodepoints()
		{
			int count = 0;
			for (int i = 0; i < _bytes.Count; i++)
			{
				var c = this[i];
				var h = c & 0xC0 >> 6;
				if (h != 2)
				{
					count++;
				}
			}
			return count;
		}

		/// <summary>
		/// Create a string consisting of this one, minus whitespace on either end.
		/// </summary>
		/// <remarks>
		/// This method does not allocate.
		/// </remarks>
		public utf8 Trim()
		{
			return this.TrimEnd().TrimStart();
		}

		/// <summary>
		/// Create a string consisting of this one, minus any trailing whitespace.
		/// </summary>
		/// <remarks>
		/// This method does not allocate.
		/// </remarks>
		public utf8 TrimEnd()
		{
			var it = new ReverseUtf8Enumerator(this);
			while (it.MoveNext())
			{
				if (!char.IsWhiteSpace((char)it.Current.Value))
				{
					return Substring(0, it.Current.Index);
				}
			}
			return utf8.Empty;
		}

		/// <summary>
		/// Create a string consisting of this one, minus any leading whitespace.
		/// </summary>
		/// <remarks>
		/// This method does not allocate.
		/// </remarks>
		public utf8 TrimStart()
		{
			var it = new Utf8Enumerator(this);
			while (it.MoveNext())
			{
				if (!char.IsWhiteSpace((char)it.Current.Value))
				{
					return Substring(it.Current.Index);
				}
			}
			return utf8.Empty;
		}

		/// <summary>
		/// Split this string based on the given separators (UTF codepoints).
		/// </summary>
		/// <remarks>
		/// You will not be able to split on certain characters (for instance, emoji).
		///
		/// This allocates for the output array, but does not copy string data.
		/// </remarks>
		public utf8[] Split(char[] splitOn)
		{
			var points = new List<utf8>();
			var last = 0;
			var it = new Utf8Enumerator(this);
			while (it.MoveNext())
			{
				for (int i = 0; i < splitOn.Length; i++)
				{
					if (it.Current.Value == (uint)splitOn[i])
					{
						points.Add(Substring(last, it.Current.Index - last));
						last = it.Current.Index;
					}
				}
			}
			points.Add(Substring(last, it.Current.Index - last));
			return points.ToArray();
		}

		/// <summary>
		/// Split this string based on the given separators (UTF codepoints).
		/// </summary>
		/// <remarks>
		/// Want to split a string based on the poop emoji character? Now you can! And not as a string!
		///
		/// This allocates for the output array, but does not copy string data.
		/// </remarks>
		public IList<utf8> Split(uint[] splitOn)
		{
			var points = new List<utf8>();
			var last = 0;
			var it = new Utf8Enumerator(this);
			while (it.MoveNext())
			{
				for (int i = 0; i < splitOn.Length; i++)
				{
					if (it.Current.Value == splitOn[i])
					{
						points.Add(Substring(last, it.Current.Index - last));
						last = it.Current.Index;
					}
				}
			}
			points.Add(Substring(last, it.Current.Index - last));
			return points;
		}

		public bool IsNullOrWhitespace { get { return Trim().IsEmpty; } }
		public bool IsNullOrEmpty { get { return Length == 0; } }
		public bool IsEmpty { get { return Length == 0; } }

		/// <summary>
		/// Determine whether this string begins with the target string.
		/// </summary>
		public bool StartsWith(utf8 other)
		{
			if (other.Length > Length) return false;
			for (int i = 0; i < other.Length; i++)
			{
				if (this[i] != other[i]) return false;
			}
			return true;
		}

		internal byte this[int i]
		{
			get { return _bytes.Array[_bytes.Offset + i]; }
		}

		/// <summary>
		/// Determine whether this string ends with the target string.
		/// </summary>
		public bool EndsWith(utf8 other)
		{
			if (other.Length > Length) return false;
			for (int i = other.Length - 1; i <= 0; i--)
			{
				if (this[Length - i] != other[i]) return false;
			}
			return true;
		}

		/// <summary>
		/// Determine whether this string contains the target string.
		/// </summary>
		public bool Contains(utf8 other)
		{
			return IndexOf(other) >= 0;
		}

		/// <summary>
		/// Locate the target string within this string.
		/// </summary>
		public int IndexOf(utf8 other)
		{
			// Naive approach.
			var end = Length - other.Length;
			for (int i = 0; i < end; i++)
			{
				for (int j = 0; j < other.Length; j++)
				{
					if (this[i + j] != other[j])
					{
						goto next;
					}
				}
				return i;
next: {}
			}
			return -1;
		}

		public bool StartsWith(string other)
		{
			var it = new Utf8Enumerator(this);
			for (int i = 0; i < other.Length; i++)
			{
				if (!it.MoveNext()) return false;
				var c = other[i];
				uint codepoint;
				if (char.IsSurrogate(c))
				{
					i++;
					codepoint = (uint)char.ConvertToUtf32(c, other[i]);
				}
				else
				{
					codepoint = (uint)c;
				}
				if (codepoint != it.Current.Value)
				{
					return false;
				}
			}
			return true;
		}

		public bool EndsWith(string other)
		{
			var it = new ReverseUtf8Enumerator(this);
			for (int i = other.Length - 1; i >= 0; i--)
			{
				if (!it.MoveNext()) return false;
				var c = other[i];
				uint codepoint;
				if (char.IsLowSurrogate(c))
				{
					i--;
					codepoint = (uint)char.ConvertToUtf32(other[i], c);
				}
				else
				{
					codepoint = (uint)c;
				}
				if (codepoint != it.Current.Value)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Determine whether the given System.String exists in this string.
		/// </summary>
		public bool Contains(string other)
		{
			return IndexOf(other) >= 0;
		}

		/// <summary>
		/// Locate the given System.String in this string.
		/// </summary>
		public int IndexOf(string other)
		{
			var it = new Utf8Enumerator(this);
			while (it.MoveNext())
			{
				if (Length - it.Current.Index < other.Length)
				{
					break;
				}
				if (Substring(it.Current.Index).StartsWith(other))
				{
					return it.Current.Index;
				}
			}
			return -1;
		}

		/// <summary>
		/// Locate the given Unicode codepoint in this string.
		/// </summary>
		/// <remarks>
		/// So you can find all the poop emoji.
		/// </remarks>
		public int IndexOf(uint codepoint)
		{
			var it = new Utf8Enumerator(this);
			while (it.MoveNext())
			{
				if (it.Current.Value == codepoint)
				{
					return it.Current.Index;
				}
			}
			return -1;
		}

		/// <summary>
		/// Locate the given character in this string.
		/// </summary>
		/// <remarks>
		/// The character must be represented as a single UTF16 code unit -- so no emoji.
		/// </remarks>
		public int IndexOf(char c)
		{
			return IndexOf((uint)c);
		}

		/// <summary>
		/// Locate the first instance of a character in the given list within this string.
		/// </summary>
		public int IndexOfAny(params char[] chars)
		{
			var it = new Utf8Enumerator(this);
			while (it.MoveNext())
			{
				for (int i = 0; i < chars.Length; i++)
				{
					if (it.Current.Value == chars[i]) return it.Current.Index;
				}
			}
			return -1;
		}

		/// <summary>
		/// Create a copy of this string converted to uppercase, using the current culture.
		/// </summary>
		/// <remarks>
		/// This obviously allocates a new array to hold the uppercase data.
		///
		/// The length of the output is not necessarily the same as the length of the input.
		/// </remarks>
		public utf8 ToUpper()
		{
			return ToUpper(CultureInfo.CurrentCulture);
		}

		/// <summary>
		/// Create a copy of this string converted to uppercase, using the invariant culture.
		/// </summary>
		/// <remarks>
		/// This obviously allocates a new array to hold the uppercase data.
		///
		/// The length of the output is not necessarily the same as the length of the input.
		/// </remarks>
		public utf8 ToUpperInvariant()
		{
			return ToUpper(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Create a copy of this string converted to uppercase.
		/// </summary>
		/// <remarks>
		/// This obviously allocates a new array to hold the uppercase data.
		///
		/// The length of the output is not necessarily the same as the length of the input.
		/// </remarks>
		public utf8 ToUpper(CultureInfo info)
		{
			if (Length == 0) return Empty;

			var ch = new char[1];
			var it = new Utf8Enumerator(this);
			int len = 0;
			while (it.MoveNext())
			{
				if (it.Current.Value <= 0x1FFFF)
				{
					var lc = char.ToUpper((char)it.Current.Value);
					ch[0] = lc;
					len += Encoding.UTF8.GetByteCount(ch);
				}
				else
				{
					// We're in emoji land, so no upper/lowercase.
					// (This will fail in the future should unicode get codepoints representing
					// upper/lowercase characters in this range.)
					len += it.Current.EncodedLength;
				}
			}

			it.Reset();

			var buf = new byte[len];
			while (it.MoveNext())
			{
				if (it.Current.Value <= 0x1FFFF)
				{
					var lc = char.ToUpper((char)it.Current.Value);
					ch[0] = lc;
					len += Encoding.UTF8.GetBytes(ch, 0, 1, buf, len);
				}
				else
				{
					// We're in emoji land, so no upper/lowercase.
					// (This will fail in the future should unicode get codepoints representing
					// upper/lowercase characters in this range.)
					for (int i = 0; i < it.Current.EncodedLength; i++)
					{
						buf[len] = this[i + it.Current.Index];
						len++;
					}
				}
			}

			return new utf8(buf);
		}

		/// <summary>
		/// Create a copy of this string converted to lowercase, using the current culture.
		/// </summary>
		/// <remarks>
		/// This obviously allocates a new array to hold the lowercase data.
		///
		/// The length of the output is not necessarily the same as the length of the input.
		/// </remarks>
		public utf8 ToLower()
		{
			return ToLower(CultureInfo.CurrentCulture);
		}

		/// <summary>
		/// Create a copy of this string converted to lowercase, using the invariant culture.
		/// </summary>
		/// <remarks>
		/// This obviously allocates a new array to hold the lowercase data.
		///
		/// The length of the output is not necessarily the same as the length of the input.
		/// </remarks>
		public utf8 ToLowerInvariant()
		{
			return ToLower(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Create a copy of this string converted to lowercase.
		/// </summary>
		/// <remarks>
		/// This obviously allocates a new array to hold the lowercase data.
		///
		/// The length of the output is not necessarily the same as the length of the input.
		/// </remarks>
		public utf8 ToLower(CultureInfo info)
		{
			if (Length == 0) return Empty;

			var ch = new char[1];
			var it = new Utf8Enumerator(this);
			int len = 0;
			while (it.MoveNext())
			{
				if (it.Current.Value <= 0x1FFFF)
				{
					var lc = char.ToLower((char)it.Current.Value);
					ch[0] = lc;
					len += Encoding.UTF8.GetByteCount(ch);
				}
				else
				{
					// We're in emoji land, so no upper/lowercase.
					// (This will fail in the future should unicode get codepoints representing
					// upper/lowercase characters in this range.)
					len += it.Current.EncodedLength;
				}
			}

			it.Reset();

			var buf = new byte[len];
			while (it.MoveNext())
			{
				if (it.Current.Value <= 0x1FFFF)
				{
					var lc = char.ToLower((char)it.Current.Value);
					ch[0] = lc;
					len += Encoding.UTF8.GetBytes(ch, 0, 1, buf, len);
				}
				else
				{
					// We're in emoji land, so no upper/lowercase.
					// (This will fail in the future should unicode get codepoints representing
					// upper/lowercase characters in this range.)
					for (int i = 0; i < it.Current.EncodedLength; i++)
					{
						buf[len] = this[i + it.Current.Index];
						len++;
					}
				}
			}

			return new utf8(buf);
		}

		/// <summary>
		/// Convert this utf8 string into a System.String.
		/// </summary>
		/// <remarks>
		/// This allocates, obviously, and also validates the string if you have configured
		/// System.Text.Encoding.UTF8 to detect errors.
		/// </remarks>
		public override string ToString()
		{
			return Encoding.UTF8.GetString(_bytes.Array, _bytes.Offset, _bytes.Count);
		}

		/// <summary>
		/// Check the equality with the given object.
		/// </summary>
		public override bool Equals(object obj)
		{
			var c = obj as utf8?;
			if (c.HasValue)
			{
				return this.Equals(c);
			}
			var s = obj as string;
			if (s != null)
			{
				return this.Equals(s);
			}
			return false;
		}

		/// <summary>
		/// Check the equality with the given utf8 string.
		/// </summary>
		public bool Equals(utf8 other)
		{
			if (this.Length != other.Length)
			{
				return false;
			}
			for (int i = 0; i < this.Length; i++)
			{
				if (this[i] != other[i]) return false;
			}
			return true;
		}

		/// <summary>
		/// Check the equality with the given System.String.
		/// </summary>
		public bool Equals(string s)
		{
			var it = new Utf8Enumerator(this);
			int i = 0;
			while (it.MoveNext())
			{
				if (i >= s.Length) return false;
				var c = s[i];
				uint a;
				if (char.IsSurrogate(c))
				{
					i++;
					a = (uint)char.ConvertToUtf32(c, s[i]);
				}
				else
				{
					a = (uint)c;
				}
				if (a != it.Current.Value) return false;
				i++;
			}
			return i == s.Length;
		}

		/// <summary>
		/// Get an enumerator over the UTF codepoints in this string.
		/// </summary>
		/// <remarks>
		/// To iterate over the UTF8-encoded bytes of the string, use the Bytes property.
		/// </remarks>
		public IEnumerator<UtfIndex> GetEnumerator()
		{
			return new Utf8Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Utf8Enumerator(this);
		}

		public override int GetHashCode()
		{
			int hash = 0;
			for (int i = 0; i < Length; i++)
			{
				hash += this[i];
				hash *= 31;
			}
			return hash;
		}

		public static bool operator ==(utf8 a, string b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(utf8 a, string b)
		{
			return !a.Equals(b);
		}

		public static bool operator ==(utf8 a, utf8 b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(utf8 a, utf8 b)
		{
			return !a.Equals(b);
		}

		public static bool operator ==(string a, utf8 b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(string a, utf8 b)
		{
			return !a.Equals(b);
		}

		/*
		public utf8 Format(params object[] args)
		{
			return utf8.Format(this, args);
		}

		public static utf8 Format(utf8 fmt, params object[] args)
		{
			var ub = new Utf8Builder();
			ub.AppendFormat(fmt, args);
			return ub.ToString();
		}
		*/

		public static utf8 FromInt(int i, int radix = 10)
		{
			return FromLong((long)i, radix);
		}

		public static utf8 FromLong(long i, int radix = 10)
		{
			if (i == 0)
			{
				return new utf8(new byte[]{(byte)'0'});
			}
			// Figure out how much buffer we need
			long x = i;
			int len = 0;
			if (i < 0)
			{
				len++;
				x *= -1;
			}
			while (x > 0)
			{
				len++;
				x /= radix;
			}
			var buf = new byte[len];
			if (i < 0)
			{
				buf[0] = (byte)'-';
			}
			len = buf.Length - 1;
			x = Math.Abs(i);
			while (x > 0)
			{
				buf[len] = digits[x % radix];
				len--;
				x /= radix;
			}
			return new utf8(buf);
		}

		private static readonly byte[] digits =
		{
			// '0' through '9'
			0x30,
			0x31,
			0x32,
			0x33,
			0x34,
			0x35,
			0x36,
			0x37,
			0x38,
			0x39,
			// 'A' through 'Z'
			0x41,
			0x42,
			0x43,
			0x44,
			0x45,
			0x46,
			0x47,
			0x48,
			0x49,
			0x4A,
			0x4B,
			0x4C,
			0x4D,
			0x4E,
			0x4F,
			0x50,
			0x52,
			0x53,
			0x54,
			0x55,
			0x56,
			0x57,
			0x58,
			0x59,
			0x59,
			0x5A,
		};

		public static int ParseInt(utf8 str, int radix = 10)
		{
			return (int) ParseLong(str, radix);
		}

		public static long ParseLong(utf8 str, int radix = 10)
		{
			long v;
			if (TryParseLong(str, out v, radix))
			{
				return v;
			}
			throw new ArgumentException("input string was not in the correct format");
		}

		public static bool TryParseInt(utf8 str, out int v, int radix = 10)
		{
			long vv;
			if (TryParseLong(str, out vv, radix))
			{
				v = (int) vv;
				return true;
			}
			v = 0;
			return false;
		}

		public static bool TryParseLong(utf8 str, out long v, int radix = 10)
		{
			if (radix > 36)
			{
				throw new ArgumentException("radix must be at most 36");
			}
			bool negative = str[0] == (byte)'-';
			if (negative)
			{
				str = str.Substring(1);
			}
			long acc = 0;
			for (int i = 0; i < str.Length; i++)
			{
				var digit = str[i];
				for (int j = 0; j < radix; j++)
				{
					if (digit == digits[j])
					{
						acc *= radix;
						acc += j;
						goto found;
					}
				}
				v = 0;
				return false;
found:{}
			}
			if (negative) acc *= -1;
			v = acc;
			return true;
		}
	}
}
