using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace FastString
{
	/// <summary>
	/// An enumerator that goes backwards through a UTF8 string.
	/// </summary>
	/// <remarks>
	/// This is a struct to reduce allocations even further.
	/// </remarks>
	public struct ReverseUtf8Enumerator : IEnumerator<UtfIndex>
	{
		private utf8 _data;
		private UtfIndex _current;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FastString.ReverseUtf8Enumerator"/> struct.
		/// </summary>
		/// <param name="data">The data to iterate over.</param>
		public ReverseUtf8Enumerator(utf8 data)
		{
			_data = data;
			_current = new UtfIndex();
			_current.Index = _data.Length;
		}

		/// <summary>
		/// Set the iterator back to the end of the string.
		/// </summary>
		public void Reset()
		{
			_current = new UtfIndex();
			_current.Index = _data.Length;
		}

		public void Dispose() { }

		/// <summary>
		/// Get the current value.
		/// </summary>
		public UtfIndex Current
		{
			get
			{
				return _current;
			}
		}

		/// <summary>
		/// Demonstrate your love of autoboxing.
		/// </summary>
		object IEnumerator.Current
		{
			get
			{
				return _current;
			}
		}

		/// <summary>
		/// Move to the next value in the string (which will be closer to the start of the string).
		/// 
		/// Returns false when there's no more string to move to.
		/// </summary>
		/// <remarks>
		/// You've used an enumerator before, right?
		/// </remarks>
		public bool MoveNext()
		{
			if (_current.Index == 0)
			{
				return false;
			}
			int i = _current.Index;
			uint accumulator = 0;
			while (i > 0)
			{
				i--;
				int delta = _current.Index - i;

				var c = _data[i];
				if (c <= 127)
				{
					if (delta > 1)
					{
						throw new InvalidDataException(
								string.Format("Input data contains invalid UTF8 sequence around byte offset {0}", i));
					}
					_current.Index = i;
					_current.Value = c;
					return true;
				}
				if (delta > 5)
				{
					throw new InvalidDataException(
							string.Format("Input data contains invalid UTF8 sequence around byte offset {0}", i));
				}
				if ((c & 0xC0) == 2)
				{
					_current.Index = i;
					accumulator |= (uint)(c << (delta * 6));
				}
				else
				{
					int mask, same;
					switch (delta)
					{
						case 1:
							mask = 0xC0;
							same = 0;
							break;
						case 2:
							mask = 0x1F;
							same = 0xC0;
							break;
						case 3:
							mask = 0xF;
							same = 0xE0;
							break;
						case 4:
							mask = 0x7;
							same = 0xF0;
							break;
						case 5:
							mask = 0xF8;
							same = 0x3;
							break;
						default:
							throw new InvalidDataException();
					}
					var b = c & mask;
					if (((c & ~mask) ^ ~same) != ~0)
					{
						throw new InvalidDataException(
								string.Format("invalid UTF8 sequence at {0}", i));
					}
					accumulator |= (uint)(b << (6 * delta));
					_current.Index = i;
					_current.Value = accumulator;
					return true;
				}
			}
			throw new InvalidDataException("invalid UTF8 sequence at start of string");
		}
	}
}
