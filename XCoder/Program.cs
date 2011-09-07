using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NewLife.Log;

namespace XCoder
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);

            if (XConfig.Current.LastUpdate.Date < DateTime.Now.Date)
            {
                XConfig.Current.LastUpdate = DateTime.Now;

                AutoUpdate au = new AutoUpdate();
                au.LocalVersion = new Version(Engine.FileVersion);
                au.VerSrc = "http://files.cnblogs.com/nnhy/XCoderVer.xml";
                au.ProcessAsync();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            XTrace.WriteLine(e.Exception.ToString());
        }
    }
}