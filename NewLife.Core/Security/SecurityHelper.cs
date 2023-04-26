using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NewLife.Security;

namespace NewLife
{
    /// <summary>安全算法</summary>
    /// <remarks>
    /// 文档 https://newlifex.com/core/security_helper
    /// </remarks>
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
            if (_md5 == null) _md5 = System.Security.Cryptography.MD5.Create();

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

        /// <summary>计算文件的MD5散列</summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Byte[] MD5(this FileInfo file)
        {
            if (_md5 == null) _md5 = System.Security.Cryptography.MD5.Create();

            using var fs = file.OpenRead();
            return _md5.ComputeHash(fs);
        }

        /// <summary>Crc散列</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static UInt32 Crc(this Byte[] data) => new Crc32().Update(data).Value;

        /// <summary>Crc16散列</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static UInt16 Crc16(this Byte[] data) => new Crc16().Update(data).Value;

        /// <summary>SHA128</summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Byte[] SHA1(this Byte[] data, Byte[] key) => new HMACSHA1(key).ComputeHash(data);

        /// <summary>SHA256</summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Byte[] SHA256(this Byte[] data, Byte[] key) => new HMACSHA256(key).ComputeHash(data);

        /// <summary>SHA384</summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Byte[] SHA384(this Byte[] data, Byte[] key) => new HMACSHA384(key).ComputeHash(data);

        /// <summary>SHA512</summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Byte[] SHA512(this Byte[] data, Byte[] key) => new HMACSHA512(key).ComputeHash(data);

        /// <summary>Murmur128哈希</summary>
        /// <param name="data"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        public static Byte[] Murmur128(this Byte[] data, UInt32 seed = 0) => new Murmur128(seed).ComputeHash(data);
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
        /// <remarks>CBC填充依赖IV，要求加解密的IV一致，而ECB填充则不需要</remarks>
        /// <param name="sa">算法</param>
        /// <param name="data">数据</param>
        /// <param name="pass">密码</param>
        /// <param name="mode">模式。.Net默认CBC，Java默认ECB</param>
        /// <param name="padding">填充算法。默认PKCS7，等同Java的PKCS5</param>
        /// <returns></returns>
        public static Byte[] Encrypt(this SymmetricAlgorithm sa, Byte[] data, Byte[] pass = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            if (data == null || data.Length <= 0) throw new ArgumentNullException(nameof(data));

            if (pass != null && pass.Length > 0)
            {
                if (sa.LegalKeySizes != null && sa.LegalKeySizes.Length > 0)
                    sa.Key = Pad(pass, sa.LegalKeySizes[0]);
                else
                    sa.Key = pass;

                // CBC填充依赖IV，要求加解密的IV一致，而ECB填充则不需要
                var iv = new Byte[sa.IV.Length];
                iv.Write(0, pass);
                sa.IV = iv;

                sa.Mode = mode;
                sa.Padding = padding;
            }

            var outstream = new MemoryStream();
            using var stream = new CryptoStream(outstream, sa.CreateEncryptor(), CryptoStreamMode.Write);
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

        /// <summary>对称解密算法扩展
        /// <para>注意：CryptoStream会把 instream 数据流关闭</para>
        /// </summary>
        /// <param name="sa"></param>
        /// <param name="instream"></param>
        /// <param name="outstream"></param>
        /// <returns></returns>
        public static SymmetricAlgorithm Decrypt(this SymmetricAlgorithm sa, Stream instream, Stream outstream)
        {
            using (var stream = new CryptoStream(instream, sa.CreateDecryptor(), CryptoStreamMode.Read))
            {
                stream.CopyTo(outstream);
            }

            return sa;
        }

        /// <summary>对称解密算法扩展</summary>
        /// <remarks>CBC填充依赖IV，要求加解密的IV一致，而ECB填充则不需要</remarks>
        /// <param name="sa">算法</param>
        /// <param name="data">数据</param>
        /// <param name="pass">密码</param>
        /// <param name="mode">模式。.Net默认CBC，Java默认ECB</param>
        /// <param name="padding">填充算法。默认PKCS7，等同Java的PKCS5</param>
        /// <returns></returns>
        public static Byte[] Decrypt(this SymmetricAlgorithm sa, Byte[] data, Byte[] pass = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            if (data == null || data.Length <= 0) throw new ArgumentNullException(nameof(data));

            if (pass != null && pass.Length > 0)
            {
                if (sa.LegalKeySizes != null && sa.LegalKeySizes.Length > 0)
                    sa.Key = Pad(pass, sa.LegalKeySizes[0]);
                else
                    sa.Key = pass;

                // CBC填充依赖IV，要求加解密的IV一致，而ECB填充则不需要
                var iv = new Byte[sa.IV.Length];
                iv.Write(0, pass);
                sa.IV = iv;

                sa.Mode = mode;
                sa.Padding = padding;
            }

            using var stream = new CryptoStream(new MemoryStream(data), sa.CreateDecryptor(), CryptoStreamMode.Read);
            return stream.ReadBytes(-1);
        }

        private static Byte[] Pad(Byte[] buf, KeySizes keySize)
        {
            var psize = buf.Length * 8;
            var size = 0;
            for (var i = keySize.MinSize; i <= keySize.MaxSize; i += keySize.SkipSize)
            {
                if (i >= psize)
                {
                    size = i / 8;
                    break;
                }

                // DES的SkipSize为0
                if (keySize.SkipSize == 0) break;
            }

            // 所有key大小都不合适，取最大值，此时密码过长，需要截断
            if (size == 0) size = keySize.MaxSize / 8;

            if (buf.Length == size) return buf;

            var buf2 = new Byte[size];
            buf2.Write(0, buf);

            return buf2;
        }

        /// <summary>转换数据（内部加解密）</summary>
        /// <param name="transform"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Byte[] Transform(this ICryptoTransform transform, Byte[] data)
        {
            // 小数据块
            if (data.Length <= transform.InputBlockSize)
                return transform.TransformFinalBlock(data, 0, data.Length);

            // 逐个数据块转换
            var blocks = data.Length / transform.InputBlockSize;
            var inputCount = blocks * transform.InputBlockSize;
            if (inputCount < data.Length) blocks++;

            var output = new Byte[blocks * transform.OutputBlockSize];
            var count = 0;
            if (inputCount > 0 && transform.CanTransformMultipleBlocks)
                count = transform.TransformBlock(data, 0, inputCount, output, 0);
            else
            {
                var pOutput = 0;
                for (var pInput = 0; pInput < inputCount;)
                {
                    count += transform.TransformBlock(data, pInput, transform.InputBlockSize, output, pOutput);
                    pInput += transform.InputBlockSize;
                    pOutput += transform.OutputBlockSize;
                }
            }

            if (count == data.Length) return output;

            //var outstream = new MemoryStream();
            //outstream.Write(output, 0, count);

            var rs = transform.TransformFinalBlock(data, count, data.Length - count);
            Buffer.BlockCopy(rs, 0, output, count, rs.Length);

            return output;

            //outstream.Write(rs);

            //return outstream.ToArray();
        }
        #endregion

        #region RC4
        /// <summary>RC4对称加密算法</summary>
        /// <param name="data"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public static Byte[] RC4(this Byte[] data, Byte[] pass) => NewLife.Security.RC4.Encrypt(data, pass);
        #endregion
    }
}