using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using NewLife;
using NewLife.Log;
using NewLife.Net;
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

            StringHelper.EnableSpeechTip = XConfig.Current.SpeechTip;

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

            if (!Runtime.Mono) new TimerX(s => Runtime.ReleaseMemory(), null, 60000, 60000) { Async = true };

            if (XConfig.Current.IsNew) "学无先后达者为师，欢迎使用新生命码神工具！".SpeechTip();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMDI());
        }

        /// <summary>参数启动</summary>
        static void StartWithParameter(String[] args)
        {
            var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            for (var i = 2; i < args.Length - 1; i++)
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
                default:
                    break;
            }
        }

        static void Render(String mdl, String cfg)
        {
            XTrace.WriteLine("生成代码：模型{0} 配置{1}", mdl, cfg);

            var config = cfg.ToXmlFileEntity<ModelConfig>();
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