using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using NewLife.Threading;

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

            Form frm = MainForm;
            frm.Visible = false;
            Application.Run(frm);

            dwGuiThreadId = IntPtr.Zero;
            SetThreadDesktop(hdeskSave);
            SetProcessWindowStation(hwinstaSave);
            CloseDesktop(hdeskUser);
            CloseWindowStation(hwinstaUser);
        }

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