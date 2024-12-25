using NewLife.Collections;

namespace NewLife.Security;

/// <summary>密码提供者</summary>
public interface IPasswordProvider
{
    /// <summary>对密码进行散列处理，此处可以加盐，结果保存在数据库</summary>
    /// <param name="password">密码明文</param>
    /// <returns></returns>
    String Hash(String password);

    /// <summary>验证密码散列，包括加盐判断</summary>
    /// <param name="password">传输密码。可能是明文、MD5</param>
    /// <param name="hash">哈希密文。服务端数据库保存，带有算法、盐值、哈希值</param>
    /// <returns></returns>
    Boolean Verify(String password, String hash);
}

/// <summary>默认密码提供者</summary>
public class PasswordProvider : IPasswordProvider
{
    /// <summary>对密码进行散列处理，此处可以加盐，结果保存在数据库</summary>
    /// <param name="password">密码明文</param>
    /// <returns></returns>
    public String Hash(String password) => password;

    /// <summary>验证密码散列，包括加盐判断</summary>
    /// <param name="password">传输密码。可能是明文、MD5</param>
    /// <param name="hash">哈希密文。服务端数据库保存，带有算法、盐值、哈希值</param>
    /// <returns></returns>
    public Boolean Verify(String password, String hash) => password.EqualIgnoreCase(hash);
}

/// <summary>MD5密码提供者</summary>
public class MD5PasswordProvider : IPasswordProvider
{
    /// <summary>对密码进行散列处理，此处可以加盐，结果保存在数据库</summary>
    /// <param name="password">密码明文</param>
    /// <returns></returns>
    public String Hash(String password) => password.MD5();

    /// <summary>验证密码散列，包括加盐判断</summary>
    /// <param name="password">传输密码。可能是明文、MD5</param>
    /// <param name="hash">哈希密文。服务端数据库保存，带有算法、盐值、哈希值</param>
    /// <returns></returns>
    public Boolean Verify(String password, String hash) => hash.EqualIgnoreCase(password, password.MD5());
}

/// <summary>盐值密码提供者</summary>
/// <remarks>
/// 1，在Web应用中，数据库保存哈希密码hash，登录时传输密码明文pass，服务端验证密码。算法配置为md5+sha512时，传输MD5散列。
/// 2，在App验证时，数据库保存密码明文pass，登录时传输哈希密码hash，服务端验证密码。
/// </remarks>
public class SaltPasswordProvider : IPasswordProvider
{
    /// <summary>算法。支持md5/sha1/sha512</summary>
    public String Algorithm { get; set; } = "sha512";

    /// <summary>使用Unix秒作为盐值。该值为允许的最大时间差，默认0，不使用时间盐值，而是使用随机字符串</summary>
    /// <remarks>一般在传输中使用，避免临时盐值被截取作为它用，建议值30秒。不仅仅是传输耗时，还有两端时间差</remarks>
    public Int32 SaltTime { get; set; }

    /// <summary>对密码进行散列处理，此处可以加盐，结果保存在数据库</summary>
    /// <param name="password">密码明文</param>
    /// <returns></returns>
    public String Hash(String password)
    {
        var salt = CreateSalt();
        var hash = Algorithm switch
        {
            "md5" => (password.MD5() + salt).MD5(),
            "sha1" => password.GetBytes().SHA1(salt.GetBytes()).ToBase64(),
            "sha512" => password.GetBytes().SHA512(salt.GetBytes()).ToBase64(),
            "md5+sha1" => password.GetBytes().MD5().SHA1(salt.GetBytes()).ToBase64(),
            "md5+sha512" => password.GetBytes().MD5().SHA512(salt.GetBytes()).ToBase64(),
            _ => throw new NotImplementedException(),
        };

        return $"${Algorithm}${salt}${hash}";
    }

    private static readonly String _cs = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789./";

    /// <summary>创建盐值</summary>
    /// <returns></returns>
    protected virtual String CreateSalt()
    {
        if (SaltTime > 0) return DateTime.UtcNow.ToInt().ToString();

        var length = 16;
        var sb = Pool.StringBuilder.Get();
        for (var i = 0; i < length; i++)
        {
            var ch = _cs[Rand.Next(0, _cs.Length)];
            sb.Append(ch);
        }

        return sb.Return(true);
    }

    /// <summary>验证密码散列，包括加盐判断</summary>
    /// <param name="password">传输密码。可能是明文、MD5</param>
    /// <param name="hash">哈希密文。服务端数据库保存，带有算法、盐值、哈希值</param>
    /// <returns></returns>
    public virtual Boolean Verify(String password, String hash)
    {
        var ss = hash?.Split('$');
        if (ss == null || ss.Length == 0) throw new ArgumentNullException(nameof(hash));

        // 老式MD5，password可能是密码原文，也可能是前端已经md5散列的值。数据库里刚好也是明文或MD5散列
        if (ss.Length == 1) return hash.EqualIgnoreCase(password, password.MD5());

        if (ss.Length != 4) throw new NotSupportedException("Unsupported password hash value");

        var salt = ss[2];
        if (SaltTime > 0)
        {
            // Unix秒作为盐值，时间差不得超过 SaltTime
            var t = DateTime.UtcNow.ToInt() - salt.ToInt();
            if (Math.Abs(t) > SaltTime) return false;
        }

        switch (ss[1])
        {
            case "md5":
                // 传输密码是明文
                if (ss[3] == (password.MD5() + salt).MD5()) return true;
                // 传输密码是MD5哈希
                return ss[3] == (password + salt).MD5();
            case "sha1":
                return ss[3] == password.GetBytes().SHA1(salt.GetBytes()).ToBase64();
            case "sha512":
                return ss[3] == password.GetBytes().SHA512(salt.GetBytes()).ToBase64();
            case "md5+sha1":
                // 标准校验
                if (ss[3] == password.GetBytes().MD5().SHA1(salt.GetBytes()).ToBase64()) return true;
                // 兼容sha1。不大可能出现
                if (ss[3] == password.GetBytes().SHA1(salt.GetBytes()).ToBase64()) return true;
                // 传输密码是MD5哈希
                if (ss[3] == password.ToHex().SHA1(salt.GetBytes()).ToBase64()) return true;
                return false;
            case "md5+sha512":
                // 标准校验
                if (ss[3] == password.GetBytes().MD5().SHA512(salt.GetBytes()).ToBase64()) return true;
                // 兼容sha512。不大可能出现
                if (ss[3] == password.GetBytes().SHA512(salt.GetBytes()).ToBase64()) return true;
                // 传输密码是MD5哈希
                if (ss[3] == password.ToHex().SHA512(salt.GetBytes()).ToBase64()) return true;
                return false;
            default:
                throw new NotSupportedException($"Unsupported password hash mode [{ss[1]}]");
        }
    }
}