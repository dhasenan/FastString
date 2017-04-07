namespace FastString
{
	/// <summary>
	/// An index/value pair into a UTF string.
	/// </summary>
	public struct UtfIndex
	{
		/// <summary>
		/// The offset into the string.
		/// </summary>
		/// <remarks>
		/// Offsets are frequently not contiguous in UTF8, occasionally not contiguous in UTF16, and
		/// always contiguous in UTF32.
		/// </remarks>
		public int Index;

		/// <summary>
		/// The UTF codepoint at this index.
		/// </summary>
		/// <remarks>
		/// A System.Char can hold values from 0 to 0xFFFF. UTF defines values from 0 to 0x10FFFF, so
		/// one System.Char can only hold a small fraction of possible values. Therefore we use a uint
		/// instead.
		/// </remarks>
		public uint Value;

		/// <summary>
		/// The value at this index as a char.
		/// </summary>
		/// <remarks>
		/// Not all Unicode codepoints can fit in a char -- for instance, emoji.
		/// </remarks>
		public char CharValue
		{
			get
			{
				return (char)Value;
			}
		}

		/// <summary>
		/// The number of code units this has in the underlying string's encoding.
		/// </summary>
		public int EncodedLength;
	}
	
}
