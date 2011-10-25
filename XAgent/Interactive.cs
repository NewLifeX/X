using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using NewLife.Threading;
using NewLife.Log;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;

namespace XAgent
{
    /// <summary>用户界面交互</summary>
    public static class Interactive
    {
        private static Form _MainForm;
        /// <summary>主窗口</summary>
        public static Form MainForm { get { return _MainForm ?? (_MainForm = new FrmMain()); } set { _MainForm = value; } }

        static Boolean hasShown = false;

        /// <summary>
        /// 隐藏
        /// </summary>
        public static void Hide()
        {
            if (!hasShown) return;

            MainForm.Invoke(new Action<Form>(f => f.Hide()));
        }

        /// <summary>
        /// 显示窗体
        /// </summary>
        public static void ShowForm()
        {
            if (hasShown) return;
            hasShown = true;

            ThreadPoolX.QueueUserWorkItem(Show);
        }
        static void Show()
        {
            GetDesktopWindow();
            IntPtr hwinstaSave = GetProcessWindowStation();
            IntPtr dwThreadId = GetCurrentThreadId();
            IntPtr hdeskSave = GetThreadDesktop(dwThreadId);
            IntPtr hwinstaUser = OpenWindowStation("WinSta0", false, 33554432);
            if (hwinstaUser == IntPtr.Zero)
            {
                RpcRevertToSelf();
                return;
            }
            SetProcessWindowStation(hwinstaUser);
            IntPtr hdeskUser = OpenDesktop("Default", 0, false, 33554432);
            RpcRevertToSelf();
            if (hdeskUser == IntPtr.Zero)
            {
                SetProcessWindowStation(hwinstaSave);
                CloseWindowStation(hwinstaUser);
                return;
            }
            SetThreadDesktop(hdeskUser);

            IntPtr dwGuiThreadId = dwThreadId;

            //Form frm = MainForm;
            //frm.Visible = false;
            //Application.Run(frm);

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Init();
            Application.Run();

            dwGuiThreadId = IntPtr.Zero;
            SetThreadDesktop(hdeskSave);
            SetProcessWindowStation(hwinstaSave);
            CloseDesktop(hdeskUser);
            CloseWindowStation(hwinstaUser);
        }

        #region 托盘图标初始化
        static void Init()
        {
            Container Components = new Container();
            NotifyIcon ni = new NotifyIcon(Components);
            ContextMenuStrip menu = new ContextMenuStrip();

            menu.Size = new Size(153, 98);
            menu.Items.Add("主界面", null, delegate { MainForm.Show(); MainForm.BringToFront(); });
            ToolStripSeparator tsmi = new System.Windows.Forms.ToolStripSeparator();
            tsmi.Size = new Size(menu.Size.Width - 4, 6);
            menu.Items.Add(tsmi);
            menu.Items.Add("退出", null, delegate { Application.Exit(); });

            //ni.Icon = null;
            ni.ContextMenuStrip = menu;
            ni.Visible = true;
            ComponentResourceManager resources = new ComponentResourceManager(typeof(FrmMain));
            //ni.Icon = ((Icon)(resources.GetObject("notifyIcon1.Icon")));
            ni.Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(Interactive), "leaf.ico"));
            ni.Text = "新生命服务代理";
            ni.Visible = true;
            ni.MouseDoubleClick += new MouseEventHandler(ni_MouseDoubleClick);
        }

        static void ni_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                MainForm.Show();
                MainForm.BringToFront();
            }
            catch { MainForm = null; }
        }
        #endregion

        #region 异常捕获
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            XTrace.WriteLine("" + e.ExceptionObject);
            if (e.IsTerminating)
            {
                XTrace.WriteLine("异常退出！");
                XTrace.WriteMiniDump(null);
                MessageBox.Show("" + e.ExceptionObject, "异常退出", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            XTrace.WriteLine(e.Exception.ToString());
        }
        #endregion

        #region PInvoke
        [DllImport("user32.dll")]
        static extern int GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetProcessWindowStation();

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern IntPtr GetThreadDesktop(IntPtr dwThread);

        [DllImport("user32.dll")]
        static extern IntPtr OpenWindowStation(string a, bool b, int c);

        [DllImport("user32.dll")]
        static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll")]
        static extern IntPtr CloseDesktop(IntPtr p);

        [DllImport("rpcrt4.dll", SetLastError = true)]
        static extern IntPtr RpcImpersonateClient(int i);

        [DllImport("rpcrt4.dll", SetLastError = true)]
        static extern IntPtr RpcRevertToSelf();

        [DllImport("user32.dll")]
        static extern IntPtr SetThreadDesktop(IntPtr a);

        [DllImport("user32.dll")]
        static extern IntPtr SetProcessWindowStation(IntPtr a);
        [DllImport("user32.dll")]
        static extern IntPtr CloseWindowStation(IntPtr a);
        #endregion
    }
}