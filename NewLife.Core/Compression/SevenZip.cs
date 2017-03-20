using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NewLife.Compression
{
    /// <summary>7Zip</summary>
    public class SevenZip
    {
        #region winrar压缩       
        #endregion

        #region 7z压缩        
        /// <summary>压缩文件</summary>
        /// <param name="fileName"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        public static Boolean Compress(List<String> fileName, String dest)
        {
            for (Int32 i = 0; i < fileName.Count; i++)
            {
                var args = " a -r \"" + dest + "\" \"" + fileName[i].ToString().Trim() + "\"";
                if (!Process(args)) return false;
            }
            return true;
        }

        /// <summary>解压缩文件</summary>
        /// <param name="file"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        public static Boolean Extract(String file, String dest)
        {
            var arguments = " x -y \"" + file + "\" -o\"" + dest + "\"";
            return Process(arguments);
        }

        private static Boolean Process(String args)
        {
            var p = new Process();
            p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized; // 隐藏窗口            
            p.StartInfo.FileName = "7z.exe".GetFullPath();
            p.StartInfo.CreateNoWindow = false;
            p.StartInfo.Arguments = args;
            p.Start();
            p.WaitForExit();

            Int32 rs = 0;
            if (p.HasExited)
            {
                rs = p.ExitCode;
                p.Close();
                if (rs != 0 && rs != 1) return false;
            }
            return true;
        }
        #endregion
    }
}