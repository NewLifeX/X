using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Http
{
    sealed class ByteParser
    {
        private byte[] _bytes;
        private int _pos;

        internal ByteParser(byte[] bytes)
        {
            this._bytes = bytes;
            this._pos = 0;
        }

        internal ByteString ReadLine()
        {
            ByteString str = null;
            for (int i = this._pos; i < this._bytes.Length; i++)
            {
                if (this._bytes[i] == 10)
                {
                    int length = i - this._pos;
                    if (length > 0 && this._bytes[i - 1] == 13) length--;
                    str = new ByteString(this._bytes, this._pos, length);
                    this._pos = i + 1;
                    return str;
                }
            }
            if (this._pos < this._bytes.Length) str = new ByteString(this._bytes, this._pos, this._bytes.Length - this._pos);
            this._pos = this._bytes.Length;
            return str;
        }

        internal int CurrentOffset { get { return this._pos; } }
    }
}