using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewLife.Queue.Utilities
{
    public class ByteUtil
    {
        public static readonly byte[] ZeroLengthBytes = BitConverter.GetBytes(0);
        public static readonly byte[] EmptyBytes = new byte[0];

        public static void EncodeString(string data, out byte[] lengthBytes, out byte[] dataBytes)
        {
            if (data != null)
            {
                dataBytes = Encoding.UTF8.GetBytes(data);
                lengthBytes = BitConverter.GetBytes(dataBytes.Length);
            }
            else
            {
                dataBytes = EmptyBytes;
                lengthBytes = ZeroLengthBytes;
            }
        }
        public static byte[] EncodeDateTime(DateTime data)
        {
            return BitConverter.GetBytes(data.Ticks);
        }
        public static string DecodeString(byte[] sourceBuffer, int startOffset, out int nextStartOffset)
        {
            return Encoding.UTF8.GetString(DecodeBytes(sourceBuffer, startOffset, out nextStartOffset));
        }
        public static short DecodeShort(byte[] sourceBuffer, int startOffset, out int nextStartOffset)
        {
            var shortBytes = new byte[2];
            Buffer.BlockCopy(sourceBuffer, startOffset, shortBytes, 0, 2);
            nextStartOffset = startOffset + 2;
            return BitConverter.ToInt16(shortBytes, 0);
        }
        public static int DecodeInt(byte[] sourceBuffer, int startOffset, out int nextStartOffset)
        {
            var intBytes = new byte[4];
            Buffer.BlockCopy(sourceBuffer, startOffset, intBytes, 0, 4);
            nextStartOffset = startOffset + 4;
            return BitConverter.ToInt32(intBytes, 0);
        }
        public static long DecodeLong(byte[] sourceBuffer, int startOffset, out int nextStartOffset)
        {
            var longBytes = new byte[8];
            Buffer.BlockCopy(sourceBuffer, startOffset, longBytes, 0, 8);
            nextStartOffset = startOffset + 8;
            return BitConverter.ToInt64(longBytes, 0);
        }
        public static DateTime DecodeDateTime(byte[] sourceBuffer, int startOffset, out int nextStartOffset)
        {
            var longBytes = new byte[8];
            Buffer.BlockCopy(sourceBuffer, startOffset, longBytes, 0, 8);
            nextStartOffset = startOffset + 8;
            return new DateTime(BitConverter.ToInt64(longBytes, 0));
        }
        public static byte[] DecodeBytes(byte[] sourceBuffer, int startOffset, out int nextStartOffset)
        {
            var lengthBytes = new byte[4];
            Buffer.BlockCopy(sourceBuffer, startOffset, lengthBytes, 0, 4);
            startOffset += 4;

            var length = BitConverter.ToInt32(lengthBytes, 0);
            var dataBytes = new byte[length];
            Buffer.BlockCopy(sourceBuffer, startOffset, dataBytes, 0, length);
            startOffset += length;

            nextStartOffset = startOffset;

            return dataBytes;
        }
        public static byte[] Combine(params byte[][] arrays)
        {
            byte[] destination = new byte[arrays.Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in arrays)
            {
                Buffer.BlockCopy(data, 0, destination, offset, data.Length);
                offset += data.Length;
            }
            return destination;
        }
        public static byte[] Combine(IEnumerable<byte[]> arrays)
        {
            byte[] destination = new byte[arrays.Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in arrays)
            {
                Buffer.BlockCopy(data, 0, destination, offset, data.Length);
                offset += data.Length;
            }
            return destination;
        }
    }
}
