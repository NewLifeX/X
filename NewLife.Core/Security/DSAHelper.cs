using System;
using System.Security.Cryptography;

namespace NewLife.Security
{
    /// <summary>DSA算法</summary>
    public static class DSAHelper
    {
        #region 产生密钥
        /// <summary>产生非对称密钥对</summary>
        /// <param name="keySize">密钥长度，默认1024位强密钥</param>
        /// <returns></returns>
        public static String[] GenerateKey(int keySize = 1024)
        {
            var dsa = new DSACryptoServiceProvider(keySize);

            var ss = new String[2];
            var pa = dsa.ExportParameters(true);
            ss[0] = dsa.ToXmlString(true);
            ss[1] = dsa.ToXmlString(false);

            return ss;
        }
        #endregion

        #region 数字签名
        /// <summary>签名</summary>
        /// <param name="buf"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] Sign(Byte[] buf, String priKey)
        {
            var dsa = new DSACryptoServiceProvider();
            dsa.FromXmlString(priKey);

            return dsa.SignData(buf);
        }

        /// <summary>验证</summary>
        /// <param name="buf"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean Verify(Byte[] buf, String pukKey, Byte[] rgbSignature)
        {
            var dsa = new DSACryptoServiceProvider();
            dsa.FromXmlString(pukKey);

            return dsa.VerifyData(buf, rgbSignature);
        }
        #endregion
    }
}