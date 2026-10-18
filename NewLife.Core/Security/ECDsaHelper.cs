using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace NewLife.Security
{
    /// <summary>椭圆曲线数字签名算法 (ECDSA) </summary>
    /// <remarks>
    /// .NET Framework 目标（net45/net461/net462）使用 Windows CNG 实现，仅限 Windows 平台；
    /// 其余目标使用跨平台实现，Windows 下由 CNG 支撑、Linux/macOS 下由 OpenSSL 支撑。
    /// 密钥均为 CNG KeyBlob 字节格式，各平台与各实现之间互通。
    /// </remarks>
    public static class ECDsaHelper
    {
        #region 生成密钥
        /// <summary>产生非对称密钥对</summary>
        /// <param name="keySize">密钥长度，默认521位强密钥</param>
        /// <returns>下标0为私钥、下标1为公钥，均为 CNG KeyBlob 格式的 Base64 字符串</returns>
#if NETFRAMEWORK
        public static String[] GenerateKey(Int32 keySize = 521)
        {
            var dsa = new ECDsaCng(keySize);

            var ss = new String[2];
            ss[0] = dsa.Key.Export(CngKeyBlobFormat.EccPrivateBlob).ToBase64();
            ss[1] = dsa.Key.Export(CngKeyBlobFormat.EccPublicBlob).ToBase64();

            return ss;
        }
#else
        public static String[] GenerateKey(Int32 keySize = 521)
        {
            var curve = keySize switch
            {
                256 => ECCurve.NamedCurves.nistP256,
                384 => ECCurve.NamedCurves.nistP384,
                521 => ECCurve.NamedCurves.nistP521,
                _ => throw new ArgumentOutOfRangeException(nameof(keySize), L.T("Unsupported key size {0}.", keySize)),
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
            var x = prm.Q.X ?? throw new InvalidOperationException(L.T("Elliptic curve parameters are missing public key coordinate X."));
            var y = prm.Q.Y ?? throw new InvalidOperationException(L.T("Elliptic curve parameters are missing public key coordinate Y."));

            // 不用 Curve.Oid.FriendlyName：现代 .NET 的命名曲线友好名为 nistPxxx，与幻数枚举命名不一致，按坐标长度显式映射
            var keySize = x.Length switch
            {
                32 => 256,
                48 => 384,
                66 => 521,
                _ => throw new NotSupportedException(L.T("Unsupported elliptic curve coordinate length {0}.", x.Length)),
            };

            var ek = new ECKey
            {
                X = x,
                Y = y,
                D = privateKey ? prm.D : null,
            };
            ek.Algorithm = $"{(privateKey ? "ECDSA_PRIVATE" : "ECDSA_PUBLIC")}_P{keySize}";

            return ek.ToArray();
        }
#endif

        /// <summary>创建ECDsa对象，支持Base64密钥和Pem密钥</summary>
        /// <param name="key">密钥内容。CNG KeyBlob 的 Base64，或 PEM 格式的公钥/私钥</param>
        /// <param name="privateKey">是否私钥。仅作兼容保留；KeyBlob 与 PEM 均自带公私标识，以实际内容为准</param>
        /// <returns>ECDsa实例；密钥为空时返回 null</returns>
#if NETFRAMEWORK
        public static ECDsa? Create(String? key, Boolean? privateKey = null)
        {
            key = key?.Trim();
            if (key.IsNullOrEmpty()) return null;

            if (key.StartsWith("-----") && key.EndsWith("-----"))
            {
                var ek = ReadPem(key);

                var buf = ek.ToArray();
                var ckey = CngKey.Import(buf, ek.D == null ? CngKeyBlobFormat.EccPublicBlob : CngKeyBlobFormat.EccPrivateBlob);

                return new ECDsaCng(ckey);
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

        /// <summary>创建CNG版ECDsa实例，仅在 .NET Framework 目标使用</summary>
        /// <param name="key">密钥内容</param>
        /// <param name="privateKey">是否私钥</param>
        /// <returns></returns>
        private static ECDsaCng CreateCng(String key, Boolean? privateKey) =>
            Create(key, privateKey) as ECDsaCng ?? throw new ArgumentNullException(nameof(key));
#else
        public static ECDsa? Create(String? key, Boolean? privateKey = null)
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
#endif
        #endregion

        #region 数字签名
        /// <summary>签名，MD5散列</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] Sign(Byte[] data, String priKey)
        {
#if NETFRAMEWORK
            using var ecc = CreateCng(priKey, true);
            ecc.HashAlgorithm = CngAlgorithm.MD5;

            return ecc.SignData(data);
#else
            using var ecc = Create(priKey, true) ?? throw new ArgumentNullException(nameof(priKey));

            return ecc.SignData(data, HashAlgorithmName.MD5);
#endif
        }

        /// <summary>验证，MD5散列</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean Verify(Byte[] data, String pukKey, Byte[] rgbSignature)
        {
#if NETFRAMEWORK
            using var ecc = CreateCng(pukKey, false);
            ecc.HashAlgorithm = CngAlgorithm.MD5;

            return ecc.VerifyData(data, rgbSignature);
#else
            using var ecc = Create(pukKey, false) ?? throw new ArgumentNullException(nameof(pukKey));

            return ecc.VerifyData(data, rgbSignature, HashAlgorithmName.MD5);
#endif
        }

        /// <summary>Sha256</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] SignSha256(this Byte[] data, String priKey)
        {
#if NETFRAMEWORK
            using var ecc = CreateCng(priKey, true);
            ecc.HashAlgorithm = CngAlgorithm.Sha256;

            return ecc.SignData(data);
#else
            using var ecc = Create(priKey, true) ?? throw new ArgumentNullException(nameof(priKey));

            return ecc.SignData(data, HashAlgorithmName.SHA256);
#endif
        }

        /// <summary>Sha256</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean VerifySha256(this Byte[] data, String pukKey, Byte[] rgbSignature)
        {
#if NETFRAMEWORK
            using var ecc = CreateCng(pukKey, false);
            ecc.HashAlgorithm = CngAlgorithm.Sha256;

            return ecc.VerifyData(data, rgbSignature);
#else
            using var ecc = Create(pukKey, false) ?? throw new ArgumentNullException(nameof(pukKey));

            return ecc.VerifyData(data, rgbSignature, HashAlgorithmName.SHA256);
#endif
        }

        /// <summary>Sha384</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] SignSha384(this Byte[] data, String priKey)
        {
#if NETFRAMEWORK
            using var ecc = CreateCng(priKey, true);
            ecc.HashAlgorithm = CngAlgorithm.Sha384;

            return ecc.SignData(data);
#else
            using var ecc = Create(priKey, true) ?? throw new ArgumentNullException(nameof(priKey));

            return ecc.SignData(data, HashAlgorithmName.SHA384);
#endif
        }

        /// <summary>Sha384</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean VerifySha384(this Byte[] data, String pukKey, Byte[] rgbSignature)
        {
#if NETFRAMEWORK
            using var ecc = CreateCng(pukKey, false);
            ecc.HashAlgorithm = CngAlgorithm.Sha384;

            return ecc.VerifyData(data, rgbSignature);
#else
            using var ecc = Create(pukKey, false) ?? throw new ArgumentNullException(nameof(pukKey));

            return ecc.VerifyData(data, rgbSignature, HashAlgorithmName.SHA384);
#endif
        }

        /// <summary>Sha512</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        public static Byte[] SignSha512(this Byte[] data, String priKey)
        {
#if NETFRAMEWORK
            using var ecc = CreateCng(priKey, true);
            ecc.HashAlgorithm = CngAlgorithm.Sha512;

            return ecc.SignData(data);
#else
            using var ecc = Create(priKey, true) ?? throw new ArgumentNullException(nameof(priKey));

            return ecc.SignData(data, HashAlgorithmName.SHA512);
#endif
        }

        /// <summary>Sha512</summary>
        /// <param name="data"></param>
        /// <param name="pukKey"></param>
        /// <param name="rgbSignature"></param>
        /// <returns></returns>
        public static Boolean VerifySha512(this Byte[] data, String pukKey, Byte[] rgbSignature)
        {
#if NETFRAMEWORK
            using var ecc = CreateCng(pukKey, false);
            ecc.HashAlgorithm = CngAlgorithm.Sha512;

            return ecc.VerifyData(data, rgbSignature);
#else
            using var ecc = Create(pukKey, false) ?? throw new ArgumentNullException(nameof(pukKey));

            return ecc.VerifyData(data, rgbSignature, HashAlgorithmName.SHA512);
#endif
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
                var asn = Asn1.Read(data) ?? throw new InvalidDataException();
                var keys = asn.Value as Asn1[] ?? throw new InvalidDataException();

                // Algorithm(algorithm + parameters)
                var oids = asn.GetOids();
                var algorithm = oids[0];
                var parameters = oids[1];

                if (algorithm.FriendlyName != "ECC") throw new InvalidDataException($"Invalid key {algorithm}");

                keys = Asn1.Read((keys[2].Value as Byte[]) ?? throw new InvalidDataException())?.Value as Asn1[] ?? throw new InvalidDataException();

                // 里面是一个字节前缀，后面X+Y
                var k2 = Asn1.Read((keys[2].Value as Byte[]) ?? throw new InvalidDataException())?.Value as Byte[] ?? throw new InvalidDataException();
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

                var asn = Asn1.Read(data) ?? throw new InvalidDataException();
                var keys = asn.Value as Asn1[] ?? throw new InvalidDataException();

                // Algorithm(algorithm + parameters)
                var oids = asn.GetOids();
                var algorithm = oids[0];
                var parameters = oids[1];

                if (algorithm.FriendlyName != "ECC") throw new InvalidDataException($"Invalid key {algorithm}");

                // 里面是一个字节前缀，后面X+Y
                var k2 = keys[1].Value as Byte[] ?? throw new InvalidDataException();
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
