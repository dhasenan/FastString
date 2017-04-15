using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FastString.Unicode;

namespace FastString.DataGen
{
	class MainClass
	{
		static readonly char[] separator = { ';' };
		static readonly Utf8String IsMirrored = new Utf8String("Y");

		public static void Main(string[] args)
		{
			new MainClass().DoThings(args);
		}

		private FileStream main;

		public void DoThings(string[] args)
		{
			// Data file available at http://www.unicode.org/Public/UCD/latest/ucd/UnicodeData.txt
			var data = new Utf8String(File.ReadAllBytes(args[0]));

			var lines = new Splitter('\n', data);

			this.main = new FileStream("chardata", FileMode.Create, FileAccess.Write);
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
				Console.WriteLine("writing {0} at offset {1}", i, main.Position);
				var parts = line.Split(separator);

				// Schema given in ftp://ftp.unicode.org/Public/3.0-Update/UnicodeData-3.0.0.html
				// The current codepoint (eg U+00A2).
				WriteCodepoint(parts[0]);

				// The name of this codepoint (eg LATIN SMALL LETTER E WITH MACRON).
				// 
				WriteOffset(namesRaw.Position);
				names.Append(parts[1]);
				WriteOffset(namesRaw.Position);

				// The major category, like Ll or Sm
				WriteByte(ParseCategory(parts[2]));

				// Numeric value.
				WriteFloat(GetNumericValue(parts));

				// Whether this thing is mirrored
				WriteByte((byte)(parts[9] == IsMirrored ? 1 : 0));

				// Upper
				WriteCodepoint(parts[12], parts[0]);
				// Lower
				WriteCodepoint(parts[13], parts[0]);
				// Title
				WriteCodepoint(parts[14], parts[0]);
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

		float GetNumericValue(Utf8String[] parts)
		{
			if (parts[6].HasValue)
			{
				return Utf8String.ParseInt(parts[6]);
			}
			if (parts[7].HasValue)
			{
				return Utf8String.ParseInt(parts[7]);
			}
			if (parts[8].HasValue)
			{
				var p = parts[8].Split(new char[] { '/' }, 3);
				Console.WriteLine("float? value {0} in {1} parts", parts[8], p.Length);
				if (p.Length == 2)
				{
					Console.WriteLine("{0} / {1}", p[0], p[1]);
					Console.WriteLine("{0} / {1}", p[0].Trim(), p[1].Trim());
					return (Utf8String.ParseInt(p[0].Trim()) * 1.0f / Utf8String.ParseInt(p[1].Trim()));
				}
				else
				{
					return Utf8String.ParseInt(parts[8]);
				}
			}
			return float.NaN;
		}

		void WriteFloat(float v)
		{
			FloatBytes f = new FloatBytes();
			f.f = v;
			WriteUint32(f.i);
		}

		void WriteOffset(long v)
		{
			WriteUint32((uint)v);
		}

		void WriteUint32(uint v)
		{
			main.WriteByte((byte)((v >> 24) & 0xFF));
			main.WriteByte((byte)((v >> 16) & 0xFF));
			main.WriteByte((byte)((v >>  8) & 0xFF));
			main.WriteByte((byte)((v >>  0) & 0xFF));
		}

		void WriteByte(byte v)
		{
			main.WriteByte(v);
		}

		void WriteCodepoint(Utf8String codepoint, Utf8String backup)
		{
			if (codepoint.HasValue)
			{
				WriteCodepoint(codepoint);
				return;
			}
			WriteCodepoint(backup);
		}

		void WriteCodepoint(Utf8String codepoint)
		{
			if (!codepoint.HasValue)
			{
				WriteUint32(0);
				return;
			}
			WriteUint32((uint)Utf8String.ParseInt(codepoint, 16));
		}

		byte ParseCategory(Utf8String str)
		{
			return (byte)CharInfo.ParseCategory(str);
		}
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
