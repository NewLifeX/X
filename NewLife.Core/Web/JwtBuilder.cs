using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Data;
using NewLife.Security;
using NewLife.Serialization;

namespace NewLife.Web
{
    /// <summary>Jwt编码委托</summary>
    /// <param name="data"></param>
    /// <param name="secrect"></param>
    /// <returns></returns>
    public delegate Byte[] JwtEncodeDelegate(Byte[] data, String secrect);

    /// <summary>Jwt解码委托</summary>
    /// <param name="data"></param>
    /// <param name="secrect"></param>
    /// <param name="signature"></param>
    /// <returns></returns>
    public delegate Boolean JwtDecodeDelegate(Byte[] data, String secrect, Byte[] signature);

    /// <summary>JSON Web Token</summary>
    /// <remarks>
    /// 主要问题：
    /// 1，JWT默认不加密，但可以加密。生成原始令牌后，可以使用该令牌再次对其进行加密。
    /// 2，当JWT未加密时，私密数据无法通过JWT传输。
    /// 3，JWT不仅可用于认证，还可用于信息交换。善用JWT有助于减少服务器请求数据库的次数。
    /// 4，JWT的最大缺点是服务器不保存会话状态，所以在使用期间不可能取消令牌或更改令牌的权限。也就是说，一旦JWT签发，在有效期内将会一直有效。
    /// 5，JWT本身包含认证信息，因此一旦信息泄露，任何人都可以获得令牌的所有权限。为了减少盗用，JWT的有效期不宜设置太长。对于某些重要操作，用户在使用时应该每次都进行进行身份验证。
    /// 6，为了减少盗用和窃取，JWT不建议使用HTTP协议来传输代码，而是使用加密的HTTPS协议进行传输。
    /// </remarks>
    public class JwtBuilder : IExtend3
    {
        #region 属性
        /// <summary>颁发者</summary>
        public String Issuer { get; set; }

        /// <summary>主体所有人。可以存放userid/roleid等，作为用户唯一标识</summary>
        public String Subject { get; set; }

        /// <summary>受众</summary>
        public String Audience { get; set; }

        /// <summary>有效期。默认2小时</summary>
        public DateTime Expire { get; set; } = DateTime.Now.AddHours(2);

        /// <summary>生效时间，在此之前是无效的</summary>
        public DateTime NotBefore { get; set; }

        /// <summary>颁发时间</summary>
        public DateTime IssuedAt { get; set; }

        /// <summary>标识</summary>
        public String Id { get; set; }

        /// <summary>算法。默认HS256</summary>
        public String Algorithm { get; set; } = "HS256";

        /// <summary>令牌类型。默认JWT</summary>
        public String Type { get; set; }

        /// <summary>密钥</summary>
        public String Secret { get; set; }

        /// <summary>数据项</summary>
        public IDictionary<String, Object> Items { get; private set; }

        /// <summary>设置 或 获取 数据项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Object this[String key] { get => Items?[key]; set => Items[key] = value; }
        #endregion

        #region 构造
        static JwtBuilder()
        {
            RegisterAlgorithm("HS256", (d, s) => d.SHA256(s.GetBytes()), null);
            RegisterAlgorithm("HS384", (d, s) => d.SHA384(s.GetBytes()), null);
            RegisterAlgorithm("HS512", (d, s) => d.SHA512(s.GetBytes()), null);

            RegisterAlgorithm("RS256", RSAHelper.SignSha256, RSAHelper.VerifySha256);
            RegisterAlgorithm("RS384", RSAHelper.SignSha384, RSAHelper.VerifySha384);
            RegisterAlgorithm("RS512", RSAHelper.SignSha512, RSAHelper.VerifySha512);
        }
        #endregion

        #region JWT方法
        /// <summary>编码目标对象，生成令牌</summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public String Encode(Object payload)
        {
            if (Secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Secret));

            var now = DateTime.Now;

            var dic = payload.ToDictionary();
            if (!dic.ContainsKey("iss") && !Issuer.IsNullOrEmpty()) dic["iss"] = Issuer;
            if (!dic.ContainsKey("sub") && !Subject.IsNullOrEmpty()) dic["sub"] = Subject;
            if (!dic.ContainsKey("aud") && !Audience.IsNullOrEmpty()) dic["aud"] = Audience;
            if (!dic.ContainsKey("exp") && Expire.Year > 2000) dic["exp"] = Expire.ToInt();
            if (!dic.ContainsKey("nbf") && NotBefore.Year > 2000) dic["nbf"] = NotBefore.ToInt();
            if (!dic.ContainsKey("iat")) dic["iat"] = (IssuedAt.Year > 2000 ? IssuedAt : now).ToInt();
            if (!dic.ContainsKey("jti") && !Id.IsNullOrEmpty()) dic["jti"] = Id;

            // 头部
            var alg = Algorithm ?? "HS256";
            var hs = new Dictionary<String, Object>
            {
                ["alg"] = alg
            };
            if (!hs.ContainsKey("typ") && !Type.IsNullOrEmpty()) hs["typ"] = Type;
            var header = hs.ToJson().GetBytes().ToUrlBase64();

            // 主题
            var body = dic.ToJson().GetBytes().ToUrlBase64();

