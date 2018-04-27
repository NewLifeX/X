using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Windows.Forms;
using NewLife.Security;

namespace XCoder.Tools
{
    [DisplayName("加密解密")]
    public partial class FrmSecurity : Form, IXForm
    {
        public FrmSecurity()
        {
            InitializeComponent();

            // 动态调节宽度高度，兼容高DPI
            this.FixDpi();
        }

        #region 辅助
        /// <summary>从字符串中获取字节数组</summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private Byte[] GetBytes(String str)
        {
            if (str.IsNullOrEmpty()) return new Byte[0];

            try
            {
                return str.ToHex();
            }
            catch { }

            try
            {
                return str.ToBase64();
            }
            catch { }

            return str.GetBytes();
        }

        /// <summary>从原文中获取字节数组</summary>
        /// <returns></returns>
        private Byte[] GetBytes()
        {
            var v = rtSource.Text;
            return GetBytes(v);
        }

        private void SetResult(params String[] rs)
        {
            var sb = new StringBuilder();
            foreach (var item in rs)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine();
                }
                sb.Append(item);
            }
            rtResult.Text = sb.ToString();
        }

        private void SetResult(Byte[] data)
        {
            SetResult("/*HEX编码、Base64编码、Url改进Base64编码*/", data.ToHex(), data.ToBase64(), data.ToUrlBase64());
        }

        private void SetResult2(Byte[] data)
        {
            SetResult("/*字符串、HEX编码、Base64编码*/", data.ToStr(), data.ToHex(), data.ToBase64());
        }
        #endregion

        private void btnExchange_Click(Object sender, EventArgs e)
        {
            var v = rtSource.Text;
            var v2 = rtResult.Text;
            // 结果区只要第一行
            if (!v2.IsNullOrEmpty())
            {
                var ss = v2.Split("\n");
                var n = 0;
                if (ss.Length > n + 1 && ss[n].StartsWith("/*") && ss[n].EndsWith("*/")) n++;
                v2 = ss[n];
            }
            rtSource.Text = v2;
            rtResult.Text = v;
        }

        private void btnHex_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            //rtResult.Text = buf.ToHex(" ", 32);
            SetResult(buf.ToHex(), buf.ToHex(" ", 32), buf.ToHex("-", 32));
        }

        private void btnHex2_Click(Object sender, EventArgs e)
        {
            var v = rtSource.Text;
            rtResult.Text = v.ToHex().ToStr();
        }

        private void btnB64_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            //rtResult.Text = buf.ToBase64();
            SetResult(buf.ToBase64(), buf.ToUrlBase64());
        }

        private void btnB642_Click(Object sender, EventArgs e)
        {
            var v = rtSource.Text;
            //rtResult.Text = v.ToBase64().ToStr();
            var buf = v.ToBase64();
            //rtResult.Text = buf.ToStr() + Environment.NewLine + buf.ToHex();
            SetResult(buf.ToStr(), buf.ToHex());
        }

        private void btnMD5_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var str = buf.MD5().ToHex();
            rtResult.Text = str.ToUpper() + Environment.NewLine + str.ToLower();
        }

        private void btnMD52_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var str = buf.MD5().ToHex(0, 8);
            rtResult.Text = str.ToUpper() + Environment.NewLine + str.ToLower();
        }

        private void btnSHA1_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var key = GetBytes(rtPass.Text);

            buf = buf.SHA1(key);
            SetResult(buf);
        }

        private void btnSHA256_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var key = GetBytes(rtPass.Text);

            buf = buf.SHA256(key);
            SetResult(buf);
        }

        private void btnSHA384_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var key = GetBytes(rtPass.Text);

            buf = buf.SHA384(key);
            SetResult(buf);
        }

        private void btnSHA512_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var key = GetBytes(rtPass.Text);

            buf = buf.SHA512(key);
            SetResult(buf);
        }

        private void btnCRC_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            rtResult.Text = "{0:X8}\r\n{0}".F(buf.Crc());
        }

        private void btnCRC2_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            rtResult.Text = "{0:X4}\r\n{0}".F(buf.Crc16());
        }

        private void btnDES_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var pass = GetBytes(rtPass.Text);

            var des = new DESCryptoServiceProvider();
            buf = des.Encrypt(buf, pass);

            SetResult(buf);
        }

        private void btnDES2_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var pass = GetBytes(rtPass.Text);

            var des = new DESCryptoServiceProvider();
            buf = des.Descrypt(buf, pass);

            SetResult2(buf);
        }

        private void btnAES_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var pass = GetBytes(rtPass.Text);

            var aes = new AesCryptoServiceProvider();
            buf = aes.Encrypt(buf, pass);

            SetResult(buf);
        }

        private void btnAES2_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var pass = GetBytes(rtPass.Text);

            var aes = new AesCryptoServiceProvider();
            buf = aes.Descrypt(buf, pass);

            SetResult2(buf);
        }

        private void btnRC4_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var pass = GetBytes(rtPass.Text);
            buf = buf.RC4(pass);

            SetResult(buf);
        }

        private void btnRC42_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var pass = GetBytes(rtPass.Text);
            buf = buf.RC4(pass);

            SetResult2(buf);
        }

        private void btnRSA_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var key = rtPass.Text;

            if (key.Length < 100)
            {
                key = RSAHelper.GenerateKey().First();
                rtPass.Text = key;
            }

            buf = RSAHelper.Encrypt(buf, key);

            SetResult(buf);
        }

        private void btnRSA2_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var pass = rtPass.Text;

            try
            {
                buf = RSAHelper.Decrypt(buf, pass, true);
            }
            catch (CryptographicException)
            {
                // 换一种填充方式
                buf = RSAHelper.Decrypt(buf, pass, false);
            }

            SetResult2(buf);
        }

        private void btnDSA_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var key = rtPass.Text;

            if (key.Length < 100)
            {
                key = DSAHelper.GenerateKey().First();
                rtPass.Text = key;
            }

            buf = DSAHelper.Sign(buf, key);

            SetResult(buf);
        }

        private void btnDSA2_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var pass = rtPass.Text;

            var v = rtResult.Text;
            if (v.Contains("\n\n")) v = v.Substring(null, "\n\n");
            var sign = GetBytes(v);

            var rs = DSAHelper.Verify(buf, pass, sign);
            if (rs)
                MessageBox.Show("验证通过", "DSA数字签名", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("验证失败", "DSA数字签名", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btnUrl_Click(Object sender, EventArgs e)
        {
            var v = rtSource.Text;
            v = HttpUtility.UrlEncode(v);
            rtResult.Text = v;
        }

        private void btnUrl2_Click(Object sender, EventArgs e)
        {
            var v = rtSource.Text;
            v = HttpUtility.UrlDecode(v);
            rtResult.Text = v;
        }

        private void btnHtml_Click(Object sender, EventArgs e)
        {
            var v = rtSource.Text;
            v = HttpUtility.HtmlEncode(v);
            rtResult.Text = v;
        }

        private void btnHtml2_Click(Object sender, EventArgs e)
        {
            var v = rtSource.Text;
            v = HttpUtility.HtmlDecode(v);
            rtResult.Text = v;
        }
    }
}