using System;
using System.IO;

namespace FastString
{
	public class Utf8Writer
	{
		protected readonly Stream _out;

		public Utf8Writer(Stream stream)
		{
			_out = stream;
		}

		public void Append(utf8 str)
		{
			_out.Write(str._bytes.Array, str._bytes.Offset, str._bytes.Count);
		}

		public void Append(object o)
		{
			// TODO avoid triple allocation
			Append(new utf8(o.ToString()));
		}

		public void Append(long s)
		{
			Append(utf8.FromLong(s));
		}

		public void Append(string str)
		{
			// TODO avoid double allocation
			Append(new utf8(str));
		}

		public void AppendLine(utf8 str)
		{
			Append(str);
			_out.WriteByte((byte)'\n');
		}

		public void AppendLine()
		{
			_out.WriteByte((byte)'\n');
		}

		public void AppendFormat(utf8 fmt, params object[] args)
		{
			AppendFormat(null, fmt, args);
		}
		public void AppendFormat(IFormatProvider provider, utf8 fmt, params object[] args)
		{
			while (fmt.Length > 0)
			{
				var nextCmd = fmt.IndexOf('{');
				if (nextCmd < 0)
				{
					Append(fmt);
					return;
				}
				Append(fmt.Substring(0, nextCmd));
				fmt = fmt.Substring(nextCmd + 1);
				if (fmt.StartsWith(OBRACE))
				{
					fmt = fmt.Substring(1);
					Append(OBRACE);
					continue;
				}
				var end = fmt.IndexOf('}');
				var cmd = fmt.Substring(0, end);
				fmt = fmt.Substring(end + 1);
				var opts = cmd.IndexOf(':');
				int index;
				utf8 indexStr;
				utf8 argFormat;
				if (opts < 0)
				{
					argFormat = utf8.Empty;
					indexStr = cmd;
				}
				else
				{
					indexStr = cmd.Substring(0, opts);
					argFormat = cmd.Substring(opts + 1);
				}
				if (!utf8.TryParseInt(indexStr, out index))
				{
					throw new FormatException(
						string.Format("Invalid format string: '{0}' is not a valid argument specifier", indexStr));
				}
				if (index < 0 || index >= args.Length)
				{
					throw new FormatException(
						string.Format("Invalid format string: referenced argument {0}, provided {1} arguments", index, args.Length));
				}
				var arg = args[index];
				if (arg == null)
				{
					// null -> empty
					continue;
				}
				if (argFormat.IsEmpty)
				{
					Append(arg.ToString());
				}
				else
				{
					var formattable = arg as IFormattable;
					if (formattable == null)
					{
						throw new FormatException(
							string.Format("Invalid format string: argument {0} has format options {1}, but it does not implement IFormattable", index, argFormat));
					}
					Console.WriteLine("formatting {0} with options {1}", formattable, argFormat);
					Append(formattable.ToString(argFormat.ToString(), provider));
				}
			}
		}

		private static readonly utf8 OBRACE = new utf8("{");
	}
}
