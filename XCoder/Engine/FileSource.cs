using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace XCoder
{
    /// <summary>
    /// 文件资源
    /// </summary>
    public static class FileSource
    {
        /// <summary>
        /// 开始检查模版，模版文件夹不存在时，释放模版
        /// </summary>
        public static void CheckTemplate()
        {
            ThreadPool.QueueUserWorkItem(delegate(Object state)
            {
                try
                {
                    String path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, XCoder.TemplatePath);
                    if (!Directory.Exists(path))
                        ReleaseTemplateFiles();
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
        public static void ReleaseTemplateFiles()
        {
            String path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, XCoder.TemplatePath);
            //if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            String[] ss = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            if (ss == null || ss.Length <= 0) return;

            //找到资源名
            foreach (String item in ss)
            {
                if (item.StartsWith("XCoder.App."))
                {
                    ReleaseFile(item, "XCoder.exe.config");
                }
                else if (item.StartsWith("XCoder.Template."))
                {
                    String tempName = item.Substring("XCoder.Template.".Length);
                    String fileName = tempName.Substring(tempName.IndexOf(".") + 1);
                    tempName = tempName.Substring(0, tempName.IndexOf("."));

                    fileName = Path.Combine(tempName, fileName);
                    fileName = Path.Combine(path, fileName);

                    ReleaseFile(item, fileName);
                }
            }
        }

        /// <summary>
        /// 读取资源，并写入到文件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fileName"></param>
        static void ReleaseFile(String name, String fileName)
        {
            try
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
                if (stream == null) return;

                Byte[] buffer = new Byte[stream.Length];
                Int32 count = stream.Read(buffer, 0, buffer.Length);

                String p = Path.GetDirectoryName(fileName);
                if (!String.IsNullOrEmpty(p) && !Directory.Exists(p)) Directory.CreateDirectory(p);

                File.WriteAllBytes(fileName, buffer);
            }
            catch { }
        }
    }
}