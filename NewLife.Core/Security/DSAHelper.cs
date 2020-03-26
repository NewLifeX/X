using System;
using System.Security.Cryptography;
using System.Xml;

namespace NewLife.Security
{
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
            var pa = dsa.ExportParameters(true);
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
                throw new Exception("Invalid XML DSA key.");
            }

            foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {
                    case "P": parameters.P = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    case "Q": parameters.Q = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    case "G": parameters.G = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    case "Y": parameters.Y = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    case "Seed": parameters.Seed = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    case "Counter": parameters.Counter = Convert.ToInt32(node.InnerText); break;
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
        #endregion
    }
}