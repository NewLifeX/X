using System;
using System.IO;
using System.Security.Cryptography;

namespace NewLife.Security
{
    /// <summary>安全算法</summary>
    public static class SecurityHelper
    {
        /// <summary>对称加密算法扩展</summary>
        /// <param name="sa"></param>
        /// <param name="instream"></param>
        /// <param name="outstream"></param>
        /// <returns></returns>
        public static SymmetricAlgorithm Encrypt(this SymmetricAlgorithm sa, Stream instream, Stream outstream)
        {
            using (var stream = new CryptoStream(outstream, sa.CreateEncryptor(), CryptoStreamMode.Write))
            {
                instream.CopyTo(stream);
                stream.FlushFinalBlock();
            }

            return sa;
        }

        /// <summary>对称加密算法扩展</summary>
        /// <param name="sa"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Byte[] Encrypt(this SymmetricAlgorithm sa, Byte[] data)
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException("data");

            var outstream = new MemoryStream();
            using (var stream = new CryptoStream(outstream, sa.CreateEncryptor(), CryptoStreamMode.Write))
            {
                stream.Write(data, 0, data.Length);
                stream.FlushFinalBlock();

                return outstream.ToArray();
            }
        }

        /// <summary>对称解密算法扩展</summary>
        /// <param name="sa"></param>
        /// <param name="instream"></param>
        /// <param name="outstream"></param>
        /// <returns></returns>
        public static SymmetricAlgorithm Descrypt(this SymmetricAlgorithm sa, Stream instream, Stream outstream)
        {
            using (var stream = new CryptoStream(instream, sa.CreateDecryptor(), CryptoStreamMode.Read))
            {
                while (true)
                {
                    Byte[] buffer = new Byte[1024];
                    Int32 count = stream.Read(buffer, 0, buffer.Length);
                    if (count <= 0) break;

                    outstream.Write(buffer, 0, count);
                    if (count < buffer.Length) break;
                }
            }

            return sa;
        }

        /// <summary>对称解密算法扩展</summary>
        /// <param name="sa"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Byte[] Descrypt(this SymmetricAlgorithm sa, Byte[] data)
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException("data");

            var ms = new MemoryStream(data);
            using (var stream = new CryptoStream(ms, sa.CreateDecryptor(), CryptoStreamMode.Read))
            {
                var ms2 = new MemoryStream();
                while (true)
                {
                    Byte[] buffer = new Byte[1024];
                    Int32 count = stream.Read(buffer, 0, buffer.Length);
                    if (count <= 0) break;

                    ms2.Write(buffer, 0, count);
                    if (count < buffer.Length) break;
                }

                return ms2.ToArray();
            }
        }
    }
}