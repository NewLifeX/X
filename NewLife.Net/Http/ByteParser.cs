
namespace NewLife.Net.Http
{
    sealed class ByteParser
    {
        private byte[] _bytes;
        private int _pos;

        internal ByteParser(byte[] bytes)
        {
            _bytes = bytes;
            _pos = 0;
        }

        internal ByteString ReadLine()
        {
            ByteString str = null;
            for (int i = _pos; i < _bytes.Length; i++)
            {
                if (_bytes[i] == 10)
                {
                    int length = i - _pos;
                    if (length > 0 && _bytes[i - 1] == 13) length--;
                    str = new ByteString(_bytes, _pos, length);
                    _pos = i + 1;
                    return str;
                }
            }
            if (_pos < _bytes.Length) str = new ByteString(_bytes, _pos, _bytes.Length - _pos);
            _pos = _bytes.Length;
            return str;
        }

        internal int CurrentOffset { get { return _pos; } }
    }
}