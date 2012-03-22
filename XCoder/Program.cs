using System;
using System.Windows.Forms;
using NewLife;
using NewLife.Log;
using NewLife.Threading;
using System.IO;

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
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

                if (XConfig.Current.LastUpdate.Date < DateTime.Now.Date)
                {
                    XConfig.Current.LastUpdate = DateTime.Now;

                    var au = new AutoUpdate();
                    au.UpdateAsync();
                }
                String ProcessHelper = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NewLife.ProcessHelper.exe");
                if (File.Exists(ProcessHelper)) File.Delete(ProcessHelper);

                new TimerX(s => Runtime.ReleaseMemory(), null, 5000, 10000);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            XTrace.WriteLine("" + e.ExceptionObject);
            if (e.IsTerminating)
            {
                XTrace.WriteLine("异常退出！");
                //XTrace.WriteMiniDump(null);
                MessageBox.Show("" + e.ExceptionObject, "异常退出", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            XTrace.WriteLine(e.Exception.ToString());
        }
    }
}