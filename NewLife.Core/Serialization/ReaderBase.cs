using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    public abstract class ReaderBase : ReaderWriterBase, IReader
    {
        #region IReader 成员

        public bool ReadBoolean()
        {
            throw new NotImplementedException();
        }

        public byte ReadByte()
        {
            throw new NotImplementedException();
        }

        public byte[] ReadBytes(int count)
        {
            throw new NotImplementedException();
        }

        public char ReadChar()
        {
            throw new NotImplementedException();
        }

        public char[] ReadChars(int count)
        {
            throw new NotImplementedException();
        }

        public decimal ReadDecimal()
        {
            throw new NotImplementedException();
        }

        public double ReadDouble()
        {
            throw new NotImplementedException();
        }

        public short ReadInt16()
        {
            throw new NotImplementedException();
        }

        public int ReadInt32()
        {
            throw new NotImplementedException();
        }

        public long ReadInt64()
        {
            throw new NotImplementedException();
        }

        public sbyte ReadSByte()
        {
            throw new NotImplementedException();
        }

        public float ReadSingle()
        {
            throw new NotImplementedException();
        }

        public string ReadString()
        {
            throw new NotImplementedException();
        }

        public ushort ReadUInt16()
        {
            throw new NotImplementedException();
        }

        public uint ReadUInt32()
        {
            throw new NotImplementedException();
        }

        public ulong ReadUInt64()
        {
            throw new NotImplementedException();
        }

        public short ReadEncodedInt16()
        {
            throw new NotImplementedException();
        }

        public int ReadEncodedInt32()
        {
            throw new NotImplementedException();
        }

        public long ReadEncodedInt64()
        {
            throw new NotImplementedException();
        }

        public object ReadObject(Type type)
        {
            throw new NotImplementedException();
        }

        public bool TryReadObject(Type type, ref object obj)
        {
            throw new NotImplementedException();
        }

        public bool TryReadEnumerable(Type type, ref object obj)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
