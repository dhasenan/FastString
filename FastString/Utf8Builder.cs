using System;
using System.IO;

namespace FastString
{
    /// <summary>
    /// A UTF8 string builder, much like <see cref="System.Text.StringBuilder"/>.
    /// </summary>
    public class Utf8Builder : Utf8Writer
    {
        private MemoryStream _memoryStream;

        /// <summary>
        /// Build me a Utf8Builder worthy of Mordor.
        /// </summary>
        public Utf8Builder() : base(new MemoryStream())
        {
            _memoryStream = (System.IO.MemoryStream)_out;
        }

        /// <summary>
        /// Build a Utf8Builder with the specified capacity.
        /// </summary>
        /// <param name="capacity">Capacity. Or worthiness of Mordor. Hard to say.</param>
        public Utf8Builder(int capacity): base(new MemoryStream(capacity))
        {
            _memoryStream = (System.IO.MemoryStream)_out;
        }

        /// <summary>
        /// Ensure that this Utf8Builder has at least the specified amount of memory available.
        /// </summary>
        public void EnsureCapacity(int capacity)
        {
            if (_memoryStream.Capacity < capacity)
            {
                _memoryStream.Capacity = capacity;
            }
        }

        /// <summary>
        /// Retrieve the string built so far.
        /// </summary>
        /// <remarks>
        /// This method does not allocate memory.
        /// </remarks>
        public Utf8String ToUtf8()
        {
#if NET451
            return new Utf8String(
                new ArraySegment<byte>(_memoryStream.GetBuffer(), 0, (int)_memoryStream.Position));
#else
            _memoryStream.TryGetBuffer(out var buffer);
            return new Utf8String(buffer);
#endif
        }
    }
}

