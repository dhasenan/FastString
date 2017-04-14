using System;
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

	public partial struct CharInfo
	{
		public static UnicodeCategory ParseCategory(utf8 str)
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
		public char? CharValue { get; internal set; }

		/// <summary>
		/// If this is represented as two UTF-16 code units, the high surrogate contains the high order half.
		/// </summary>
		public char? SurrogateHighValue { get; internal set; }

		/// <summary>
		/// If this is represented as two UTF-16 code units, the low surrogate contains the low order half.
		/// </summary>
		public char? SurrogateLowValue { get; internal set; }

		/// <summary>
		/// The human-readable name of this codepoint, such as "LATIN CAPITAL LETTER A".
		/// </summary>
		public utf8 Name { get; internal set; }

		public double NumericValue { get; internal set; }
		public uint Uppercase { get; internal set; }
		public uint Lowercase { get; internal set; }
		public uint Titlecase { get; internal set; }
		public UnicodeCategory Category { get; internal set; }
		public bool Mirrored { get; internal set; }
	}
}
