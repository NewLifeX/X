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
        [ThreadStatic]
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
            if (encoding == null) encoding = Encoding.UTF8;

            var buf = MD5(encoding.GetBytes(data + ""));
            return buf.ToHex();
        }

        /// <summary>MD5散列</summary>
        /// <param name="data"></param>
        /// <param name="encoding">字符串编码，默认Default</param>
        /// <returns></returns>
        public static String MD5_16(this String data, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;

            var buf = MD5(encoding.GetBytes(data + ""));
            return buf.ToHex(0, 8);
        }

        /// <summary>Crc散列</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static UInt32 Crc(this Byte[] data) { return new Crc32().Update(data).Value; }

        /// <summary>Crc16散列</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static UInt16 Crc16(this Byte[] data) { return new Crc16().Update(data).Value; }

        /// <summary>SHA128</summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Byte[] SHA1(this Byte[] data, Byte[] key)
        {
            return new HMACSHA1(key).ComputeHash(data);
        }

        /// <summary>SHA256</summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Byte[] SHA256(this Byte[] data, Byte[] key)
        {
            return new HMACSHA256(key).ComputeHash(data);
        }

        /// <summary>SHA384</summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Byte[] SHA384(this Byte[] data, Byte[] key)
        {
            return new HMACSHA384(key).ComputeHash(data);
        }

        /// <summary>SHA512</summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Byte[] SHA512(this Byte[] data, Byte[] key)
        {
            return new HMACSHA512(key).ComputeHash(data);
        }
        #endregion

        #region 同步加密扩展
        /// <summary>对称加密算法扩展</summary>
        /// <remarks>注意：CryptoStream会把 outstream 数据流关闭</remarks>
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
        /// <param name="sa">算法</param>
        /// <param name="data">数据</param>
        /// <param name="pass">密码</param>
        /// <param name="mode">模式。.Net默认CBC，Java默认ECB</param>
        /// <param name="padding">填充算法。默认PKCS7，等同Java的PKCS5</param>
        /// <returns></returns>
        public static Byte[] Encrypt(this SymmetricAlgorithm sa, Byte[] data, Byte[] pass = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));

            if (pass != null && pass.Length > 0)
            {
                var keySize = sa.KeySize / 8;
                sa.Key = Pad(pass, keySize);

                var ivSize = sa.IV.Length;
                sa.IV = Pad(pass, ivSize);

                sa.Mode = mode;
                sa.Padding = padding;
            }

            var outstream = new MemoryStream();
            using (var stream = new CryptoStream(outstream, sa.CreateEncryptor(), CryptoStreamMode.Write))
            {
                stream.Write(data, 0, data.Length);

                // 数据长度必须是8的倍数
                if (sa.Padding == PaddingMode.None)
                {
                    var len = data.Length % 8;
                    if (len > 0)
                    {
                        var buf = new Byte[8 - len];
                        stream.Write(buf, 0, buf.Length);
                    }
                }

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
        /// <param name="sa">算法</param>
        /// <param name="data">数据</param>
        /// <param name="pass">密码</param>
        /// <param name="mode">模式。.Net默认CBC，Java默认ECB</param>
        /// <param name="padding">填充算法。默认PKCS7，等同Java的PKCS5</param>
        /// <returns></returns>
        public static Byte[] Descrypt(this SymmetricAlgorithm sa, Byte[] data, Byte[] pass = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));

            if (pass != null && pass.Length > 0)
            {
                var keySize = sa.KeySize / 8;
                sa.Key = Pad(pass, keySize);

                var ivSize = sa.IV.Length;
                sa.IV = Pad(pass, ivSize);

                sa.Mode = mode;
                sa.Padding = padding;
            }

            using (var stream = new CryptoStream(new MemoryStream(data), sa.CreateDecryptor(), CryptoStreamMode.Read))
            {
                return stream.ReadBytes();
            }
        }

        private static Byte[] Pad(Byte[] buf, Int32 length)
        {
            if (buf.Length == length) return buf;

            var buf2 = new Byte[length];
            buf2.Write(0, buf);

            return buf2;
        }
        #endregion

        #region RC4
        /// <summary>RC4对称加密算法</summary>
        /// <param name="data"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public static Byte[] RC4(this Byte[] data, Byte[] pass) { return NewLife.Security.RC4.Encrypt(data, pass); }
        #endregion
    }
}