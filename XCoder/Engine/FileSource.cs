using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;

namespace XCoder
{
    /// <summary>
    /// 文件资源
    /// </summary>
    public static class FileSource
    {
        ///// <summary>
        ///// 开始检查模版，模版文件夹不存在时，释放模版
        ///// </summary>
        //public static void CheckTemplate()
        //{
        //    ThreadPool.QueueUserWorkItem(delegate(Object state)
        //    {
        //        try
        //        {
        //            String path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Engine.TemplatePath);
        //            if (!Directory.Exists(path))
        //                ReleaseTemplateFiles();
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show(ex.ToString());
        //        }
        //    });
        //}

        /// <summary>
        /// 释放模版文件
        /// </summary>
        public static Dictionary<String, String> ReleaseTemplateFiles()
        {
            String[] ss = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            if (ss == null || ss.Length <= 0) return null;

            Dictionary<String, String> dic = new Dictionary<string, string>();

            //找到资源名
            foreach (String item in ss)
            {
                if (item.StartsWith("XCoder.App."))
                {
                    ReleaseFile(item, "XCoder.exe.config");
                }
                else if (item.StartsWith("XCoder.Template."))
                {
                    Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(item);
                    String tempName = item.Substring("XCoder.Template.".Length);
                    Byte[] buffer = new Byte[stream.Length];
                    Int32 count = stream.Read(buffer, 0, buffer.Length);

                    String content = Encoding.UTF8.GetString(buffer, 0, count);
                    dic.Add(tempName, content);
                }
            }

            return dic;
        }

        /// <summary>
        /// 读取资源，并写入到文件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fileName"></param>
        public static void ReleaseFile(String name, String fileName)
        {
            if (String.IsNullOrEmpty(fileName) || File.Exists(fileName)) return;

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
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}