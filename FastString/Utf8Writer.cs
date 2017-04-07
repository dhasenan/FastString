using System;
using System.IO;

namespace FastString
{
	/// <summary>
	/// An efficient appender to a stream of UTF8 data.
	/// </summary>
	/// <remarks>
	/// This is a generalization of StringBuilder that works with arbitrary streams.
	/// </remarks>
	public class Utf8Writer
	{
		protected readonly Stream _out;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FastString.Utf8Writer"/> class.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		public Utf8Writer(Stream stream)
		{
			_out = stream;
		}

		/// <summary>
		/// Append the specified utf8 string.
		/// </summary>
		/// <param name="str">String.</param>
		public void Append(utf8 str)
		{
			_out.Write(str._bytes.Array, str._bytes.Offset, str._bytes.Count);
		}

		/// <summary>
		/// Append the given object.
		/// </summary>
		public void Append(object o)
		{
			// TODO avoid triple allocation
			Append(new utf8(o.ToString()));
		}

		/// <summary>
		/// Append the given integer-type thing.
		/// </summary>
		public void Append(long s)
		{
			Append(utf8.FromLong(s));
		}

		/// <summary>
		/// Append the given string.
		/// </summary>
		public void Append(string str)
		{
			// TODO avoid double allocation
			Append(new utf8(str));
		}

		/// <summary>
		/// Append the given string and insert a following newline.
		/// </summary>
		public void AppendLine(utf8 str)
		{
			Append(str);
			_out.WriteByte((byte)'\n');
		}

		/// <summary>
		/// Append a newline character.
		/// </summary>
		public void AppendLine()
		{
			_out.WriteByte((byte)'\n');
		}

		/// <summary>
		/// Append a formatted string.
		/// </summary>
		/// <param name="fmt">The format string.</param>
		/// <param name="args">Format arguments to use to generate the formatted value.</param>
		public void AppendFormat(utf8 fmt, params object[] args)
		{
			AppendFormat(null, fmt, args);
		}

		/// <summary>
		/// Append a formatted string.
		/// </summary>
		/// <param name="provider">The format provider to use to format arguments.</param>
		/// <param name="fmt">The format string.</param>
		/// <param name="args">Format arguments to use to generate the formatted value.</param>
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
