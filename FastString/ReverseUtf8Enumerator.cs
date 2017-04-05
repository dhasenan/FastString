using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

namespace FastString
{

	public struct ReverseUtf8Enumerator : System.Collections.Generic.IEnumerator<UtfIndex>
	{
		private utf8 _data;
		private UtfIndex _current;

		public ReverseUtf8Enumerator(utf8 data)
		{
			_data = data;
			_current = new UtfIndex();
			_current.Index = _data.Length;
		}

		public void Reset()
		{
			_current = new UtfIndex();
			_current.Index = _data.Length;
		}

		public void Dispose() { }

		public UtfIndex Current
		{
			get
			{
				return _current;
			}
		}

		// I have to implement this explicitly, otherwise I get a compile error saying this needs to return object.
		object IEnumerator.Current
		{
			get
			{
				return _current;
			}
		}

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
				var c = _data[i];
				int delta = _current.Index - i;
				if (delta > 5)
				{
					throw new InvalidDataException(
							string.Format("Input data contains invalid UTF8 sequence around byte offset {1}", i));
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

		void IDisposable.Dispose()
		{
			throw new NotImplementedException();
		}

		bool IEnumerator.MoveNext()
		{
			throw new NotImplementedException();
		}

		void IEnumerator.Reset()
		{
			throw new NotImplementedException();
		}
	}
	
}
