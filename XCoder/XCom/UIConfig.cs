using System;
using System.Drawing;
using System.IO;
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

        private static UIConfig _Current;
        /// <summary>当前配置</summary>
        public static UIConfig Current
        {
            get
            {
                if (_Current == null) _Current = Load() ?? new UIConfig();
                return _Current;
            }
        }

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
            binary.EncodeInt = true;
            binary.AddHandler<BinaryFont>(11);
            binary.AddHandler<BinaryColor>(12);
            binary.AddHandler<BinaryUnknown>(20);
            binary.Stream = ms;
//#if DEBUG
            binary.Debug = true;
            binary.EnableTrace();
//#endif

            try
            {
                return binary.Read(typeof(UIConfig)) as UIConfig;
            }
            catch { return null; }
        }

        public void Save()
        {
            var binary = new Binary();
            binary.EncodeInt = true;
            binary.AddHandler<BinaryFont>(11);
            binary.AddHandler<BinaryColor>(12);
            binary.AddHandler<BinaryUnknown>(20);
//#if DEBUG
            binary.Debug = true;
            binary.EnableTrace();
//#endif
            binary.Write(this);

            var cfg = SerialPortConfig.Current;
            cfg.Extend = binary.GetBytes().ToBase64(0, 0, true);
            cfg.Save();
        }
    }
}