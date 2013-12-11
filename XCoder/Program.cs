using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using NewLife;
using NewLife.Log;
using NewLife.Threading;
using NewLife.Xml;
using XCode.DataAccessLayer;

namespace XCoder
{
    static class Program
    {
        /// <summary>应用程序的主入口点。</summary>
        [STAThread]
        static void Main()
        {
            XTrace.UseWinForm();

            // 参数启动
            var args = Environment.GetCommandLineArgs();
            if (args != null && args.Length > 1)
            {
                try
                {
                    StartWithParameter(args);
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
                return;
            }

            try
            {
                Update();

                new TimerX(s => Runtime.ReleaseMemory(), null, 5000, 10000);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMDI());
        }

        /// <summary>参数启动</summary>
        static void StartWithParameter(String[] args)
        {
            var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            for (int i = 2; i < args.Length - 1; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-config":
                    case "-model":
                    case "-connstr":
                    case "-provider":
                    case "-log":
                        dic.Add(args[i].Substring(1), args[++i].Trim('\"'));
                        break;
                    default:
                        break;
                }
            }

            // 转移日志
            var logfile = "";
            if (dic.TryGetValue("Log", out logfile) && !logfile.IsNullOrWhiteSpace())
            {
#if DEBUG
                XTrace.WriteLine("准备切换日志到 {0}", logfile);
#endif

                try
                {
                    var log = TextFileLog.CreateFile(logfile);
                    log.Info("XCoder.exe {0}", String.Join(" ", args));
                    XTrace.Log = log;
                }
                catch (Exception ex) { XTrace.WriteException(ex); }
            }

            switch (args[1].ToLower())
            {
                case "-render":
                    Render(dic["Model"], dic["Config"]);
                    return;
                case "-makemodel":
                    MakeModel(dic["Model"], dic["ConnStr"], dic["Provider"]);
                    return;
                case "-update":
                    Update(false);
                    return;
                default:
                    break;
            }
        }

        static void Update(Boolean isAsync = true)
        {
            if (!isAsync) XTrace.WriteLine("自动更新！");
            if (XConfig.Current.LastUpdate.Date < DateTime.Now.Date)
            {
                XConfig.Current.LastUpdate = DateTime.Now;

                var au = new AutoUpdate();
                if (isAsync)
                    au.UpdateAsync();
                else
                    au.Update();
            }
            var ProcessHelper = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NewLife.ProcessHelper.exe");
            if (File.Exists(ProcessHelper)) File.Delete(ProcessHelper);

            if (isAsync)
            {
                // 释放T4模版
                var b = File.Exists("XCoder.tt");
                var txt = Source.GetText("XCoder.tt");
                txt = txt.Replace("{XCoderPath}", AppDomain.CurrentDomain.BaseDirectory);
                File.WriteAllText("XCoder.tt", txt);

                if (!b) MessageBox.Show("新版本增加XCoder.tt，拷贝到类库项目里面。\r\nVS中修改文件内参数，右键执行自定义工具！", "提示");
            }
        }

        static void Render(String mdl, String cfg)
        {
            XTrace.WriteLine("生成代码：模型{0} 配置{1}", mdl, cfg);

            var config = cfg.ToXmlFileEntity<XConfig>();
            XTrace.WriteLine("模版：{0}", config.TemplateName);
            XTrace.WriteLine("输出：{0}", config.OutputPath);

            var xml = File.ReadAllText(mdl);
            var tables = DAL.Import(xml);

            var engine = new Engine(config);
            engine.Tables = tables;
            foreach (var item in tables)
            {
                XTrace.WriteLine("生成：{0}", item);
                engine.Render(item);
            }

            // 重新整理模型
            Int32 i = 0;
            foreach (var item in tables)
            {
                item.ID = ++i;

                Int32 j = 0;
                foreach (var dc in item.Columns)
                {
                    dc.ID = ++j;
                }
            }

            // 如果有改变，才重新写入模型文件
            var xml2 = DAL.Export(tables);
            if (xml2 != xml) File.WriteAllText(mdl, xml2);
        }

        static void MakeModel(String mdl, String connstr, String provider)
        {
            XTrace.WriteLine("导出模型：ConnStr={0} Provider={1} Model={2}", connstr, provider, mdl);

            var key = "XCoder_Temp";
            DAL.AddConnStr(key, connstr, null, provider);
            var list = DAL.Create(key).Tables;
            var xml = DAL.Export(list);

            var dir = Path.GetDirectoryName(mdl);
            if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(mdl, xml);
        }
    }
}