            // 签名
            var data = $"{header}.{body}".GetBytes();
            //var sign = alg switch
            //{
            //    "HS256" => data.SHA256(Secret.GetBytes()),
            //    "HS384" => data.SHA384(Secret.GetBytes()),
            //    "HS512" => data.SHA512(Secret.GetBytes()),
            //    "RS256" => RSAHelper.SignSha256(data, Secret),
            //    "RS384" => RSAHelper.SignSha384(data, Secret),
            //    "RS512" => RSAHelper.SignSha512(data, Secret),
            //    "ES256" => ECDsaHelper.SignSha256(data, Secret),
            //    "ES384" => ECDsaHelper.SignSha384(data, Secret),
            //    "ES512" => ECDsaHelper.SignSha512(data, Secret),
            //    _ => throw new InvalidOperationException($"不支持的算法[{alg}]"),
            //};

            if (_encodes.TryGetValue(alg, out var enc) && enc != null)
            {
                var sign = enc(data, Secret);

                return $"{header}.{body}.{sign.ToUrlBase64()}";
            }

            throw new InvalidOperationException($"不支持的算法[{alg}]");
        }

        /// <summary>分析令牌</summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public String[] Parse(String token)
        {
            var ts = token.Split('.');
            if (ts.Length != 3) return null;

            // 头部
            var header = JsonParser.Decode(ts[0].ToBase64().ToStr());
            if (header == null) return null;

            if (header.TryGetValue("alg", out var alg) && alg != null) Algorithm = alg + "";
            if (header.TryGetValue("typ", out var typ)) Type = typ + "";

            // 主体
            var body = JsonParser.Decode(ts[1].ToBase64().ToStr());
            Items = body;

            if (body.TryGetValue("iss", out var value)) Issuer = value + "";
            if (body.TryGetValue("sub", out value)) Subject = value + "";
            if (body.TryGetValue("aud", out value)) Audience = value + "";
            if (body.TryGetValue("exp", out value)) Expire = value.ToDateTime();
            if (body.TryGetValue("nbf", out value)) NotBefore = value.ToDateTime();
            if (body.TryGetValue("iat", out value)) IssuedAt = value.ToDateTime();
            if (body.TryGetValue("jti", out value)) Id = value + "";

            return ts;
        }

        /// <summary>解码令牌，得到目标对象</summary>
        /// <param name="token"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Boolean TryDecode(String token, out String message)
        {
            message = "JWT格式不正确";

            if (Secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Secret));

            var ts = Parse(token);
            if (ts == null) return false;

            // 验证关键字段
            var now = DateTime.Now;
            if (Expire.Year > 2000 && Expire < now)
            {
                message = "令牌已过期";
                return false;
            }
            if (NotBefore.Year > 2000 && now < NotBefore)
            {
                message = "令牌未生效";
                return false;
            }

            message = null;

            // 验证签名
            var data = $"{ts[0]}.{ts[1]}".GetBytes();
            //switch (Algorithm)
            //{
            //    case "RS256": return RSAHelper.VerifySha256(data, Secret, ts[2].ToBase64());
            //    case "RS384": return RSAHelper.VerifySha384(data, Secret, ts[2].ToBase64());
            //    case "RS512": return RSAHelper.VerifySha512(data, Secret, ts[2].ToBase64());
            //    case "ES256": return ECDsaHelper.VerifySha256(data, Secret, ts[2].ToBase64());
            //    case "ES384": return ECDsaHelper.VerifySha384(data, Secret, ts[2].ToBase64());
            //    case "ES512": return ECDsaHelper.VerifySha512(data, Secret, ts[2].ToBase64());
            //}

            //var sec = Secret.GetBytes();
            //var sign = Algorithm switch
            //{
            //    "HS256" => data.SHA256(sec),
            //    "HS384" => data.SHA384(sec),
            //    "HS512" => data.SHA512(sec),
            //    _ => throw new InvalidOperationException($"不支持的算法[{alg}]"),
            //};
            //return sign.ToUrlBase64() == ts[2];

            if (_decodes.TryGetValue(Algorithm, out var dec))
            {
                if (dec != null) return dec(data, Secret, ts[2].ToBase64());

                // 没有验证算法，对比签名
                if (_encodes.TryGetValue(Algorithm, out var enc) && enc != null) return enc(data, Secret).ToUrlBase64() == ts[2];
            }

            throw new InvalidOperationException($"不支持的算法[{Algorithm}]");
        }
        #endregion

        #region 算法管理
        private static IDictionary<String, JwtEncodeDelegate> _encodes = new Dictionary<String, JwtEncodeDelegate>(StringComparer.OrdinalIgnoreCase);
        private static IDictionary<String, JwtDecodeDelegate> _decodes = new Dictionary<String, JwtDecodeDelegate>(StringComparer.OrdinalIgnoreCase);

        /// <summary>注册算法的编解码实现</summary>
        /// <param name="algorithm"></param>
        /// <param name="encode"></param>
        /// <param name="decode"></param>
        public static void RegisterAlgorithm(String algorithm, JwtEncodeDelegate encode, JwtDecodeDelegate decode)
        {
            _encodes[algorithm] = encode;
            _decodes[algorithm] = decode;
        }
        #endregion
    }
}