using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NewLife.Security
{
    /// <summary>DES算法</summary>
    public static class DESHelper
    {
        #region 加密解密
        /// <summary>产生非对称密钥对</summary>
        /// <param name="keySize">密钥长度，默认1024位强密钥</param>
        /// <param name="ivSize">初始化向量长度</param>
        /// <returns></returns>
        public static Byte[] GenerateKey(int keySize = 1024, Int32 ivSize = 8)
        {
            var des = new DESCryptoServiceProvider();
            des.GenerateIV();
            des.GenerateKey();

            return des.Key;
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
    }
}