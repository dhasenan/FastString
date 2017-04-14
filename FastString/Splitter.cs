using System;
using System.Collections;
using System.Collections.Generic;

namespace FastString
{
	public struct Splitter : IEnumerator<utf8>, IEnumerable<utf8>
	{
		readonly uint codepoint;
		readonly utf8 str;
		int currStart, currEnd, nextStart;

		public Splitter(char c, utf8 str) : this((uint)c, str) { }

		public Splitter(uint codepoint, utf8 str)
		{
			this.str = str;
			this.codepoint = codepoint;
			nextStart = 0;
			currStart = 0;
			currEnd = 0;
		}

		public utf8 Current
		{
			get
			{
				return str.Substring(currStart, currEnd - currStart);
			}
		}

		object IEnumerator.Current
		{
			get
			{
				return this.Current;
			}
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			if (currStart >= str.Length) return false;
			currStart = nextStart;
			var remaining = str.Substring(nextStart);
			var it = new Utf8Enumerator(remaining);


			while (it.MoveNext())
			{
				if (it.Current.Value == codepoint)
				{
					currEnd = it.Current.Index + currStart;
					nextStart = currEnd + it.Current.EncodedLength ;
					return true;
				}
			}

			currEnd = nextStart = str.Length;
			return true;
		}

		public void Reset()
		{
			nextStart = 0;
		}

		public IEnumerator<utf8> GetEnumerator()
		{
			return this;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this;
		}
	}
}
