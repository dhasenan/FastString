using System;
using System.IO;

namespace FastString
{
	public class Utf8Builder : Utf8Writer
	{
		private MemoryStream _memoryStream;

		public Utf8Builder() : base(new MemoryStream())
		{
			_memoryStream = (System.IO.MemoryStream)_out;
		}

		public Utf8Builder(int capacity): base(new MemoryStream(capacity))
		{
			_memoryStream = (System.IO.MemoryStream)_out;
		}

		public void EnsureCapacity(int capacity)
		{
			if (_memoryStream.Capacity < capacity)
			{
				_memoryStream.Capacity = capacity;
			}
		}

		public utf8 ToUtf8()
		{
			return new utf8(
				new ArraySegment<byte>(_memoryStream.GetBuffer(), 0, (int)_memoryStream.Position));
		}
	}
}

