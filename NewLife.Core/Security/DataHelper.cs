using System;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NewLife.Security
{
    /// <summary>数据助手</summary>
    public static class DataHelper
    {
        #region 散列
        /// <summary>MD5散列</summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String Hash(String str)
        {
            //if (String.IsNullOrEmpty(str)) throw new ArgumentNullException("str");
            if (String.IsNullOrEmpty(str)) return null;

            var md5 = new MD5CryptoServiceProvider();
            var by = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
            return BitConverter.ToString(by).Replace("-", "");
        }

        /// <summary>文件散列</summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static String HashFile(String filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException("filename");

            var md5 = new MD5CryptoServiceProvider();
            //Byte[] buffer = md5.ComputeHash(File.ReadAllBytes(filename));
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                var buffer = md5.ComputeHash(stream);
                return BitConverter.ToString(buffer).Replace("-", "");
            }
        }
        #endregion

        #region TripleDES加解密
        private static SymmetricAlgorithm GetProvider(String key)
        {
            var sa = new TripleDESCryptoServiceProvider();
            Int32 max = sa.LegalKeySizes[0].MaxSize / 8;
            key = Hash(key);
            String str = key;
            Byte[] bts = Encoding.UTF8.GetBytes(str);

            if (bts.Length != max) Array.Resize<Byte>(ref bts, max);

            sa.Key = bts;

            max = sa.LegalBlockSizes[0].MaxSize / 8;
            bts = Encoding.UTF8.GetBytes(str);
            //倒序
            Array.Reverse(bts);
            if (bts.Length != max) Array.Resize<Byte>(ref bts, max);
            sa.IV = bts;

            return sa;
        }

        /// <summary>TripleDES加密</summary>
        /// <param name="content">UTD8编码的明文</param>
        /// <param name="key">密码字符串经MD5散列后作为DES密码</param>
        /// <returns></returns>
        public static String Encrypt(String content, String key)
        {
            if (String.IsNullOrEmpty(content)) throw new ArgumentNullException("content");
            var data = Encoding.UTF8.GetBytes(content);

            data = Encrypt(data, key);

            return Convert.ToBase64String(data);
        }

        /// <summary>TripleDES加密</summary>
        /// <param name="data"></param>
        /// <param name="key">密码字符串经MD5散列后作为DES密码</param>
        /// <returns></returns>
        public static Byte[] Encrypt(Byte[] data, String key)
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException("data");
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException("key");

            var outstream = new MemoryStream();
            var stream = new CryptoStream(outstream, GetProvider(key).CreateEncryptor(), CryptoStreamMode.Write);
            stream.Write(data, 0, data.Length);
            stream.FlushFinalBlock();

            data = outstream.ToArray();

            stream.Close();
            outstream.Close();

            return data;
        }

        /// <summary>TripleDES解密</summary>
        /// <param name="content">UTD8编码的密文</param>
        /// <param name="key">密码字符串经MD5散列后作为DES密码</param>
        /// <returns></returns>
        public static String Descrypt(String content, String key)
        {
            if (String.IsNullOrEmpty(content)) throw new ArgumentNullException("content");
            Byte[] data = Convert.FromBase64String(content);

            data = Descrypt(data, key);

            return Encoding.UTF8.GetString(data);
        }

        /// <summary>TripleDES解密</summary>
        /// <param name="data"></param>
        /// <param name="key">密码字符串经MD5散列后作为DES密码</param>
        /// <returns></returns>
        public static Byte[] Descrypt(Byte[] data, String key)
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException("data");
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException("key");

            var ms = new MemoryStream(data);
            var stream = new CryptoStream(ms, GetProvider(key).CreateDecryptor(), CryptoStreamMode.Read);

            var ms2 = new MemoryStream();
            while (true)
            {
                Byte[] buffer = new Byte[10];
                Int32 count = stream.Read(buffer, 0, buffer.Length);
                if (count <= 0) break;

                ms2.Write(buffer, 0, count);
                if (count < buffer.Length) break;
            }

            data = ms2.ToArray();

            stream.Close();
            ms.Close();
            ms2.Close();

            return data;
        }
        #endregion

        #region RC4加密
        /// <summary>RC4加密解密</summary>
        /// <param name="data">数据</param>
        /// <param name="pass">UTF8编码的密码</param>
        /// <returns></returns>
        public static Byte[] RC4(Byte[] data, String pass)
        {
            if (data == null || pass == null) return null;
            Byte[] output = new Byte[data.Length];
            Int64 i = 0;
            Int64 j = 0;
            Byte[] mBox = GetKey(Encoding.UTF8.GetBytes(pass), 256);

            // 加密
            for (Int64 offset = 0; offset < data.Length; offset++)
            {
                i = (i + 1) % mBox.Length;
                j = (j + mBox[i]) % mBox.Length;
                Byte temp = mBox[i];
                mBox[i] = mBox[j];
                mBox[j] = temp;
                Byte a = data[offset];
                //Byte b = mBox[(mBox[i] + mBox[j] % mBox.Length) % mBox.Length];
                // mBox[j] 一定比 mBox.Length 小，不需要在取模
                Byte b = mBox[(mBox[i] + mBox[j]) % mBox.Length];
                output[offset] = (Byte)((Int32)a ^ (Int32)b);
            }

            return output;
        }

        /// <summary>打乱密码</summary>
        /// <param name="pass">密码</param>
        /// <param name="kLen">密码箱长度</param>
        /// <returns>打乱后的密码</returns>
        static Byte[] GetKey(Byte[] pass, Int32 kLen)
        {
            Byte[] mBox = new Byte[kLen];

            for (Int64 i = 0; i < kLen; i++)
            {
                mBox[i] = (Byte)i;
            }
            Int64 j = 0;
            for (Int64 i = 0; i < kLen; i++)
            {
                j = (j + mBox[i] + pass[i % pass.Length]) % kLen;
                Byte temp = mBox[i];
                mBox[i] = mBox[j];
                mBox[j] = temp;
            }
            return mBox;
        }
        #endregion

        #region RSA签名
        /// <summary>签名</summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns>Base64编码的签名</returns>
        internal static String Sign(Byte[] data, String priKey)
        {
            if (data == null | String.IsNullOrEmpty(priKey)) return null;

            var rsa = new RSACryptoServiceProvider();
            var md5 = new MD5CryptoServiceProvider();
            try
            {
                rsa.FromXmlString(priKey);
                return Convert.ToBase64String(rsa.SignHash(md5.ComputeHash(data), "1.2.840.113549.2.5"));
            }
            catch { return null; }
        }
        #endregion

        #region RSA验证签名
        /// <summary>验证签名</summary>
        /// <param name="data">待验证的数据</param>
        /// <param name="signdata">Base64编码的签名</param>
        /// <param name="pubKey">公钥</param>
        /// <returns></returns>
        internal static Boolean Verify(Byte[] data, String signdata, String pubKey)
        {
            if (data == null ||
                data.Length < 1 ||
                String.IsNullOrEmpty(signdata) ||
                String.IsNullOrEmpty(pubKey)) return false;

            var rsa = new RSACryptoServiceProvider();
            var md5 = new MD5CryptoServiceProvider();
            try
            {
                rsa.FromXmlString(pubKey);
                return rsa.VerifyHash(md5.ComputeHash(data), "1.2.840.113549.2.5", Convert.FromBase64String(signdata));
            }
            catch { return false; }
        }
        #endregion

        #region 编码
        /// <summary>把字节数组编码为十六进制字符串</summary>
        /// <param name="data"></param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        [Obsolete("=>IOHelper.ToHex")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static String ToHex(this Byte[] data, Int32 offset = 0, Int32 count = 0) { return IOHelper.ToHex(data, offset, count); }

        /// <summary>把十六进制字符串解码字节数组</summary>
        /// <param name="data"></param>
        /// <param name="startIndex">起始位置</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        [Obsolete("=>IOHelper.ToHex")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Byte[] FromHex(String data, Int32 startIndex = 0, Int32 length = 0) { return IOHelper.ToHex(data, startIndex, length); }
        #endregion
    }
}