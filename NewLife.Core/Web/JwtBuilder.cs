using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Serialization;

namespace NewLife.Web
{
    /// <summary>JSON Web Token</summary>
    /// <remarks>
    /// 主要问题：
    /// 1，JWT默认不加密，但可以加密。生成原始令牌后，可以使用改令牌再次对其进行加密。
    /// 2，当JWT未加密时，私密数据无法通过JWT传输。
    /// 3，JWT不仅可用于认证，还可用于信息交换。善用JWT有助于减少服务器请求数据库的次数。
    /// 4，JWT的最大缺点是服务器不保存会话状态，所以在使用期间不可能取消令牌或更改令牌的权限。也就是说，一旦JWT签发，在有效期内将会一直有效。
    /// 5，JWT本身包含认证信息，因此一旦信息泄露，任何人都可以获得令牌的所有权限。为了减少盗用，JWT的有效期不宜设置太长。对于某些重要操作，用户在使用时应该每次都进行进行身份验证。
    /// 6，为了减少盗用和窃取，JWT不建议使用HTTP协议来传输代码，而是使用加密的HTTPS协议进行传输。
    /// </remarks>
    public class JwtBuilder
    {
        #region 属性
        /// <summary>标识</summary>
        public String Id { get; set; }

        /// <summary>主体所有人。可以存放userid/roleid等，作为用户唯一标识</summary>
        public String Subject { get; set; }

        /// <summary>颁发者</summary>
        public String Issuer { get; set; }

        /// <summary>颁发时间</summary>
        public DateTime IssuedAt { get; set; }

        /// <summary>算法。默认HS256</summary>
        public String Algorithm { get; set; } = "HS256";

        /// <summary>令牌类型。默认JWT</summary>
        public String Type { get; set; }

        /// <summary>密钥</summary>
        public String Secret { get; set; }

        /// <summary>有效期。默认2小时</summary>
        public TimeSpan Expire { get; set; } = TimeSpan.FromHours(2);
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
            if (!dic.ContainsKey("iat")) dic["iat"] = (IssuedAt.Year > 2000 ? IssuedAt : now).ToInt();
            if (!dic.ContainsKey("exp") && Expire.TotalSeconds > 10) dic["exp"] = now.Add(Expire);
            if (!dic.ContainsKey("sub") && !Subject.IsNullOrEmpty()) dic["sub"] = Subject;
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
            var sec = Secret.GetBytes();
            var data = $"{header}.{body}".GetBytes();
            Byte[] sign;
            switch (alg)
            {
                case "HS256": sign = data.SHA256(sec); break;
                case "HS384": sign = data.SHA384(sec); break;
                case "HS512": sign = data.SHA512(sec); break;
                default:
                    throw new InvalidOperationException($"不支持的算法[{alg}]");
            }

            return $"{header}.{body}.{sign.ToUrlBase64()}";
        }

        /// <summary>解码令牌，得到目标对象</summary>
        /// <param name="token"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public Boolean TryDecode(String token, out IDictionary<String, Object> payload)
        {
            payload = null;

            if (Secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Secret));

            var ts = token.Split('.');
            if (ts.Length != 3) return false;

            // 头部
            var header = new JsonParser(ts[0].ToBase64().ToStr()).Decode() as IDictionary<String, Object>;
            if (header == null) return false;

            if (header.TryGetValue("alg", out var alg) && alg != null) Algorithm = alg + "";
            if (header.TryGetValue("typ", out var typ)) Type = typ + "";

            // 主体
            var body = new JsonParser(ts[1].ToBase64().ToStr()).Decode() as IDictionary<String, Object>;
            payload = body;

            if (body.TryGetValue("iss", out var value)) Issuer = value + "";
            if (body.TryGetValue("iat", out value)) IssuedAt = value.ToDateTime();
            if (body.TryGetValue("sub", out value)) Subject = value + "";
            if (body.TryGetValue("jti", out value)) Id = value + "";

            // 验证签名
            var sec = Secret.GetBytes();
            var data = $"{ts[0]}.{ts[1]}".GetBytes();
            Byte[] sign;
            switch (alg)
            {
                case "HS256": sign = data.SHA256(sec); break;
                case "HS384": sign = data.SHA384(sec); break;
                case "HS512": sign = data.SHA512(sec); break;
                default:
                    throw new InvalidOperationException($"不支持的算法[{alg}]");
            }

            return sign.ToUrlBase64() == ts[2];
        }
        #endregion
    }
}