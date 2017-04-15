using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FastString.Unicode;

namespace FastString.DataGen
{
	class FancyFile
	{
		readonly Stream main;

		public FancyFile(Stream str)
		{
			main = str;
		}


		public void WriteFloat(float v)
		{
			FloatBytes f = new FloatBytes();
			f.f = v;
			WriteUint32(f.i);
		}

		public void WriteOffset(long v)
		{
			WriteUint32((uint)v);
		}

		public void WriteUint32(uint v)
		{
			main.WriteByte((byte)((v >> 24) & 0xFF));
			main.WriteByte((byte)((v >> 16) & 0xFF));
			main.WriteByte((byte)((v >> 8) & 0xFF));
			main.WriteByte((byte)((v >> 0) & 0xFF));
		}

		public void WriteByte(byte v)
		{
			main.WriteByte(v);
		}

		public void WriteCodepoint(utf8 codepoint, utf8 backup)
		{
			if (codepoint.HasValue)
			{
				WriteCodepoint(codepoint);
				return;
			}
			WriteCodepoint(backup);
		}

		public void WriteCodepoint(utf8 codepoint)
		{
			if (!codepoint.HasValue)
			{
				WriteUint32(0);
				return;
			}
			WriteUint32((uint)utf8.ParseInt(codepoint, 16));
		}

		public long Position { get { return main.Position; } }

		public void Flush() { main.Flush(); }

		public void Close() { main.Close(); }
	}

	class MainClass
	{
		static readonly char[] separator = { ';' };
		static readonly utf8 IsMirrored = new utf8("Y");

		public static void Main(string[] args)
		{
			new MainClass().DoThings(args);
		}

		public void DoThings(string[] args)
		{
			// Data file available at http://www.unicode.org/Public/UCD/latest/ucd/UnicodeData.txt
			var data = new utf8(File.ReadAllBytes(args[0]));

			var lines = new Splitter('\n', data);

			var main = new FancyFile(new FileStream("chardata", FileMode.Create, FileAccess.Write));
			var toUpper = new FancyFile(new FileStream("chartoupper", FileMode.Create, FileAccess.Write));
			var toLower = new FancyFile(new FileStream("chartolower", FileMode.Create, FileAccess.Write));
			var namesRaw = new FileStream("charnames", FileMode.Create, FileAccess.Write);
			var names = new Utf8Writer(namesRaw);
			// TODO normalization map
			int i = 0;
			long per = 0;
			foreach (var line in lines)
			{
				i++;
				if (line.IsEmpty) continue;
				var start = main.Position;
				//Console.WriteLine("writing {0} at offset {1}", i, main.Position);
				var parts = line.Split(separator);

				// Schema given in ftp://ftp.unicode.org/Public/3.0-Update/UnicodeData-3.0.0.html
				// The current codepoint (eg U+00A2).
				main.WriteCodepoint(parts[0]);

				// The name of this codepoint (eg LATIN SMALL LETTER E WITH MACRON).
				main.WriteOffset(namesRaw.Position);
				names.Append(parts[1]);
				main.WriteOffset(namesRaw.Position);

				// The major category, like Ll or Sm
				main.WriteByte(ParseCategory(parts[2]));

				// Numeric value.
				main.WriteFloat(GetNumericValue(parts));

				// Whether this thing is mirrored
				main.WriteByte((byte)(parts[9] == IsMirrored ? 1 : 0));

				// Upper
				main.WriteCodepoint(parts[12], parts[0]);
				// Lower
				main.WriteCodepoint(parts[13], parts[0]);
				// Title
				main.WriteCodepoint(parts[14], parts[0]);

				if (parts[12].HasValue)
				{
					toUpper.WriteCodepoint(parts[0]);
					toUpper.WriteCodepoint(parts[12]);
					Console.WriteLine("({0}).ToUpper => {1}", parts[0], parts[12]);
				}

				if (parts[14].HasValue)
				{
					toLower.WriteCodepoint(parts[0]);
					toLower.WriteCodepoint(parts[14]);
				}

				var end = main.Position;
				if (per == 0)
				{
					per = end - start;
				}
				else
				{
					if (per != end - start)
					{
						throw new Exception($"at entry $i, expected $per bytes written; actual was ${end - start}");
					}
				}
			}
			main.Flush();
			main.Close();
			namesRaw.Flush();
			namesRaw.Close();
		}

