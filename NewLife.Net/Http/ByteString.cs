using System;
using System.Collections;
using System.Text;

namespace NewLife.Net.Http
{
    sealed class ByteString
    {
        private byte[] _bytes;
        private int _length;
        private int _offset;

        public ByteString(byte[] bytes, int offset, int length)
        {
            _bytes = bytes;
            if (_bytes != null && offset >= 0 && length >= 0 && offset + length <= _bytes.Length)
            {
                _offset = offset;
                _length = length;
            }
        }

        public byte[] GetBytes()
        {
            byte[] dst = new byte[_length];
            if (_length > 0) Buffer.BlockCopy(_bytes, _offset, dst, 0, _length);
            return dst;
        }

        public string GetString() { return GetString(Encoding.UTF8); }

        public string GetString(Encoding enc)
        {
            if (IsEmpty) return string.Empty;
            return enc.GetString(_bytes, _offset, _length);
        }

        public int IndexOf(char ch) { return IndexOf(ch, 0); }

        public int IndexOf(char ch, int offset)
        {
            for (int i = offset; i < _length; i++)
            {
                if (this[i] == ((byte)ch)) return i;
            }
            return -1;
        }

        public ByteString[] Split(char sep)
        {
            ArrayList list = new ArrayList();
            int offset = 0;
            while (offset < _length)
            {
                int index = IndexOf(sep, offset);
                if (index < 0)
                {
                    list.Add(Substring(offset));
                    break;
                }
                list.Add(Substring(offset, index - offset));
                for (offset = index + 1; offset < _length && this[offset] == ((byte)sep); offset++)
                {
                }
            }
            int count = list.Count;
            ByteString[] strArray = new ByteString[count];
            for (int i = 0; i < count; i++)
            {
                strArray[i] = (ByteString)list[i];
            }
            return strArray;
        }

        public ByteString Substring(int offset) { return Substring(offset, _length - offset); }

        public ByteString Substring(int offset, int len) { return new ByteString(_bytes, _offset + offset, len); }

        public byte[] Bytes { get { return _bytes; } }

        public bool IsEmpty
        {
            get
            {
                if (_bytes != null) return _length == 0;
                return true;
            }
        }

        public byte this[int index] { get { return _bytes[_offset + index]; } }

        public int Length { get { return _length; } }

        public int Offset { get { return _offset; } }
    }
}