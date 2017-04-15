using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FastString.Unicode
{
	public enum UnicodeCategory
	{
		LetterUppercase,
		LetterLowercase,
		LetterTitlecase,
		MarkNonSpacing,
		MarkSpacingCombining,
		MarkEnclosing,
		NumberDecimalDigit,
		NumberLetter,
		NumberOther,
		SeparatorSpace,
		SeparatorLine,
		SeparatorParagraph,
		Control,
		Format,
		Surrogate,
		PrivateUse,
		NotAssigned,
		LetterModifier,
		LetterOther,
		PunctuationConnector,
		PunctuationDash,
		PunctuationOpen,
		PunctuationClose,
		PunctuationInitial,
		PunctuationFinal,
		PunctuationOther,
		SymbolMath,
		SymbolCurrency,
		SymbolModifier,
		SymbolOther,
	}

	[StructLayout(LayoutKind.Explicit, Pack = 1)]
	struct FloatBytes
	{
		[FieldOffset(0)]
		public float f;
		[FieldOffset(0)]
		public uint i;
	}

	public struct CharInfo
	{
		private const int _serializedLength = 30;
		public static CharInfo? For(uint codepoint)
		{
			// We have 30k-ish codepoints.
			// They're scattered across a much larger range, but at the earliest codepoint X is in position X.
			// There are a fair bit glommed together at the start, though.
			if (Match(codepoint * _serializedLength, codepoint))
			{
				return LoadAtOffset(codepoint * _serializedLength);
			}

			// It is not lowerBarrier and is not lower than it.
			int lowerBarrier = -1;
			// It is not upperBarrier and is not greater than it.
			int upperBarrier = _rawData.Length / _serializedLength;
			Console.WriteLine("start search for {2}: lower {0} upper {1}", lowerBarrier, upperBarrier, codepoint);

			while (lowerBarrier < upperBarrier - 1)
			{
				var guess = (upperBarrier - lowerBarrier) / 2 + lowerBarrier;
				var it = ReadUint32(guess * _serializedLength);
				if (it == codepoint)
				{
					return LoadAtOffset(guess * _serializedLength);
				}
				if (it < codepoint)
				{
					lowerBarrier = guess;
				}
				if (it > codepoint)
				{
					upperBarrier = guess;
				}
				Console.WriteLine("guess: {0} was: {1} lower: {2} upper: {3}", guess, it, lowerBarrier, upperBarrier);
			}

			return null;
		}

		static CharInfo LoadAtOffset(long offset)
		{
			CharInfo info = new CharInfo();

			info.Codepoint = ReadUint32(offset);
			offset += 4;

			var nameStart = ReadUint32(offset);
			offset += 4;

			var nameEnd = ReadUint32(offset);
			offset += 4;

			if (_rawNames != null)
			{
				info.Name = new Utf8String(new ArraySegment<byte>(_rawNames, (int)nameStart, (int)(nameEnd - nameStart)));
			}

			info.Category = (UnicodeCategory)_rawData[offset];
			offset++;

			var fb = new FloatBytes();
			fb.i = ReadUint32(offset);
			info.NumericValue = fb.f;
			offset += 4;

			info.Mirrored = _rawData[offset] == 1;
			offset++;

			info.Uppercase = ReadUint32(offset);
			offset += 4;

			info.Lowercase = ReadUint32(offset);
			offset += 4;

			info.Titlecase = ReadUint32(offset);

			return info;
		}

		static bool Match(long offset, uint codepoint)
		{
			if (offset < 0 || offset >= _rawData.Length - _serializedLength)
			{
				return false;
			}
			var at = ReadUint32(offset);
			return at == codepoint;
		}

		static uint ReadUint32(long offset)
		{
			uint val = 0;
			val <<= 8; val |= _rawData[offset];
			val <<= 8; val |= _rawData[offset + 1];
			val <<= 8; val |= _rawData[offset + 2];
			val <<= 8; val |= _rawData[offset + 3];
			return val;
		}

		public static void LoadCharacterData()
		{
			if (_rawData != null)
			{
				return;
			}
			lock (_lock)
			{
				if (_rawData != null)
				{
					return;
				}
				var stream = typeof(CharInfo).GetTypeInfo().Assembly.GetManifestResourceStream("FastString.Unicode.chardata");
				using (var mem = new MemoryStream())
				{
					stream.CopyTo(mem);
					_rawData = mem.ToArray();
				}
			}
		}

		public static void LoadCharacterNames()
		{
			if (_rawNames != null)
			{
				return;
			}
			lock (_lock)
			{
				if (_rawNames != null)
				{
					return;
				}
				var stream = typeof(CharInfo).GetTypeInfo().Assembly.GetManifestResourceStream("FastString.Unicode.charnames");
				using (var mem = new MemoryStream())
				{
					stream.CopyTo(mem);
					_rawNames = mem.ToArray();
				}
			}
		}

		private static object _lock = new object();
		private static byte[] _rawData;
		private static byte[] _rawNames;

		public static UnicodeCategory ParseCategory(Utf8String str)
		{
			if (str == "Lu") return UnicodeCategory.LetterUppercase;
			if (str == "Ll") return UnicodeCategory.LetterLowercase;
			if (str == "Lt") return UnicodeCategory.LetterTitlecase;
			if (str == "Mn") return UnicodeCategory.MarkNonSpacing;
			if (str == "Mc") return UnicodeCategory.MarkSpacingCombining;
			if (str == "Me") return UnicodeCategory.MarkEnclosing;
			if (str == "Nd") return UnicodeCategory.NumberDecimalDigit;
			if (str == "Nl") return UnicodeCategory.NumberLetter;
			if (str == "No") return UnicodeCategory.NumberOther;
			if (str == "Zs") return UnicodeCategory.SeparatorSpace;
			if (str == "Zl") return UnicodeCategory.SeparatorLine;
			if (str == "Zp") return UnicodeCategory.SeparatorParagraph;
			if (str == "Cc") return UnicodeCategory.Control;
			if (str == "Cf") return UnicodeCategory.Format;
			if (str == "Cs") return UnicodeCategory.Surrogate;
			if (str == "Co") return UnicodeCategory.PrivateUse;
			if (str == "Cn") return UnicodeCategory.NotAssigned;
			if (str == "Lm") return UnicodeCategory.LetterModifier;
			if (str == "Lo") return UnicodeCategory.LetterOther;
			if (str == "Pc") return UnicodeCategory.PunctuationConnector;
			if (str == "Pd") return UnicodeCategory.PunctuationDash;
			if (str == "Ps") return UnicodeCategory.PunctuationOpen;
			if (str == "Pe") return UnicodeCategory.PunctuationClose;
			if (str == "Pi") return UnicodeCategory.PunctuationInitial;
			if (str == "Pf") return UnicodeCategory.PunctuationFinal;
			if (str == "Po") return UnicodeCategory.PunctuationOther;
			if (str == "Sm") return UnicodeCategory.SymbolMath;
			if (str == "Sc") return UnicodeCategory.SymbolCurrency;
			if (str == "Sk") return UnicodeCategory.SymbolModifier;
			if (str == "So") return UnicodeCategory.SymbolOther;
			throw new ArgumentOutOfRangeException("invalid unicode category " + str.ToString());
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
		public Utf8String Name { get; internal set; }

		public UnicodeCategory Category { get; internal set; }
		public bool Mirrored { get; internal set; }
		public double NumericValue { get; internal set; }
		public uint Uppercase { get; internal set; }
		public uint Lowercase { get; internal set; }
		public uint Titlecase { get; internal set; }
	}
}
