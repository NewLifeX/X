using System.Security.Cryptography;
using NewLife.Configuration;

namespace NewLife.Security;

/// <summary>数据保护者。保护连接字符串中的密码</summary>
public class ProtectedKey
{
    #region 属性
    /// <summary>保护数据的密钥</summary>
    public Byte[] Secret { get; set; }

    /// <summary>算法。默认AES</summary>
    public String Algorithm { get; set; } = "AES";

    /// <summary>隐藏字符串</summary>
    public String HideString { get; set; } = "{***}";

    /// <summary>密码名字</summary>
    public String[] Names { get; set; } = new[] { "password", "pass", "pwd" };
    #endregion

    #region 静态实例
    /// <summary>全局实例。从环境变量和配置文件读取ProtectedKey密钥</summary>
    public static ProtectedKey Instance { get; set; }

    static ProtectedKey()
    {
        var pd = new ProtectedKey();

        var key = Runtime.GetEnvironmentVariable("ProtectedKey");
        if (key.IsNullOrEmpty())
        {
            var config = JsonConfigProvider.LoadAppSettings();
            key = config["ProtectedKey"];
        }

        if (!key.IsNullOrEmpty())
        {
            // 支持Base64格式和Hex格式的密码，默认文本
            if (key.StartsWithIgnoreCase("$Base64$"))
                pd.Secret = key.Substring("$Base64$".Length).ToBase64();
            else if (key.StartsWithIgnoreCase("$Hex$"))
                pd.Secret = key.Substring("$Hex$".Length).ToBase64();
            else
                pd.Secret = key.GetBytes();
        }

        Instance = pd;
    }
    #endregion

    #region 方法
    /// <summary>保护连接字符串中的密码</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public String Protect(String value)
    {
        var alg = Create(Algorithm);

        // 单纯待加密数据
        var p = value.IndexOf('=');
        if (p < 0)
        {
            var pass = alg.Encrypt(value.GetBytes(), Secret).ToUrlBase64();
            return $"${Algorithm}${pass}";
        }

        // 查找密码片段
        var dic = value.SplitAsDictionary("=", ";", true);
        foreach (var item in Names)
        {
            if (dic.TryGetValue(item, out var pass))
            {
                if (pass.IsNullOrEmpty()) break;

                // 加密密码后，重新组装
                pass = alg.Encrypt(pass.GetBytes(), Secret).ToUrlBase64();
                dic[item] = $"${Algorithm}${pass}";

                return dic.Join(";", e => $"{e.Key}={e.Value}");
            }
        }

        return value;
    }

    /// <summary>解保护连接字符串中的密码</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public String Unprotect(String value)
    {
        // 单纯待加密数据
        var p = value.IndexOf('=');
        if (p < 0)
        {
            // 分解加密算法，$AES$string
            var ss = value.Split('$');
            if (ss == null || ss.Length < 3) return value;

            var alg = Create(ss[1]);

            return alg.Decrypt(ss[2].ToBase64(), Secret).ToStr();
        }

        // 查找密码片段
        var dic = value.SplitAsDictionary("=", ";");
        foreach (var item in Names)
        {
            if (dic.TryGetValue(item, out var pass))
            {
                if (pass.IsNullOrEmpty()) break;

                // 分解加密算法，$AES$string
                var ss = pass.Split('$');
                if (ss == null || ss.Length < 3) continue;

                var alg = Create(ss[1]);

                dic[item] = alg.Decrypt(ss[2].ToBase64(), Secret).ToStr();

                return dic.Join(";", e => $"{e.Key}={e.Value}");
            }
        }

        return value;
    }

    /// <summary>隐藏连接字符串中的密码</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public String Hide(String value)
    {
        var dic = value.SplitAsDictionary("=", ";");
        foreach (var item in Names)
        {
            if (dic.TryGetValue(item, out var pass))
            {
                dic[item] = HideString;

                return dic.Join(";", e => $"{e.Key}={e.Value}");
            }
        }

        return value;
    }

    static SymmetricAlgorithm Create(String name)
    {
        return name.ToLowerInvariant() switch
        {
            "aes" => Aes.Create(),
            "des" => DES.Create(),
            "rc2" => RC2.Create(),
            "tripledes" => TripleDES.Create(),
            _ => throw new NotSupportedException($"不支持[{name}]"),
        };
    }
    #endregion
}
