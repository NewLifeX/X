using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using NewLife.Net;
using NewLife.Serialization;

namespace XCoder
{
    /// <summary>界面配置</summary>
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
            var cfg = XConfig.Current;
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

            //binary.Debug = true;
            //binary.EnableTrace();

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

            //binary.Debug = true;
            //binary.EnableTrace();

            binary.Write(this);

            var cfg = XConfig.Current;
            cfg.Extend = binary.GetBytes().ToBase64(0, 0, true);
            cfg.Save();
        }

        public static UIConfig Apply(TextBoxBase txt)
        {
            // 加载颜色
            var ui = UIConfig.Load();
            if (ui != null)
            {
                try
                {
                    txt.Font = ui.Font;
                    txt.BackColor = ui.BackColor;
                    txt.ForeColor = ui.ForeColor;
                }
                catch { ui = null; }
            }
            if (ui == null)
            {
                ui = UIConfig.Current;
                ui.Font = txt.Font;
                ui.BackColor = txt.BackColor;
                ui.ForeColor = txt.ForeColor;
                ui.Save();
            }

            // 菜单控制
            var menu = txt.ContextMenuStrip;
            if (menu != null)
            {
                var ti = Find(menu.Items, "字体", true);
                if (ti == null)
                {
                    menu.Items.Insert(0, new ToolStripSeparator());

                    ti = new ToolStripMenuItem("字体");
                    menu.Items.Add(ti);
                    ti.Click += mi字体_Click;

                    ti = new ToolStripMenuItem("前景色");
                    menu.Items.Add(ti);
                    ti.Click += mi前景色_Click;

                    ti = new ToolStripMenuItem("背景色");
                    menu.Items.Add(ti);
                    ti.Click += mi背景色_Click;
                }
            }

            return ui;
        }

        static ToolStripItem Find(ToolStripItemCollection items, String key, Boolean searchAllChildren)
        {
            var tis = items.Find(key, searchAllChildren);
            if (tis != null && tis.Length > 0) return tis[0];

            foreach (ToolStripItem item in items)
            {
                if (item.Text.EqualIgnoreCase(key)) return item;
            }
            if (searchAllChildren)
            {
                foreach (ToolStripItem item in items)
                {
                    var tdi = item as ToolStripDropDownItem;
                    if (tdi != null)
                    {
                        var ti = Find(tdi.DropDownItems, key, searchAllChildren);
                        if (ti != null) return ti;
                    }
                }
            }

            return null;
        }

        static void mi字体_Click(Object sender, EventArgs e)
        {
            var ti = sender as ToolStripItem;
            var txt = (ti.Owner as ContextMenuStrip).SourceControl as TextBoxBase;

            var fd = new FontDialog();
            fd.Font = txt.Font;
            if (fd.ShowDialog() != DialogResult.OK) return;

            txt.Font = fd.Font;

            var ui = UIConfig.Current;
            ui.Font = txt.Font;
            ui.Save();
        }

        static void mi前景色_Click(Object sender, EventArgs e)
        {
            var ti = sender as ToolStripItem;
            var txt = (ti.Owner as ContextMenuStrip).SourceControl as TextBoxBase;

            var cd = new ColorDialog();
            cd.Color = txt.ForeColor;
            if (cd.ShowDialog() != DialogResult.OK) return;

            txt.ForeColor = cd.Color;

            var ui = UIConfig.Current;
            ui.ForeColor = txt.ForeColor;
            ui.Save();
        }

        static void mi背景色_Click(Object sender, EventArgs e)
        {
            // ((System.Windows.Forms.ContextMenuStrip)(((System.Windows.Forms.ToolStripItem)(sender)).Owner)).SourceControl
            var ti = sender as ToolStripItem;
            var txt = (ti.Owner as ContextMenuStrip).SourceControl as TextBoxBase;

            var cd = new ColorDialog();
            cd.Color = txt.BackColor;
            if (cd.ShowDialog() != DialogResult.OK) return;

            txt.BackColor = cd.Color;

            var ui = UIConfig.Current;
            ui.BackColor = txt.BackColor;
            ui.Save();
        }
    }
}