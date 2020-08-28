using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace NewLife.Security
{
    /// <summary>RSA算法</summary>
    /// <remarks>
    /// RSA加密或签名小数据块时，密文长度128，速度也很快。
    /// </remarks>
    public static class RSAHelper
    {
        #region 加密解密
        /// <summary>产生非对称密钥对</summary>
        /// <remarks>
        /// RSAParameters的各个字段采用大端字节序，转为BigInteger的之前一定要倒序。
        /// RSA加密后密文最小长度就是密钥长度，所以1024密钥最小密文长度是128字节。
        /// </remarks>
        /// <param name="keySize">密钥长度，默认1024位强密钥</param>
        /// <returns></returns>
        public static String[] GenerateKey(Int32 keySize = 2048)
        {
            var rsa = new RSACryptoServiceProvider(keySize);

            var ss = new String[2];
            ss[0] = rsa.ToXmlString(true);
            ss[1] = rsa.ToXmlString(false);

            return ss;
        }

        /// <summary>创建RSA对象，支持Xml密钥和Pem密钥</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static RSACryptoServiceProvider Create(String key)
        {
            key = key?.Trim();
            if (key.IsNullOrEmpty()) return null;

            var rsa = new RSACryptoServiceProvider();
            if (key.StartsWith("-----") && key.EndsWith("-----"))
                rsa.ImportParameters(ReadPem(key));
            else
                rsa.FromXmlString(key);

            return rsa;
        }

        /// <summary>RSA加密</summary>
        /// <param name="buf"></param>
        /// <param name="pubKey"></param>
        /// <param name="fOAEP">如果为 true，则使用 OAEP 填充（仅可用于运行 Windows XP 及更高版本的计算机）执行直接 System.Security.Cryptography.RSA加密；否则，如果为 false，则使用 PKCS#1 v1.5 填充。</param>
        /// <returns></returns>
        public static Byte[] Encrypt(Byte[] buf, String pubKey, Boolean fOAEP = true)
        {
            var rsa = Create(pubKey);

            return rsa.Encrypt(buf, fOAEP);
        }

        /// <summary>RSA解密</summary>
        /// <param name="buf"></param>
        /// <param name="priKey"></param>
        /// <param name="fOAEP">如果为 true，则使用 OAEP 填充（仅可用于运行 Microsoft Windows XP 及更高版本的计算机）执行直接 System.Security.Cryptography.RSA解密；否则，如果为 false 则使用 PKCS#1 v1.5 填充。</param>
        /// <returns></returns>
        public static Byte[] Decrypt(Byte[] buf, String priKey, Boolean fOAEP = true)
        {
            var rsa = Create(priKey);

            return rsa.Decrypt(buf, fOAEP);
        }
        #endregion

        #region 复合加解密
        /// <summary>配合DES加密</summary>
        /// <param name="buf"></param>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public static Byte[] EncryptWithDES(Byte[] buf, String pubKey) => Encrypt<DESCryptoServiceProvider>(buf, pubKey);

        /// <summary>配合DES解密</summary>
        /// <param name="buf"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] DecryptWithDES(Byte[] buf, String priKey) => Decrypt<DESCryptoServiceProvider>(buf, priKey);

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
            buf = sa.Encrypt(buf);

            var ms = new MemoryStream();
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

            var sa = new TSymmetricAlgorithm
            {
                Key = ms.ReadWithLength(),
                IV = ms.ReadWithLength()
            };

            // 对称解密
            return sa.Decrypt(buf);
        }
        #endregion

        #region 数字签名
        /// <summary>签名</summary>
        /// <param name="buf"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] Sign(Byte[] buf, String priKey)
        {
            var rsa = Create(priKey);

            return rsa.SignData(buf, MD5.Create());
        }

        /// <summary>验证</summary>
        /// <param name="buf"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean Verify(Byte[] buf, String pukKey, Byte[] rgbSignature)
        {
            var rsa = Create(pukKey);

            return rsa.VerifyData(buf, MD5.Create(), rgbSignature);
        }

        /// <summary>RS256</summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Byte[] RSASHA256(this Byte[] data, Byte[] key) => new HMACSHA256(key).ComputeHash(data);
        #endregion

        #region PEM
        /// <summary>读取PEM文件到RSA参数</summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static RSAParameters ReadPem(String content)
        {
            if (String.IsNullOrEmpty(content)) throw new ArgumentNullException(nameof(content));

            // 公钥私钥分别处理
            content = content.Trim();
            if (content.StartsWithIgnoreCase("-----BEGIN RSA PRIVATE KEY-----", "-----BEGIN PRIVATE KEY-----"))
            {
                var content2 = content.TrimStart("-----BEGIN RSA PRIVATE KEY-----")
                     .TrimEnd("-----END RSA PRIVATE KEY-----")
                     .TrimStart("-----BEGIN PRIVATE KEY-----")
                     .TrimEnd("-----END PRIVATE KEY-----")
                     .Replace("\n", null).Replace("\r", null);

                var data = Convert.FromBase64String(content2);
                //var reader = new BinaryReader(new MemoryStream(data));

                var asn = Asn1.Read(data);

                var key = asn.Value as Asn1[];
                //var version = seq[0].Value;
                //var privateKey = seq[2].Value as Byte[];

                //var seq2 = seq[1].Value as Asn1[];
                //var algorithm = seq2[0].Value;
                //var parameters = seq2[1].Value;

                if (content.StartsWithIgnoreCase("-----BEGIN PRIVATE KEY-----"))
                    key = Asn1.Read(key[2].Value as Byte[]).Value as Asn1[];

                //// 头部版本
                //var total = reader.ReadTLV(out var tag);
                //Debug.Assert(tag == 0x30);
                //var version = reader.ReadTLV(false);

                // 参数数据
                return new RSAParameters
                {
                    Modulus = key[1].GetByteArray(true),
                    Exponent = key[2].GetByteArray(false),
                    D = key[3].GetByteArray(true),
                    P = key[4].GetByteArray(true),
                    Q = key[5].GetByteArray(true),
                    DP = key[6].GetByteArray(true),
                    DQ = key[7].GetByteArray(true),
                    InverseQ = key[8].GetByteArray(true)
                };
            }
            else
            {
                content = content.Replace("-----BEGIN PUBLIC KEY-----", null)
                    .Replace("-----END PUBLIC KEY-----", null)
                    .Replace("\n", null).Replace("\r", null);

                var data = Convert.FromBase64String(content);
                //var reader = new BinaryReader(new MemoryStream(data));

                var asn = Asn1.Read(data);
                var seq = asn.Value as Asn1[];
                var key = Asn1.Read(seq[1].Value as Byte[]).Value as Asn1[];

                //// 头部版本
                //var total = reader.ReadTLV(out var tag);
                //Debug.Assert(tag == 0x30);
                //var version = reader.ReadTLV(false);

                //var total2 = reader.ReadTLV(out tag);
                //if (reader.PeekChar() == 0) { reader.ReadByte(); }
                //var total3 = reader.ReadTLV(out tag);

                // 参数数据
                return new RSAParameters
                {
                    Modulus = key[0].GetByteArray(true),
                    Exponent = key[1].GetByteArray(false),
                };
            }
        }
        #endregion

        #region 辅助
        private static RNGCryptoServiceProvider _rng;
        /// <summary>使用随机数设置</summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        public static Byte[] SetRandom(this Byte[] buf)
        {
            if (_rng == null) _rng = new RNGCryptoServiceProvider();

            _rng.GetBytes(buf);

            return buf;
        }

        private static Stream WriteWithLength(this Stream stream, Byte[] buf)
        {
            var bts = BitConverter.GetBytes(buf.Length);
            stream.Write(bts);
            stream.Write(buf);

            return stream;
        }

        private static Byte[] ReadWithLength(this Stream stream)
        {
            var bts = new Byte[4];
            stream.Read(bts, 0, bts.Length);

            var len = BitConverter.ToInt32(bts, 0);
            bts = new Byte[len];

            stream.Read(bts, 0, bts.Length);

            return bts;
        }

        private static Stream Write(this Stream stream, params Byte[][] bufs)
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