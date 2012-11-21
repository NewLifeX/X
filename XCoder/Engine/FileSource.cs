using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

namespace XCoder
{
    /// <summary>文件资源</summary>
    public static class FileSource
    {
        public static Icon GetIcon()
        {
            return new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(FileSource), "leaf.ico"));
        }

        public static String GetText(String name)
        {
            if (Path.GetExtension(name).IsNullOrWhiteSpace()) name += ".txt";
            var ms = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(FileSource), name);
            return ms.ToStr();
        }

        public static void ReleaseAllTemplateFiles()
        {
            String path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Engine.TemplatePath);
            Dictionary<String, String> dic = GetTemplates();

            foreach (String item in dic.Keys)
            {
                // 第一层是目录，然后是文件
                String dir = item.Substring(0, item.IndexOf("."));
                String file = item.Substring(dir.Length + 1);

                dir = Path.Combine(path, dir);
                file = Path.Combine(dir, file);

                if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(file, dic[item]);
            }
        }

        /// <summary>释放模版文件</summary>
        public static Dictionary<String, String> GetTemplates()
        {
            var ss = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            if (ss == null || ss.Length <= 0) return null;

            var dic = new Dictionary<String, String>();

            //找到资源名
            foreach (var item in ss)
            {
                if (item.StartsWith("XCoder.App."))
                {
                    ReleaseFile(item, "XCoder.exe.config");
                }
                else if (item.StartsWith("XCoder.Template."))
                {
                    var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(item);
                    var tempName = item.Substring("XCoder.Template.".Length);
                    var buffer = new Byte[stream.Length];
                    var count = stream.Read(buffer, 0, buffer.Length);

                    var content = Encoding.UTF8.GetString(buffer, 0, count);
                    dic.Add(tempName, content);
                }
            }

            return dic.OrderBy(e => e.Key).ToDictionary(e => e.Key, e => e.Value);
        }

        /// <summary>读取资源，并写入到文件</summary>
        /// <param name="name"></param>
        /// <param name="fileName"></param>
        public static void ReleaseFile(String name, String fileName)
        {
            fileName = fileName.GetFullPath();
            if (String.IsNullOrEmpty(fileName) || File.Exists(fileName)) return;

            try
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
                if (stream == null) return;

                var buffer = new Byte[stream.Length];
                var count = stream.Read(buffer, 0, buffer.Length);

                fileName = fileName.GetFullPath();
                var p = Path.GetDirectoryName(fileName);
                if (!String.IsNullOrEmpty(p) && !Directory.Exists(p)) Directory.CreateDirectory(p);

                File.WriteAllBytes(fileName, buffer);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}