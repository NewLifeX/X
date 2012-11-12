using System;
using System.IO;
using System.Windows.Forms;
using NewLife;
using NewLife.Log;
using NewLife.Threading;

namespace XCoder
{
    static class Program
    {
        /// <summary>应用程序的主入口点。</summary>
        [STAThread]
        static void Main()
        {
            try
            {
                XTrace.UseWinForm();

                if (XConfig.Current.LastUpdate.Date < DateTime.Now.Date)
                {
                    XConfig.Current.LastUpdate = DateTime.Now;

                    var au = new AutoUpdate();
                    au.UpdateAsync();
                }
                var ProcessHelper = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NewLife.ProcessHelper.exe");
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
    }
}