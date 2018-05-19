using System;
using System.IO;
using NewLife.Security;

namespace NewLife.Web
{
    /// <summary>令牌提供者</summary>
    public class TokenProvider
    {
        #region 属性
        /// <summary>密钥。签发方用私钥，验证方用公钥</summary>
        public String Key { get; set; }
        #endregion

        #region 方法
        /// <summary>读取密钥</summary>
        /// <param name="file">文件</param>
        /// <param name="generate">是否生成</param>
        /// <returns></returns>
        public Boolean ReadKey(String file, Boolean generate = false)
        {
            if (file.IsNullOrEmpty()) return false;

            file = file.GetFullPath();
            if (File.Exists(file))
            {
                Key = File.ReadAllText(file);

                if (!Key.IsNullOrEmpty()) return true;
            }

            if (!generate || !file.EndsWithIgnoreCase(".prvkey")) return false;

            var ss = DSAHelper.GenerateKey();
            File.WriteAllText(file.EnsureDirectory(true), ss[0]);
            file = Path.ChangeExtension(file, ".pubkey");
            File.WriteAllText(file, ss[1]);

            Key = ss[0];

            return true;
        }

        /// <summary>编码用户和有效期得到令牌</summary>
        /// <param name="user">用户</param>
        /// <param name="expire">有效期</param>
        /// <returns></returns>
        public String Encode(String user, DateTime expire)
        {
            if (user.IsNullOrEmpty()) throw new ArgumentNullException(nameof(user));
            if (expire.Year < 2000) throw new ArgumentOutOfRangeException(nameof(expire));
            if (Key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Key));

            var secs = expire.ToInt();

            // 拼接数据并签名
            var data = (user + "," + secs).GetBytes();
            var sig = DSAHelper.Sign(data, Key);

            // Base64拼接数据和签名
            return data.ToUrlBase64() + "." + sig.ToUrlBase64();
        }

        /// <summary>令牌解码得到用户和有效期</summary>
        /// <param name="token">令牌</param>
        /// <param name="expire">有效期</param>
        /// <returns></returns>
        public String Decode(String token, out DateTime expire)
        {
            if (token.IsNullOrEmpty()) throw new ArgumentNullException(nameof(token));
            if (Key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Key));
            expire = DateTime.MinValue;

            // Base64拆分数据和签名
            var p = token.IndexOf('.');
            var data = token.Substring(0, p).ToBase64();
            var sig = token.Substring(p + 1).ToBase64();

            // 验证签名
            //if (!DSAHelper.Verify(data, Key, sig)) throw new InvalidOperationException("签名验证失败！");
            if (!DSAHelper.Verify(data, Key, sig)) return null;

            // 拆分数据和有效期
            var str = data.ToStr();
            p = str.LastIndexOf(',');

            var user = str.Substring(0, p);
            var secs = str.Substring(p + 1).ToInt();
            expire = secs.ToDateTime();

            return user;
        }
        #endregion
    }
}