using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    public abstract class WriterBase : ReaderWriterBase, IWriter
    {
        #region IWriter 成员

        public void Write(bool value)
        {
            throw new NotImplementedException();
        }

        public void Write(byte value)
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public void Write(char ch)
        {
            throw new NotImplementedException();
        }

        public void Write(char[] chars)
        {
            throw new NotImplementedException();
        }

        public void Write(decimal value)
        {
            throw new NotImplementedException();
        }

        public void Write(double value)
        {
            throw new NotImplementedException();
        }

        public void Write(float value)
        {
            throw new NotImplementedException();
        }

        public void Write(int value)
        {
            throw new NotImplementedException();
        }

        public void Write(long value)
        {
            throw new NotImplementedException();
        }

        public void Write(sbyte value)
        {
            throw new NotImplementedException();
        }

        public void Write(short value)
        {
            throw new NotImplementedException();
        }

        public void Write(string value)
        {
            throw new NotImplementedException();
        }

        public void Write(uint value)
        {
            throw new NotImplementedException();
        }

        public void Write(ulong value)
        {
            throw new NotImplementedException();
        }

        public void Write(ushort value)
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public void Write(char[] chars, int index, int count)
        {
            throw new NotImplementedException();
        }

        public int WriteEncoded(short value)
        {
            throw new NotImplementedException();
        }

        public int WriteEncoded(int value)
        {
            throw new NotImplementedException();
        }

        public int WriteEncoded(long value)
        {
            throw new NotImplementedException();
        }

        public bool WriteObject(object value)
        {
            throw new NotImplementedException();
        }

        public bool Write(System.Collections.IEnumerable value)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
