using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NewLife.Threading;

namespace XCoder.XRegex
{
    /// <summary>文件资源</summary>
    public static class FileResource
    {
        /// <summary>
        /// 开始检查模版，模版文件夹不存在时，释放模版
        /// </summary>
        public static void CheckTemplate()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    ReleaseTemplateFiles("Pattern");
                    ReleaseTemplateFiles("Sample");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            });
        }

        /// <summary>
        /// 释放模版文件
        /// </summary>
        /// <param name="name">名称</param>
        public static void ReleaseTemplateFiles(String name)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
            if (Directory.Exists(path)) return;

            var ss = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            if (ss == null || ss.Length <= 0) return;

            // 取命名空间
            var prefix = typeof(FileResource).FullName;
            prefix = prefix.Substring(0, prefix.LastIndexOf("."));
            prefix += "." + name + ".";

            //找到资源名
            foreach (var item in ss)
            {
                if (item.StartsWith(prefix))
                {
                    var fileName = item.Substring(prefix.Length);
                    fileName = fileName.Replace(".", @"\");
                    // 最后一个斜杠变回圆点
                    var cc = fileName.ToCharArray();
                    cc[fileName.LastIndexOf("\\")] = '.';
                    fileName = new String(cc);
                    fileName = Path.Combine(path, fileName);

                    ReleaseFile(item, fileName);
                }
            }
        }

        /// <summary>
        /// 读取资源，并写入到文件
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="fileName"></param>
        static void ReleaseFile(String name, String fileName)
        {
            try
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
                if (stream == null) return;

                var buffer = new Byte[stream.Length];
                var count = stream.Read(buffer, 0, buffer.Length);

                var p = Path.GetDirectoryName(fileName);
                if (!String.IsNullOrEmpty(p) && !Directory.Exists(p)) Directory.CreateDirectory(p);

                File.WriteAllBytes(fileName, buffer);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        //public static Icon GetIcon()
        //{
        //    return new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(FileResource), "leaf.ico"));
        //}
    }
}