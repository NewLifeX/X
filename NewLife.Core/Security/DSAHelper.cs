using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Xml;

namespace NewLife.Security;

/// <summary>DSA算法</summary>
public static class DSAHelper
{
    #region 产生密钥
    /// <summary>产生非对称密钥对（私钥和公钥）</summary>
    /// <param name="keySize">密钥长度，默认1024位强密钥</param>
    /// <returns>私钥和公钥</returns>
    public static String[] GenerateKey(Int32 keySize = 1024)
    {
        var dsa = new DSACryptoServiceProvider(keySize);

        var ss = new String[2];
        _ = dsa.ExportParameters(true);
        ss[0] = dsa.ToXmlStringX(true);
        ss[1] = dsa.ToXmlStringX(false);

        return ss;
    }

    #endregion

    #region 数字签名
    /// <summary>签名</summary>
    /// <param name="buf"></param>
    /// <param name="priKey"></param>
    /// <returns></returns>
    public static Byte[] Sign(Byte[] buf, String priKey)
    {
        var dsa = new DSACryptoServiceProvider();
        dsa.FromXmlStringX(priKey);

        return dsa.SignData(buf);
    }

    /// <summary>验证</summary>
    /// <param name="buf"></param>
    /// <param name="pukKey"></param>
    /// <param name="rgbSignature"></param>
    /// <returns></returns>
    public static Boolean Verify(Byte[] buf, String pukKey, Byte[] rgbSignature)
    {
        var dsa = new DSACryptoServiceProvider();
        dsa.FromXmlStringX(pukKey);

        return dsa.VerifyData(buf, rgbSignature);
    }
    #endregion

    #region 兼容core
    /// <summary>从Xml加载DSA密钥</summary>
    /// <param name="rsa"></param>
    /// <param name="xmlString"></param>
    public static void FromXmlStringX(this DSACryptoServiceProvider rsa, String xmlString)
    {
        var parameters = new DSAParameters();

        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlString);

        if (xmlDoc.DocumentElement == null || !xmlDoc.DocumentElement.Name.Equals("DSAKeyValue"))
        {
            throw new Exception(L.T("Invalid XML DSA key."));
        }

        foreach (var item in xmlDoc.DocumentElement.ChildNodes)
        {
            if (item is not XmlNode node) continue;
            switch (node.Name)
            {
                case "P": parameters.P = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                case "Q": parameters.Q = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                case "G": parameters.G = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                case "Y": parameters.Y = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                case "Seed": parameters.Seed = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                // PgenCounter 有两种格式：本库 ToXmlStringX 写入的十进制整数，以及历史/外部密钥写入的 Base64 编码整数
                case "PgenCounter":
                case "Counter": parameters.Counter = ParseCounter(node.InnerText); break;
                case "X": parameters.X = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
            }
        }

        rsa.ImportParameters(parameters);
    }

    /// <summary>保存DSA密钥到Xml</summary>
    /// <param name="rsa"></param>
    /// <param name="includePrivateParameters"></param>
    /// <returns></returns>
    public static String ToXmlStringX(this DSACryptoServiceProvider rsa, Boolean includePrivateParameters)
    {
        var parameters = rsa.ExportParameters(includePrivateParameters);

        return String.Format("<DSAKeyValue><P>{0}</P><Q>{1}</Q><G>{2}</G><Y>{3}</Y><Seed>{4}</Seed><PgenCounter>{5}</PgenCounter><X>{6}</X></DSAKeyValue>",
            parameters.P != null ? Convert.ToBase64String(parameters.P) : null,
            parameters.Q != null ? Convert.ToBase64String(parameters.Q) : null,
            parameters.G != null ? Convert.ToBase64String(parameters.G) : null,
            parameters.Y != null ? Convert.ToBase64String(parameters.Y) : null,
            parameters.Seed != null ? Convert.ToBase64String(parameters.Seed) : null,
            parameters.Counter,
            parameters.X != null ? Convert.ToBase64String(parameters.X) : null);
    }

    /// <summary>解析DSA计数器</summary>
    /// <remarks>
    /// 兼容两种格式：十进制整数（本库 ToXmlStringX 写入），以及 Base64 编码整数（历史密钥或外部工具写入，如旧版 NewLife / OpenSSL 转换）。
    /// Counter 仅用于密钥生成验证，不影响签名验签，解析失败时返回 0。
    /// </remarks>
    /// <param name="text">XML 节点文本</param>
    /// <returns>计数器</returns>
    static Int32 ParseCounter(String text)
    {
        if (String.IsNullOrEmpty(text)) return 0;
        if (Int32.TryParse(text, out var counter)) return counter;

        // Base64 编码的整数，可能不足 4 字节，按小端补零
        try
        {
            var buf = Convert.FromBase64String(text);
            if (buf.Length == 0) return 0;
            if (buf.Length >= 4) return BinaryPrimitives.ReadInt32LittleEndian(buf);

            var arr = new Byte[4];
            Buffer.BlockCopy(buf, 0, arr, 0, buf.Length);
            return BinaryPrimitives.ReadInt32LittleEndian(arr);
        }
        catch { return 0; }
    }
    #endregion
}