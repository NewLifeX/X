using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using NewLife.Net;
using NewLife.Serialization;

namespace XCom
{
    class UIConfig
    {
        #region 属性
        private Font _Font;
        /// <summary>字体</summary>
        public Font Font { get { return _Font; } set { _Font = value; } }

        private Color _BackColor;
        /// <summary>背景颜色</summary>
        public Color BackColor { get { return _BackColor; } set { _BackColor = value; } }

        private Color _ForeColor;
        /// <summary>前景颜色</summary>
        public Color ForeColor { get { return _ForeColor; } set { _ForeColor = value; } }
        #endregion

        public static UIConfig Load()
        {
            var cfg = SerialPortConfig.Current;
            if (cfg.Extend.IsNullOrWhiteSpace()) return null;

            Byte[] buf = null;
            try
            {
                buf = cfg.Extend.ToBase64();
            }
            catch { return null; }

            var ms = new MemoryStream(buf);

            var binary = new Binary();
            binary.AddHandler<BinaryUnknown>();
            binary.Stream = ms;
            binary.EnableTrace();

            try
            {
                return binary.Read(typeof(UIConfig)) as UIConfig;
            }
            catch { return null; }
        }

        public void Save()
        {
            var binary = new Binary();
            binary.AddHandler<BinaryUnknown>();
            binary.Write(this);
            binary.EnableTrace();

            var cfg = SerialPortConfig.Current;
            cfg.Extend = binary.Stream.ToArray().ToBase64();
            cfg.Save();
        }
    }
}