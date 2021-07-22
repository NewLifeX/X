using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace NewLife.Security
{
    /// <summary>椭圆曲线数字签名算法 (ECDSA) </summary>
    public static class ECDsaHelper
    {
        #region 生成密钥
        /// <summary>产生非对称密钥对</summary>
        /// <param name="keySize">密钥长度，默认521位强密钥</param>
        /// <returns></returns>
        public static String[] GenerateKey(Int32 keySize = 521)
        {
            var dsa = new ECDsaCng(keySize);

            var ss = new String[2];
            ss[0] = dsa.Key.Export(CngKeyBlobFormat.EccPrivateBlob).ToBase64();
            ss[1] = dsa.Key.Export(CngKeyBlobFormat.EccPublicBlob).ToBase64();

            return ss;
        }

        /// <summary>创建ECDsa对象，支持Base64密钥和Pem密钥</summary>
        /// <param name="key"></param>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static ECDsaCng Create(String key, Boolean? privateKey = null)
        {
            key = key?.Trim();
            if (key.IsNullOrEmpty()) return null;

            if (key.StartsWith("-----") && key.EndsWith("-----"))
            {
                var ek = ReadPem(key);

#if __CORE__
                // netcore下优先使用ExportParameters，CngKey.Import有兼容问题
                var ec = new ECDsaCng();
                ec.ImportParameters(ek.ExportParameters());

                return ec;
#else
                var buf = ek.ToArray();

                var ckey = CngKey.Import(buf, ek.D == null ? CngKeyBlobFormat.EccPublicBlob : CngKeyBlobFormat.EccPrivateBlob);

                return new ECDsaCng(ckey);
#endif
            }
            else
            {
                var buf = key.ToBase64();
                var ckey =
                    privateKey != null ?
                    CngKey.Import(buf, !privateKey.Value ? CngKeyBlobFormat.EccPublicBlob : CngKeyBlobFormat.EccPrivateBlob) :
                    CngKey.Import(buf, buf.Length < 100 ? CngKeyBlobFormat.EccPublicBlob : CngKeyBlobFormat.EccPrivateBlob);

                return new ECDsaCng(ckey);
            }
        }
        #endregion

        #region 数字签名
        /// <summary>签名，MD5散列</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] Sign(Byte[] data, String priKey)
        {
            var ecc = Create(priKey, true);
            ecc.HashAlgorithm = CngAlgorithm.MD5;

            return ecc.SignData(data);
        }

        /// <summary>验证，MD5散列</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean Verify(Byte[] data, String pukKey, Byte[] rgbSignature)
        {
            var ecc = Create(pukKey, false);
            ecc.HashAlgorithm = CngAlgorithm.MD5;

            return ecc.VerifyData(data, rgbSignature);
        }

        /// <summary>Sha256</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] SignSha256(this Byte[] data, String priKey)
        {
            var ecc = Create(priKey, true);
            ecc.HashAlgorithm = CngAlgorithm.Sha256;

            return ecc.SignData(data);
        }

        /// <summary>Sha256</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean VerifySha256(this Byte[] data, String pukKey, Byte[] rgbSignature)
        {
            var ecc = Create(pukKey, false);
            ecc.HashAlgorithm = CngAlgorithm.Sha256;

            return ecc.VerifyData(data, rgbSignature);
        }

        /// <summary>Sha384</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] SignSha384(this Byte[] data, String priKey)
        {
            var ecc = Create(priKey, true);
            ecc.HashAlgorithm = CngAlgorithm.Sha384;

            return ecc.SignData(data);
        }

        /// <summary>Sha384</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean VerifySha384(this Byte[] data, String pukKey, Byte[] rgbSignature)
        {
            var ecc = Create(pukKey, false);
            ecc.HashAlgorithm = CngAlgorithm.Sha384;

            return ecc.VerifyData(data, rgbSignature);
        }

        /// <summary>Sha512</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] SignSha512(this Byte[] data, String priKey)
        {
            var ecc = Create(priKey, true);
            ecc.HashAlgorithm = CngAlgorithm.Sha512;

            return ecc.SignData(data);
        }

        /// <summary>Sha512</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean VerifySha512(this Byte[] data, String pukKey, Byte[] rgbSignature)
        {
            var ecc = Create(pukKey, false);
            ecc.HashAlgorithm = CngAlgorithm.Sha512;

            return ecc.VerifyData(data, rgbSignature);
        }
        #endregion

        #region PEM
        /// <summary>读取PEM文件到RSA参数</summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static ECKey ReadPem(String content)
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

                // PrivateKeyInfo: version + Algorithm(algorithm + parameters) + privateKey
                var asn = Asn1.Read(data);
                var keys = asn.Value as Asn1[];

                // Algorithm(algorithm + parameters)
                var oids = asn.GetOids();
                var algorithm = oids[0];
                var parameters = oids[1];

                if (algorithm.FriendlyName != "ECC") throw new InvalidDataException($"Invalid key {algorithm}");

                keys = Asn1.Read(keys[2].Value as Byte[]).Value as Asn1[];

                // 里面是一个字节前缀，后面X+Y
                var k2 = Asn1.Read(keys[2].Value as Byte[]).Value as Byte[];
                var len = (k2.Length - 1) / 2;

                // 参数
                var ek = new ECKey
                {
                    D = keys[1].Value as Byte[],
                    X = k2.ReadBytes(1, len),
                    Y = k2.ReadBytes(1 + len, len),
                };
                ek.SetAlgorithm(parameters, true);

                return ek;
            }
            else
            {
                content = content.Replace("-----BEGIN PUBLIC KEY-----", null)
                    .Replace("-----END PUBLIC KEY-----", null)
                    .Replace("\n", null).Replace("\r", null);

                var data = Convert.FromBase64String(content);

                var asn = Asn1.Read(data);
                var keys = asn.Value as Asn1[];

                // Algorithm(algorithm + parameters)
                var oids = asn.GetOids();
                var algorithm = oids[0];
                var parameters = oids[1];

                if (algorithm.FriendlyName != "ECC") throw new InvalidDataException($"Invalid key {algorithm}");

                // 里面是一个字节前缀，后面X+Y
                var k2 = keys[1].Value as Byte[];
                var len = (k2.Length - 1) / 2;

                // 参数
                var ek = new ECKey
                {
                    X = k2.ReadBytes(1, len),
                    Y = k2.ReadBytes(1 + len, len),
                };
                ek.SetAlgorithm(parameters, false);

                return ek;
            }
        }
        #endregion
    }
}