using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace XCoder.Tools
{
    [DisplayName("加密解密")]
    public partial class FrmSecurity : Form, IXForm
    {
        public FrmSecurity()
        {
            InitializeComponent();
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
        #endregion

        private void btnExchange_Click(Object sender, EventArgs e)
        {
            var v = rtSource.Text;
            rtSource.Text = rtResult.Text;
            rtResult.Text = v;
        }

        private void btnHex_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            rtResult.Text = buf.ToHex(" ", 32);
        }

        private void btnHex2_Click(Object sender, EventArgs e)
        {
            var v = rtSource.Text;
            rtResult.Text = v.ToHex().ToStr();
        }

        private void btnB64_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            rtResult.Text = buf.ToBase64();
        }

        private void btnB642_Click(Object sender, EventArgs e)
        {
            var v = rtSource.Text;
            //rtResult.Text = v.ToBase64().ToStr();
            var buf = v.ToBase64();
            rtResult.Text = buf.ToStr() + Environment.NewLine + buf.ToHex();
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

            rtResult.Text = buf.ToHex() + Environment.NewLine + Environment.NewLine + buf.ToBase64();
        }

        private void btnDES2_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var pass = GetBytes(rtPass.Text);

            var des = new DESCryptoServiceProvider();
            buf = des.Descrypt(buf, pass);

            rtResult.Text = buf.ToStr() + Environment.NewLine + Environment.NewLine + buf.ToHex() + Environment.NewLine + Environment.NewLine + buf.ToBase64();
        }

        private void btnAES_Click(Object sender, EventArgs e)
        {

        }

        private void btnAES2_Click(Object sender, EventArgs e)
        {

        }

        private void btnRC4_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var pass = GetBytes(rtPass.Text);
            buf = buf.RC4(pass);

            rtResult.Text = buf.ToHex() + Environment.NewLine + Environment.NewLine + buf.ToBase64();
        }

        private void btnRC42_Click(Object sender, EventArgs e)
        {
            var buf = GetBytes();
            var pass = GetBytes(rtPass.Text);
            buf = buf.RC4(pass);

            rtResult.Text = buf.ToStr() + Environment.NewLine + Environment.NewLine + buf.ToHex() + Environment.NewLine + Environment.NewLine + buf.ToBase64();
        }
    }
}