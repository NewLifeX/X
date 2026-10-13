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
        /// <returns>下标0为私钥、下标1为公钥，均为 CNG KeyBlob 格式的 Base64 字符串</returns>
        public static String[] GenerateKey(Int32 keySize = 521)
        {
            var curve = keySize switch
            {
                256 => ECCurve.NamedCurves.nistP256,
                384 => ECCurve.NamedCurves.nistP384,
                521 => ECCurve.NamedCurves.nistP521,
                _ => throw new ArgumentOutOfRangeException(nameof(keySize), $"不支持的密钥长度 {keySize}"),
            };

            // 生成 EC 参数后写成 CNG KeyBlob（幻数+长度+X+Y+D），产物与历史 Windows CNG 导出格式一致，全平台通用
            using var dsa = ECDsa.Create(curve);
            var prm = dsa.ExportParameters(true);

            var ss = new String[2];
            ss[0] = ToKeyBlob(prm, true).ToBase64();
            ss[1] = ToKeyBlob(prm, false).ToBase64();

            return ss;
        }

        /// <summary>把EC参数写成 CNG KeyBlob（幻数+长度+X+Y+D）</summary>
        /// <param name="prm">EC参数</param>
        /// <param name="privateKey">是否包含私钥D</param>
        /// <returns></returns>
        private static Byte[] ToKeyBlob(ECParameters prm, Boolean privateKey)
        {
            // 不用 Curve.Oid.FriendlyName：现代 .NET 的命名曲线友好名为 nistPxxx，与幻数枚举命名不一致，按坐标长度显式映射
            var keySize = prm.Q.X.Length switch
            {
                32 => 256,
                48 => 384,
                66 => 521,
                _ => throw new NotSupportedException($"不支持的椭圆曲线坐标长度 {prm.Q.X.Length}"),
            };

            var ek = new ECKey
            {
                X = prm.Q.X,
                Y = prm.Q.Y,
                D = privateKey ? prm.D : null,
            };
            ek.Algorithm = $"{(privateKey ? "ECDSA_PRIVATE" : "ECDSA_PUBLIC")}_P{keySize}";

            return ek.ToArray();
        }

        /// <summary>创建ECDsa对象，支持Base64密钥和Pem密钥</summary>
        /// <param name="key">密钥内容。CNG KeyBlob 的 Base64，或 PEM 格式的公钥/私钥</param>
        /// <param name="privateKey">是否私钥。仅作兼容保留；KeyBlob 与 PEM 均自带公私标识，以实际内容为准</param>
        /// <returns></returns>
        public static ECDsa Create(String key, Boolean? privateKey = null)
        {
            key = key?.Trim();
            if (key.IsNullOrEmpty()) return null;

            var ec = ECDsa.Create();

            if (key.StartsWith("-----") && key.EndsWith("-----"))
            {
                var ek = ReadPem(key);

                // netcore下优先使用ExportParameters，CngKey.Import有兼容问题
                ec.ImportParameters(ek.ExportParameters());

                //var buf = ek.ToArray();
                //var ckey = CngKey.Import(buf, ek.D == null ? CngKeyBlobFormat.EccPublicBlob : CngKeyBlobFormat.EccPrivateBlob);
                //return new ECDsaCng(ckey);
            }
            else
            {
                // 托管解析 CNG KeyBlob（幻数+长度+X+Y+D），替代 CngKey.Import，Linux/macOS 不再依赖 Windows CNG
                var eckey = new ECKey();
                eckey.Read(key.ToBase64());

                ec.ImportParameters(eckey.ExportParameters());
            }

            return ec;
        }
        #endregion

        #region 数字签名
        /// <summary>签名，MD5散列</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] Sign(Byte[] data, String priKey)
        {
            using var ecc = Create(priKey, true);

            return ecc.SignData(data, HashAlgorithmName.MD5);
        }

        /// <summary>验证，MD5散列</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean Verify(Byte[] data, String pukKey, Byte[] rgbSignature)
        {
            using var ecc = Create(pukKey, false);

            return ecc.VerifyData(data, rgbSignature, HashAlgorithmName.MD5);
        }

        /// <summary>Sha256</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] SignSha256(this Byte[] data, String priKey)
        {
            using var ecc = Create(priKey, true);

            return ecc.SignData(data, HashAlgorithmName.SHA256);
        }

        /// <summary>Sha256</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean VerifySha256(this Byte[] data, String pukKey, Byte[] rgbSignature)
        {
            using var ecc = Create(pukKey, false);

            return ecc.VerifyData(data, rgbSignature, HashAlgorithmName.SHA256);
        }

        /// <summary>Sha384</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] SignSha384(this Byte[] data, String priKey)
        {
            using var ecc = Create(priKey, true);

            return ecc.SignData(data, HashAlgorithmName.SHA384);
        }

        /// <summary>Sha384</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean VerifySha384(this Byte[] data, String pukKey, Byte[] rgbSignature)
        {
            using var ecc = Create(pukKey, false);

            return ecc.VerifyData(data, rgbSignature, HashAlgorithmName.SHA384);
        }

        /// <summary>Sha512</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] SignSha512(this Byte[] data, String priKey)
        {
            using var ecc = Create(priKey, true);

            return ecc.SignData(data, HashAlgorithmName.SHA512);
        }

        /// <summary>Sha512</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean VerifySha512(this Byte[] data, String pukKey, Byte[] rgbSignature)
        {
            using var ecc = Create(pukKey, false);

            return ecc.VerifyData(data, rgbSignature, HashAlgorithmName.SHA512);
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
                var content2 = content.TrimPrefix("-----BEGIN RSA PRIVATE KEY-----")
                     .TrimSuffix("-----END RSA PRIVATE KEY-----")
                     .TrimPrefix("-----BEGIN PRIVATE KEY-----")
                     .TrimSuffix("-----END PRIVATE KEY-----")
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
