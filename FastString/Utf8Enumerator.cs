using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

namespace FastString
{

	/// <summary>
	/// An enumerator over Unicode codepoints in a utf8 string.
	/// </summary>
	/// <remarks>
	/// This iterates over codepoints, which are integers in the range 0 to 0x10FFFF.
	/// </remarks>
	public struct Utf8Enumerator : IEnumerator<UtfIndex>
	{
		private readonly byte[] _data;
		private readonly int _offset;
		private readonly int _count;
		private UtfIndex _current;
		private int _nextIndex;

		/// <summary>
		/// Create a Utf8Enumerator from the given utf8 string.
		/// </summary>
		public Utf8Enumerator(utf8 data)
		{
			_data = data._bytes.Array;
			_offset = data._bytes.Offset;
			_count = data._bytes.Count;
			_current = new UtfIndex();
			_current.Index = -1;
			_nextIndex = 0;
		}

		public void Dispose() { }

		public void Reset()
		{
			_current = new UtfIndex();
			_current.Index = -1;
			_nextIndex = 0;
		}

		/// <summary>
		/// Gets the current value.
		/// </summary>
		public UtfIndex Current
		{
			get { return _current; }
		}

		// I have to implement this explicitly, otherwise I get a compile error saying this needs to return object.
		object IEnumerator.Current
		{
			get { return _current; }
		}

		/// <summary>
		/// Moves to the next value in the string.
		/// </summary>
		public bool MoveNext()
		{
			if (_nextIndex >= _count) return false;

			var c = _data[_nextIndex + _offset];
			if ((c & 0x80) == 0)
			{
				// Single-octet character.
				_current.Index = _nextIndex;
				_current.Value = c;
				_current.EncodedLength = 1;
				_nextIndex++;
			}
			else if (c >= 0xF8)
			{
				ReadSingleCharacter(5, 0x3);
			}
			else if (c >= 0xF0)
			{
				ReadSingleCharacter(4, 0x7);
			}
			else if (c >= 0xE0)
			{
				ReadSingleCharacter(3, 0xF);
			}
			else if (c >= 0xC0)
			{
				ReadSingleCharacter(2, 0x1F);
			}
			else
			{
				throw new InvalidDataException(
						string.Format(
							"Input data contains invalid UTF8 element {0} at byte offset {1}",
							c,
							_nextIndex));
			}
			return true;
		}

		private void ReadSingleCharacter(int count, byte mask)
		{
			if (_nextIndex + count > _data.Length)
			{
				throw new InvalidDataException("The input was terminated in the middle of a UTF sequence.");
			}
			// 11100010:10000000:10011101
			uint accumulator = (uint)(_data[_nextIndex + _offset] & mask);
			for (int j = 1; j < count; j++)
			{
				accumulator <<= 6;
				var curr = _data[_nextIndex + j + _offset] & 0x3F;
				accumulator |= (uint)curr;
			}
			_current.Index = _nextIndex;
			_current.Value = accumulator;
			_current.EncodedLength = count;
			_nextIndex += count;
		}
	}
	
}
