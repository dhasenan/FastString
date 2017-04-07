using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace FastString
{
	/// <summary>
	/// A view into a substring of a string.
	/// </summary>
	/// <remarks>
	/// The string.Substring method produces a new copy of its underlying data. This reduces eventual memory usage for
	/// long-held substrings with much larger and ephemeral source strings. However, when the lifetime of the source
	/// string is similar to or greater than the substrings, this is inefficient.
	/// 
	/// StringView is a substring that does not reallocate data.
	/// 
	/// StringView is a struct and never holds a null string.
	/// </remarks>
	public struct StringSlice
	{
		public StringSlice(string str) : this(str ?? "", 0, str == null ? 0 : str.Length)
		{
		}

		public StringSlice(string str, int start, int end)
		{
			if (str == null)
			{
				str = "";
			}
			Contract.Requires(end >= start, "StringView: end must not be before start");
			Contract.Requires(end <= str.Length, "StringView: end must not be beyond end of string");
			_str = str;
			_start = start;
			_end = end;
			Length = end - start;
		}

		private readonly string _str;
		private readonly int _start, _end;

		/// <summary>
		/// The length of this slice.
		/// </summary>
		public readonly int Length;

		/// <summary>
		/// Whether this string slice represents a non-empty slice.
		/// </summary>
		public bool HasValue { get { return Length > 0; } }

		/// <summary>
		/// Get the character at the given index.
		/// </summary>
		public char this[int i]
		{
			get { return _str[i + _start]; }
		}

		/// <summary>
		/// Take a substring of this slice.
		/// </summary>
		public StringSlice Substring(int start, int length)
		{
			return new StringSlice(_str, _start + start, _start + start + length);
		}

		/// <summary>
		/// Take a substring of this slice.
		/// </summary>
		public StringSlice Substring(int start)
		{
			return new StringSlice(_str, _start + start, _end);
		}

		/// <summary>
		/// Find the first index in this slice at which any of the input characters can be found.
		/// </summary>
		public int IndexOfAny(char[] options)
		{
			for (int i = _start; i < _end; i++)
			{
				for (int j = 0; j < options.Length; j++)
				{
					if (_str[i] == options[j]) return i - _start;
				}
			}
			return -1;
		}

		/// <summary>
		/// Find the first index in this slice at which the given string can be found.
		/// </summary>
		public int IndexOf(string str)
		{
			for (int i = _start; i < _end - str.Length; i++)
			{
				if (OccursAt(str, i)) return i - _start;
			}
			return -1;
		}

		/// <summary>
		/// Find the first index in this slice at which the given string can be found.
		/// </summary>
		public int IndexOf(StringSlice str)
		{
			for (int i = _start; i < _end - str.Length; i++)
			{
				if (OccursAt(str, i)) return i - _start;
			}
			return -1;
		}

		/// <summary>
		/// Find the first index in this slice at which the given character can be found.
		/// </summary>
		public int IndexOf(char c)
		{
			for (int i = _start; i < _end; i++)
			{
				if (_str[i] == c) return i - _start;
			}
			return -1;
		}

		/// <summary>
		/// Whether this slice begins with the given string.
		/// </summary>
		public bool StartsWith(string expected)
		{
			if (Length < expected.Length) return false;
			return OccursAt(expected, _start);
		}

		/// <summary>
		/// Whether this slice begins with the given string.
		/// </summary>
		public bool StartsWith(StringSlice expected)
		{
			if (Length < expected.Length) return false;
			return OccursAt(expected, _start);
		}

		/// <summary>
		/// Create a slice of this slice with any initial whitespace removed.
		/// </summary>
		public StringSlice TrimStart()
		{
			for (int s = _start; s < _end; s++)
			{
				if (!char.IsWhiteSpace(_str[s]))
				{
					return new StringSlice(_str, s, _end);
				}
			}
			return new StringSlice("");
		}

		/// <summary>
		/// Split this slice on the given separator character, limited to at most max segments.
		/// </summary>
		public StringSlice[] Split(char separator, int max)
		{
			var list = new List<StringSlice>();
			int curr = _start;
			for (int i = _start; i < _end; i++)
			{
				if (_str[i] == separator)
				{
					list.Add(new StringSlice(_str, curr, i));
					curr = i + 1;
					if (max - 1 <= list.Count) break;
				}
			}
			if (curr < _end)
			{
				list.Add(new StringSlice(_str, curr, _end));
			}
			return list.ToArray();
		}

		/// <summary>
		/// Determines whether the specified <see cref="FastString.StringSlice"/> is equal to the current <see cref="T:FastString.StringSlice"/>.
		/// </summary>
		public bool Equals(StringSlice other)
		{
			if (Length != other.Length) return false;
			for (int i = 0; i < Length; i++)
			{
				if (this[i] != other[i]) return false;
			}
			return true;
		}

		/// <summary>
		/// Determines whether the specified <see cref="string"/> is equal to the current <see cref="T:FastString.StringSlice"/>.
		/// </summary>
		public bool Equals(string other)
		{
			return Equals(new StringSlice(other));
		}

		/// <summary>
		/// Determines whether the specified <see cref="object"/> is equal to the current <see cref="T:FastString.StringSlice"/>.
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return !HasValue;
			}
			var sv = obj as StringSlice?;
			if (sv != null)
			{
				return this.Equals(sv.Value);
			}
			var str = obj as string;
			if (str == null)
			{
				return this.Equals(str);
			}
			return false;
		}

		/// <summary>
		/// Serves as a hash function for a <see cref="T:FastString.StringSlice"/> object.
		/// </summary>
		/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
		public override int GetHashCode()
		{
			var hash = 0;
			for (int i = _start; i < _end; i++)
			{
				hash += _str[i];
				hash *= 31;
			}
			return hash;
		}

		/// <summary>
		/// Get the represented data as a System.String.
		/// </summary>
		/// <remarks>
		/// Every time you invoke this, you create a new copy of the underlying data. Please, think of the data.
		/// </remarks>
		public override string ToString()
		{
			if (_str == null) return "";
			return _str.Substring(_start, Length);
		}

		public static int Compare(StringSlice a, StringSlice b)
		{
			var end = Math.Min(a.Length, b.Length);
			for (int i = 0; i < end; i++)
			{
				var x = a[i].CompareTo(b[i]);
				if (x != 0) return x;
			}
			return a.Length.CompareTo(b.Length);
		}

		private bool OccursAt(string expected, int loc)
		{
			for (int i = 0; i < expected.Length; i++)
			{
				if (expected[i] != _str[loc + i])
				{
					return false;
				}
			}
			return true;
		}

		private bool OccursAt(StringSlice expected, int loc)
		{
			for (int i = 0; i < expected.Length; i++)
			{
				if (expected[i] != _str[loc + i])
				{
					return false;
				}
			}
			return true;
		}
	}
}
