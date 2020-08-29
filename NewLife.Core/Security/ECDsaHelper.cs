using System;
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
        /// <returns></returns>
        public static ECDsaCng Create(String key)
        {
            key = key?.Trim();
            if (key.IsNullOrEmpty()) return null;

            if (key.StartsWith("-----") && key.EndsWith("-----"))
            {
                var ec = new ECDsaCng();
                var p = ReadPem(key);
                //ec.ImportParameters(ReadPem(key));
                return ec;
            }
            else
            {
                var buf = key.ToBase64();
                var ckey = CngKey.Import(buf, buf.Length < 100 ? CngKeyBlobFormat.EccPublicBlob : CngKeyBlobFormat.EccPrivateBlob);

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
            var key = CngKey.Import(priKey.ToBase64(), CngKeyBlobFormat.EccPrivateBlob);
            var ecc = new ECDsaCng(key) { HashAlgorithm = CngAlgorithm.MD5 };

            return ecc.SignData(data);
        }

        /// <summary>验证，MD5散列</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean Verify(Byte[] data, String pukKey, Byte[] rgbSignature)
        {
            var key = CngKey.Import(pukKey.ToBase64(), CngKeyBlobFormat.EccPublicBlob);
            var ecc = new ECDsaCng(key) { HashAlgorithm = CngAlgorithm.MD5 };

            return ecc.VerifyData(data, rgbSignature);
        }

        /// <summary>Sha256</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] SignSha256(this Byte[] data, String priKey)
        {
            var key = CngKey.Import(priKey.ToBase64(), CngKeyBlobFormat.EccPrivateBlob);
            var ecc = new ECDsaCng(key) { HashAlgorithm = CngAlgorithm.Sha256 };

            return ecc.SignData(data);
        }

        /// <summary>Sha256</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean VerifySha256(this Byte[] data, String pukKey, Byte[] rgbSignature)
        {
            var key = CngKey.Import(pukKey.ToBase64(), CngKeyBlobFormat.EccPublicBlob);
            var ecc = new ECDsaCng(key) { HashAlgorithm = CngAlgorithm.Sha256 };

            return ecc.VerifyData(data, rgbSignature);
        }

        /// <summary>Sha384</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] SignSha384(this Byte[] data, String priKey)
        {
            var key = CngKey.Import(priKey.ToBase64(), CngKeyBlobFormat.EccPrivateBlob);
            var ecc = new ECDsaCng(key) { HashAlgorithm = CngAlgorithm.Sha384 };

            return ecc.SignData(data);
        }

        /// <summary>Sha384</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean VerifySha384(this Byte[] data, String pukKey, Byte[] rgbSignature)
        {
            var key = CngKey.Import(pukKey.ToBase64(), CngKeyBlobFormat.EccPublicBlob);
            var ecc = new ECDsaCng(key) { HashAlgorithm = CngAlgorithm.Sha384 };

            return ecc.VerifyData(data, rgbSignature);
        }

        /// <summary>Sha512</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] SignSha512(this Byte[] data, String priKey)
        {
            var key = CngKey.Import(priKey.ToBase64(), CngKeyBlobFormat.EccPrivateBlob);
            var ecc = new ECDsaCng(key) { HashAlgorithm = CngAlgorithm.Sha512 };

            return ecc.SignData(data);
        }

        /// <summary>Sha512</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean VerifySha512(this Byte[] data, String pukKey, Byte[] rgbSignature)
        {
            var key = CngKey.Import(pukKey.ToBase64(), CngKeyBlobFormat.EccPublicBlob);
            var ecc = new ECDsaCng(key) { HashAlgorithm = CngAlgorithm.Sha512 };

            return ecc.VerifyData(data, rgbSignature);
        }
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
    }
}