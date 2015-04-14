using System.IO;
using System.Security.Cryptography;
using System.Text;
using NewLife.Security;

namespace System
{
    /// <summary>安全算法</summary>
    public static class SecurityHelper
    {
        #region 哈希
        private static MD5 _md5;
        /// <summary>MD5散列</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Byte[] MD5(this Byte[] data)
        {
            if (_md5 == null) _md5 = new MD5CryptoServiceProvider();

            return _md5.ComputeHash(data);
        }

        /// <summary>MD5散列</summary>
        /// <param name="data"></param>
        /// <param name="encoding">字符串编码，默认Default</param>
        /// <returns></returns>
        public static String MD5(this String data, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.Default;

            var buf = MD5(encoding.GetBytes(data));
            return buf.ToHex();
        }

        /// <summary>MD5散列</summary>
        /// <param name="data"></param>
        /// <param name="encoding">字符串编码，默认Default</param>
        /// <returns></returns>
        public static String MD5_16(this String data, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.Default;

            var buf = MD5(encoding.GetBytes(data));
            return buf.ToHex(0, 16);
        }

        /// <summary>Crc散列</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static UInt32 Crc(this Byte[] data) { return new Crc32().Update(data).Value; }

        /// <summary>Crc16散列</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static UInt16 Crc16(this Byte[] data) { return new Crc16().Update(data).Value; }
        #endregion

        #region 同步加密扩展
        /// <summary>对称加密算法扩展
        /// <para>注意：CryptoStream会把 outstream 数据流关闭</para>
        /// </summary>
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

        /// <summary>对称解密算法扩展
        /// <para>注意：CryptoStream会把 instream 数据流关闭</para>
        /// </summary>
        /// <param name="sa"></param>
        /// <param name="instream"></param>
        /// <param name="outstream"></param>
        /// <returns></returns>
        public static SymmetricAlgorithm Descrypt(this SymmetricAlgorithm sa, Stream instream, Stream outstream)
        {
            using (var stream = new CryptoStream(instream, sa.CreateDecryptor(), CryptoStreamMode.Read))
            {
                stream.CopyTo(outstream);
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

            using (var stream = new CryptoStream(new MemoryStream(data), sa.CreateDecryptor(), CryptoStreamMode.Read))
            {
                return stream.ReadBytes();
            }
        }
        #endregion

        #region RC4
#if !Android
        /// <summary>RC4对称加密算法</summary>
        /// <param name="data"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public static Byte[] RC4(this Byte[] data, Byte[] pass) { return NewLife.Security.RC4.Encrypt(data, pass); }
#endif
        #endregion
    }
}