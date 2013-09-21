using System;
using System.IO;
using System.Security.Cryptography;

namespace NewLife.Security
{
    /// <summary>RSA算法</summary>
    public static class RSAHelper
    {
        #region 加密解密
        /// <summary>产生非对称密钥对</summary>
        /// <param name="keySize">密钥长度，默认1024位强密钥</param>
        /// <returns></returns>
        public static String[] GenerateKey(int keySize = 1024)
        {
            var rsa = new RSACryptoServiceProvider(keySize);

            var ss = new String[2];
            var pa = rsa.ExportParameters(true);
            ss[0] = rsa.ToXmlString(true);
            ss[1] = rsa.ToXmlString(false);

            return ss;
        }

        /// <summary>RSA加密</summary>
        /// <param name="buf"></param>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public static Byte[] Encrypt(Byte[] buf, String pubKey)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(pubKey);

            return rsa.Encrypt(buf, true);
        }

        /// <summary>RSA解密</summary>
        /// <param name="buf"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] Decrypt(Byte[] buf, String priKey)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(priKey);

            return rsa.Decrypt(buf, true);
        }
        #endregion

        #region 复合加解密
        /// <summary>配合DES加密</summary>
        /// <param name="buf"></param>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public static Byte[] EncryptWithDES(Byte[] buf, String pubKey) { return Encrypt<DESCryptoServiceProvider>(buf, pubKey); }

        /// <summary>配合DES解密</summary>
        /// <param name="buf"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] DecryptWithDES(Byte[] buf, String priKey) { return Decrypt<DESCryptoServiceProvider>(buf, priKey); }

        /// <summary>配合对称算法加密</summary>
        /// <typeparam name="TSymmetricAlgorithm"></typeparam>
        /// <param name="buf"></param>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public static Byte[] Encrypt<TSymmetricAlgorithm>(Byte[] buf, String pubKey) where TSymmetricAlgorithm : SymmetricAlgorithm, new()
        {
            // 随机产生对称加密密钥
            var sa = new TSymmetricAlgorithm();
            sa.GenerateIV();
            sa.GenerateKey();

            // 对称加密
            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, sa.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(buf, 0, buf.Length);
            cs.FlushFinalBlock();

            buf = ms.ToArray();

            ms = new MemoryStream();
            ms.WriteWithLength(sa.Key)
                .WriteWithLength(sa.IV);
            var keys = ms.ToArray();

            // 非对称加密前面的随机密钥
            keys = Encrypt(keys, pubKey);

            // 组合起来
            ms = new MemoryStream();
            ms.WriteWithLength(keys)
                .WriteWithLength(buf);

            return ms.ToArray();
        }

        /// <summary>配合对称算法解密</summary>
        /// <typeparam name="TSymmetricAlgorithm"></typeparam>
        /// <param name="buf"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] Decrypt<TSymmetricAlgorithm>(Byte[] buf, String priKey) where TSymmetricAlgorithm : SymmetricAlgorithm, new()
        {
            var ms = new MemoryStream(buf);

            // 读取已加密的对称密钥
            var keys = ms.ReadWithLength();
            // 读取已加密的数据
            buf = ms.ReadWithLength();

            // 非对称解密密钥
            keys = Decrypt(keys, priKey);

            ms = new MemoryStream(keys);

            var sa = new TSymmetricAlgorithm();
            sa.Key = ms.ReadWithLength();
            sa.IV = ms.ReadWithLength();

            // 对称解密
            var stream = new CryptoStream(new MemoryStream(buf), sa.CreateDecryptor(), CryptoStreamMode.Read);

            ms = new MemoryStream();
            while (true)
            {
                Byte[] buffer = new Byte[1024];
                Int32 count = stream.Read(buffer, 0, buffer.Length);
                if (count <= 0) break;

                ms.Write(buffer, 0, count);
                if (count < buffer.Length) break;
            }

            return ms.ToArray();
        }
        #endregion

        #region 辅助
        static Stream WriteWithLength(this Stream stream, Byte[] buf)
        {
            var bts = BitConverter.GetBytes(buf.Length);
            stream.Write(bts);
            stream.Write(buf);

            return stream;
        }

        static Byte[] ReadWithLength(this Stream stream)
        {
            var bts = new Byte[4];
            stream.Read(bts, 0, bts.Length);

            var len = BitConverter.ToInt32(bts, 0);
            bts = new Byte[len];

            stream.Read(bts, 0, bts.Length);

            return bts;
        }

        static Stream Write(this Stream stream, params Byte[][] bufs)
        {
            //stream.Write(buf, 0, buf.Length);
            foreach (var buf in bufs)
            {
                stream.Write(buf, 0, buf.Length);
            }

            return stream;
        }
        #endregion
    }
}