		float GetNumericValue(utf8[] parts)
		{
			if (parts[6].HasValue)
			{
				return utf8.ParseInt(parts[6]);
			}
			if (parts[7].HasValue)
			{
				return utf8.ParseInt(parts[7]);
			}
			if (parts[8].HasValue)
			{
				var p = parts[8].Split(new char[] { '/' }, 3);
				Console.WriteLine("float? value {0} in {1} parts", parts[8], p.Length);
				if (p.Length == 2)
				{
					Console.WriteLine("{0} / {1}", p[0], p[1]);
					Console.WriteLine("{0} / {1}", p[0].Trim(), p[1].Trim());
					return (utf8.ParseInt(p[0].Trim()) * 1.0f / utf8.ParseInt(p[1].Trim()));
				}
				else
				{
					return utf8.ParseInt(parts[8]);
				}
			}
			return float.NaN;
		}


		byte ParseCategory(utf8 str)
		{
			return (byte)Category(str);
		}

		static UnicodeCategory Category(utf8 str)
		{
			if (str == Lu) return UnicodeCategory.UppercaseLetter;
			if (str == Ll) return UnicodeCategory.LowercaseLetter;
			if (str == Lt) return UnicodeCategory.TitlecaseLetter;
			if (str == Mn) return UnicodeCategory.NonSpacingMark;
			if (str == Mc) return UnicodeCategory.SpacingCombiningMark;
			if (str == Me) return UnicodeCategory.EnclosingMark;
			if (str == Nd) return UnicodeCategory.DecimalDigitNumber;
			if (str == Nl) return UnicodeCategory.LetterNumber;
			if (str == No) return UnicodeCategory.OtherNumber;
			if (str == Zs) return UnicodeCategory.SpaceSeparator;
			if (str == Zl) return UnicodeCategory.LineSeparator;
			if (str == Zp) return UnicodeCategory.ParagraphSeparator;
			if (str == Cc) return UnicodeCategory.Control;
			if (str == Cf) return UnicodeCategory.Format;
			if (str == Cs) return UnicodeCategory.Surrogate;
			if (str == Co) return UnicodeCategory.PrivateUse;
			if (str == Cn) return UnicodeCategory.OtherNotAssigned;
			if (str == Lm) return UnicodeCategory.ModifierLetter;
			if (str == Lo) return UnicodeCategory.OtherLetter;
			if (str == Pc) return UnicodeCategory.ConnectorPunctuation;
			if (str == Pd) return UnicodeCategory.DashPunctuation;
			if (str == Ps) return UnicodeCategory.OpenPunctuation;
			if (str == Pe) return UnicodeCategory.ClosePunctuation;
			if (str == Pi) return UnicodeCategory.InitialQuotePunctuation;
			if (str == Pf) return UnicodeCategory.FinalQuotePunctuation;
			if (str == Po) return UnicodeCategory.OtherPunctuation;
			if (str == Sm) return UnicodeCategory.MathSymbol;
			if (str == Sc) return UnicodeCategory.CurrencySymbol;
			if (str == Sk) return UnicodeCategory.ModifierSymbol;
			if (str == So) return UnicodeCategory.OtherSymbol;
			throw new ArgumentOutOfRangeException("invalid unicode category " + str.ToString());
		}

		static readonly utf8 Lu = new utf8("Lu");
		static readonly utf8 Ll = new utf8("Ll");
		static readonly utf8 Lt = new utf8("Lt");
		static readonly utf8 Mn = new utf8("Mn");
		static readonly utf8 Mc = new utf8("Mc");
		static readonly utf8 Me = new utf8("Me");
		static readonly utf8 Nd = new utf8("Nd");
		static readonly utf8 Nl = new utf8("Nl");
		static readonly utf8 No = new utf8("No");
		static readonly utf8 Zs = new utf8("Zs");
		static readonly utf8 Zl = new utf8("Zl");
		static readonly utf8 Zp = new utf8("Zp");
		static readonly utf8 Cc = new utf8("Cc");
		static readonly utf8 Cf = new utf8("Cf");
		static readonly utf8 Cs = new utf8("Cs");
		static readonly utf8 Co = new utf8("Co");
		static readonly utf8 Cn = new utf8("Cn");
		static readonly utf8 Lm = new utf8("Lm");
		static readonly utf8 Lo = new utf8("Lo");
		static readonly utf8 Pc = new utf8("Pc");
		static readonly utf8 Pd = new utf8("Pd");
		static readonly utf8 Ps = new utf8("Ps");
		static readonly utf8 Pe = new utf8("Pe");
		static readonly utf8 Pi = new utf8("Pi");
		static readonly utf8 Pf = new utf8("Pf");
		static readonly utf8 Po = new utf8("Po");
		static readonly utf8 Sm = new utf8("Sm");
		static readonly utf8 Sc = new utf8("Sc");
		static readonly utf8 Sk = new utf8("Sk");
		static readonly utf8 So = new utf8("So");
	}

	[StructLayout(LayoutKind.Explicit, Pack=1)]
	struct FloatBytes
	{
		[FieldOffset(0)]
		public float f;
		[FieldOffset(0)]
		public uint i;
	}
}
