using System;
using NewLife;
using NewLife.Collections;
using NewLife.Security;

namespace XCode.Membership
{
    /// <summary>密码提供者</summary>
    public interface IPasswordProvider
    {
        /// <summary>对密码进行散列处理，此处可以加盐，结果保存在数据库</summary>
        /// <param name="password"></param>
        /// <returns></returns>
        String Hash(String password);

        /// <summary>验证密码散列，包括加盐判断</summary>
        /// <param name="password"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        Boolean Verify(String password, String hash);
    }

    /// <summary>MD5密码提供者</summary>
    public class MD5PasswordProvider : IPasswordProvider
    {
        /// <summary>对密码进行散列处理，此处可以加盐，结果保存在数据库</summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public String Hash(String password) => password.MD5();

        /// <summary>验证密码散列，包括加盐判断</summary>
        /// <param name="password"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public Boolean Verify(String password, String hash) => password.MD5().EqualIgnoreCase(hash);
    }

    /// <summary>盐值密码提供者</summary>
    public class SaltPasswordProvider : IPasswordProvider
    {
        /// <summary>算法。支持md5/sha1/sha512</summary>
        public String Algorithm { get; set; } = "sha512";

        /// <summary>对密码进行散列处理，此处可以加盐，结果保存在数据库</summary>
        /// <param name="password">密码</param>
        /// <returns></returns>
        public String Hash(String password)
        {
            var salt = NextString(16);
            var hash = Algorithm switch
            {
                "md5" => (password.MD5() + salt).MD5(),
                "sha1" => password.GetBytes().SHA1(salt.GetBytes()).ToBase64(),
                "sha512" => password.GetBytes().SHA512(salt.GetBytes()).ToBase64(),
                _ => throw new NotImplementedException(),
            };

            return $"${Algorithm}${salt}${hash}";
        }

        private static readonly String _cs = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789./";

        /// <summary>返回指定长度随机字符串</summary>
        /// <param name="length">长度</param>
        /// <returns></returns>
        private static String NextString(Int32 length)
        {
            var sb = Pool.StringBuilder.Get();
            for (var i = 0; i < length; i++)
            {
                var ch = _cs[Rand.Next(0, _cs.Length)];
                sb.Append(ch);
            }

            return sb.Put(true);
        }

        /// <summary>验证密码散列，包括加盐判断</summary>
        /// <param name="password"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public Boolean Verify(String password, String hash)
        {
            var ss = hash?.Split('$');
            if (ss == null || ss.Length == 0) throw new ArgumentNullException(nameof(hash));

            // 老式MD5，password可能是密码原文，也可能是前端已经md5散列的值
            if (ss.Length == 1) return hash.EqualIgnoreCase(password, password.MD5());

            if (ss.Length != 4) throw new NotSupportedException("不支持的密码哈希值");

            var salt = ss[2];
            switch (ss[1])
            {
                case "md5":
                    if (ss[3] == (password.MD5() + salt).MD5()) return true;
                    return ss[3] == (password + salt).MD5();
                case "sha1":
                    return ss[3] == password.GetBytes().SHA1(salt.GetBytes()).ToBase64();
                case "sha512":
                    return ss[3] == password.GetBytes().SHA512(salt.GetBytes()).ToBase64();
                default:
                    throw new NotSupportedException($"不支持的密码哈希模式[{ss[1]}]");
            }
        }
    }
}