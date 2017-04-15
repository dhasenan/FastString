using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FastString.Unicode
{
	[StructLayout(LayoutKind.Explicit, Pack = 1)]
	struct FloatBytes
	{
		[FieldOffset(0)]
		public float f;
		[FieldOffset(0)]
		public uint i;
	}

	/// <summary>
	/// Unicode character info.
	/// </summary>
	public struct CharInfo
	{
		/// <summary>
		/// Find the lowercase version of the given codepoint.
		/// </summary>
		/// <returns>The lowercase codepoint, or the given codepoint if the lowercase was not found.</returns>
		/// <param name="codepoint">Codepoint.</param>
		/// <remarks>
		/// Unicode has approximately 1300 codepoints with lowercase variants. Thus, it is typically more efficient
		/// to call this than to call CharInfo.For(codepoint).Lowercase.
		/// 
		/// System.String does not produce correct results for certain scripts (including Deseret and Old Hungarian).
		/// This does.
		/// </remarks>
		public static uint ToLower(uint codepoint)
		{
			// There are a lot of things like this.
			// For instance, C0 through DE is also +0x20. 0x100 through 0x12F: odds are +1.
			if (codepoint <= 0xC0)
			{
				if (0x41 <= codepoint && codepoint <= 0x5A)
				{
					return codepoint + 0x20;
				}
				// Nothing else in this range has a lowercase variant.
				return codepoint;
			}
			var loc = BinarySearch(codepoint, _toLower, 8);
			if (loc < 0)
			{
				return codepoint;
			}
			return ReadUint32(_toLower, loc + 4);
		}

		/// <summary>
		/// Find the uppercase version of the given codepoint.
		/// </summary>
		/// <returns>The uppercase codepoint, or the given codepoint if the uppercase was not found.</returns>
		/// <param name="codepoint">Codepoint.</param>
		/// <remarks>
		/// Unicode has approximately 1300 codepoints with uppercase variants. Thus, it is typically more efficient
		/// to call this than to call CharInfo.For(codepoint).Uppercase.
		/// 
		/// System.String does not produce correct results for certain scripts (including Deseret and Old Hungarian).
		/// This does.
		/// </remarks>
		public static uint ToUpper(uint codepoint)
		{
			var loc = BinarySearch(codepoint, _toUpper, 8);
			if (loc < 0)
			{
				Console.WriteLine("ToUpper({0:X4}) => self", codepoint);
				return codepoint;
			}
			var c = ReadUint32(_toUpper, loc + 4);
			Console.WriteLine("ToUpper({0:X4}) => {1:X4}", codepoint, c);
			return c;
		}

		/// <summary>
		/// Find the character metadata for the given codepoint.
		/// </summary>
		/// <returns>The relevant character info, or null if it was not found.</returns>
		/// <param name="codepoint">A Unicode codepoint to look up data for.</param>
		/// <remarks>
		/// This will not return data for most characters in the private use areas.
		/// </remarks>
		public static CharInfo? For(uint codepoint)
		{
			// We have 30k-ish codepoints.
			// They're scattered across a much larger range, but at the earliest codepoint X is in position X.
			// A more efficient implementation would look at contiguous ranges.
			if (Match(codepoint * _serializedLength, codepoint))
			{
				return LoadAtOffset(codepoint * _serializedLength);
			}

			long loc = BinarySearch(codepoint, _rawData, _serializedLength);
			if (loc < 0)
			{
				return null;
			}
			return LoadAtOffset(loc);
		}

		public static int Utf8Length(uint c)
		{
			if (c < 0x80) return 1;
			if (c < (1 << 11)) return 2;
			if (c < (1 << 16)) return 3;
			if (c < (1 << 21)) return 4;
			return 5;
		}


		/// <summary>
		/// The codepoint this struct represents, such as 0x41 for 'A'.
		/// </summary>
		public uint Codepoint { get; internal set; }

		/// <summary>
		/// If this is representable as a single UTF-16 code unit, this contains that code unit.
		/// </summary>
		public char? CharValue
		{
			get
			{
				if (Codepoint <= 0xFFFF)
				{
					return (char)Codepoint;
				}
				return null;
			}
		}

		/// <summary>
		/// If this is represented as two UTF-16 code units, the high surrogate contains the high order half.
		/// </summary>
		public char? SurrogateHighValue
		{
			get
			{
				if (Codepoint <= 0xFFFF)
				{
					return null;
				}
				return (char)((Codepoint - 0x10000) >> 10);
			}
		}

		/// <summary>
		/// If this is represented as two UTF-16 code units, the low surrogate contains the low order half.
		/// </summary>
		public char? SurrogateLowValue
		{
			get
			{
				if (Codepoint <= 0xFFFF)
				{
					return null;
				}
				return (char)((Codepoint - 0x10000) & 0x3FF);
			}
		}

		/// <summary>
		/// The human-readable name of this codepoint, such as "LATIN CAPITAL LETTER A".
		/// </summary>
		public utf8 Name { get; internal set; }

		public UnicodeCategory Category { get; internal set; }
		public bool Mirrored { get; internal set; }
		public double NumericValue { get; internal set; }
		public uint Uppercase { get; internal set; }
		public uint Lowercase { get; internal set; }
		public uint Titlecase { get; internal set; }





		private const int _serializedLength = 30;

		static long BinarySearch(uint codepoint, byte[] b, long len)
		{
			// It is not lowerBarrier and is not lower than it.
			long lowerBarrier = -1;
			// It is not upperBarrier and is not greater than it.
			long upperBarrier = b.Length / len;

			while (lowerBarrier < upperBarrier - 1)
			{
				var guess = (upperBarrier - lowerBarrier) / 2 + lowerBarrier;
				Console.WriteLine("guess: {0} lower: {1} upper: {2}", guess, lowerBarrier, upperBarrier);
				var it = ReadUint32(b, guess * len);
				Console.WriteLine("found {0}, want {1}", it, codepoint);
				if (it == codepoint)
				{
					return guess * len;
				}
				if (it < codepoint)
				{
					lowerBarrier = guess;
				}
				if (it > codepoint)
				{
					upperBarrier = guess;
				}
			}
			return -1;
		}

		static CharInfo LoadAtOffset(long offset)
		{
			CharInfo info = new CharInfo();

			info.Codepoint = ReadUint32(_rawData, offset);
			offset += 4;

			var nameStart = ReadUint32(_rawData, offset);
			offset += 4;

			var nameEnd = ReadUint32(_rawData, offset);
			offset += 4;

			if (_rawNames != null)
			{
				info.Name = new utf8(new ArraySegment<byte>(_rawNames, (int)nameStart, (int)(nameEnd - nameStart)));
			}

			info.Category = (UnicodeCategory)_rawData[offset];
			offset++;

			var fb = new FloatBytes();
			fb.i = ReadUint32(_rawData, offset);
			info.NumericValue = fb.f;
			offset += 4;

			info.Mirrored = _rawData[offset] == 1;
			offset++;

			info.Uppercase = ReadUint32(_rawData, offset);
			offset += 4;

			info.Lowercase = ReadUint32(_rawData, offset);
			offset += 4;

			info.Titlecase = ReadUint32(_rawData, offset);

			return info;
		}

		static bool Match(long offset, uint codepoint)
		{
			if (offset < 0 || offset >= _rawData.Length - _serializedLength)
			{
				return false;
			}
			var at = ReadUint32(_rawData, offset);
			return at == codepoint;
		}

		static uint ReadUint32(byte[] b, long offset)
		{
			uint val = 0;
			val <<= 8; val |= b[offset];
			val <<= 8; val |= b[offset + 1];
			val <<= 8; val |= b[offset + 2];
			val <<= 8; val |= b[offset + 3];
			return val;
		}

		static CharInfo()
		{
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FastString.Unicode.chardata");
			using (var mem = new MemoryStream())
			{
				stream.CopyTo(mem);
				_rawData = mem.ToArray();
			}
			stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FastString.Unicode.chartoupper");
			using (var mem = new MemoryStream())
			{
				stream.CopyTo(mem);
				_toUpper = mem.ToArray();
			}
			stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FastString.Unicode.chartolower");
			using (var mem = new MemoryStream())
			{
				stream.CopyTo(mem);
				_toLower = mem.ToArray();
			}
			stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FastString.Unicode.charnames");
			using (var mem = new MemoryStream())
			{
				stream.CopyTo(mem);
				_rawNames = mem.ToArray();
			}
		}

		private static byte[] _rawData;
		private static byte[] _rawNames;
		private static byte[] _toUpper;
		private static byte[] _toLower;
	}
}
