using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Security.Cryptography;
using System.IO;
using System.Reflection;
using NewLife.Log;
using NewLife.Configuration;

namespace XTemplate.Templating
{
    partial class Template
    {
        #region 辅助函数
        /// <summary>
        /// MD5散列
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        protected static String Hash(String str)
        {
            if (String.IsNullOrEmpty(str)) return null;

            MD5 md5 = MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(str))).Replace("-", null);
        }

        /// <summary>
        /// 把名称处理为标准类名
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static String GetClassName(String fileName)
        {
            String name = fileName;
            //if (name.Contains(".")) name = name.Substring(0, name.LastIndexOf("."));
            name = name.Replace(@"\", "_").Replace(@"/", "_").Replace(".", "_");
            name = name.Replace(Path.VolumeSeparatorChar, '_');
            name = name.Replace(Path.PathSeparator, '_');
            return name;
        }
        #endregion

        #region 调试
        private static Boolean? _Debug;
        /// <summary>
        /// 是否调试
        /// </summary>
        public static Boolean Debug
        {
            get
            {
                if (_Debug != null) return _Debug.Value;

                _Debug = Config.GetConfig<Boolean>("XTemplate.Debug", false);

                return _Debug.Value;
            }
            set { _Debug = value; }
        }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="msg"></param>
        public static void WriteLog(String msg)
        {
            XTrace.WriteLine(msg);
        }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args)
        {
            XTrace.WriteLine(format, args);
        }
        #endregion

        #region 配置
        private static String _BaseClassName;
        /// <summary>
        /// 默认基类名称
        /// </summary>
        public static String BaseClassName
        {
            get { return _BaseClassName ?? (_BaseClassName = Config.GetConfig<String>("XTemplate.BaseClassName", String.Empty)); }
            set { _BaseClassName = value; }
        }

        private static List<String> _References;
        /// <summary>
        /// 标准程序集引用
        /// </summary>
        public static List<String> References
        {
            get
            {
                if (_References != null) return _References;

                // 程序集路径
                List<String> list = new List<String>();
                // 程序集名称，小写，用于重复判断
                List<String> names = new List<String>();

                // 加入配置的程序集
                String[] ss = Config.GetConfigSplit<String>("XTemplate.References", null);
                if (ss != null && ss.Length > 0)
                {
                    foreach (String item in ss)
                    {
                        list.Add(item);

                        names.Add(item.ToLower());
                    }
                }

                // 当前应用程序域所有程序集，虽然加上了很多引用，但是编译时会自动忽略没有实际引用的程序集！
                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                if (asms == null || asms.Length < 1) return null;
                foreach (Assembly item in asms)
                {
                    try
                    {
                        if (String.IsNullOrEmpty(item.Location)) continue;

                        String name = Path.GetFileName(item.Location);
                        if (!String.IsNullOrEmpty(name)) name = name.ToLower();
                        if (names.Contains(name)) continue;
                        names.Add(name);
                        list.Add(item.Location);
                    }
                    catch { }
                }

                return _References = list;
            }
        }

        private static List<String> _Imports;
        /// <summary>
        /// 标准命名空间引用
        /// </summary>
        public static List<String> Imports
        {
            get
            {
                if (_Imports != null) return _Imports;

                // 命名空间
                List<String> list = new List<String>();

                // 加入配置的命名空间
                String[] ss = Config.GetConfigSplit<String>("XTemplate.Imports", null);
                if (ss != null && ss.Length > 0) list.AddRange(ss);

                String[] names = new String[] { "System", "System.Collections", "System.Collections.Generic", "System.Text" };
                if (names != null && names.Length > 0)
                {
                    foreach (String item in names)
                    {
                        if (!list.Contains(item)) list.Add(item);
                    }
                }

                // 特别支持
                Dictionary<String, String[]> supports = new Dictionary<String, String[]>();
                supports.Add("XCode", new String[] { "XCode", "XCode.DataAccessLayer" });
                supports.Add("XCommon", new String[] { "XCommon" });
                supports.Add("XControl", new String[] { "XControl" });
                supports.Add("NewLife.CommonEntity", new String[] { "NewLife.CommonEntity" });
                supports.Add("System.Web", new String[] { "System.Web" });
                supports.Add("System.Xml", new String[] { "System.Xml" });

                foreach (String item in supports.Keys)
                {
                    Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (Assembly asm in asms)
                    {
                        if (!asm.FullName.StartsWith(item + ",")) continue;

                        names = supports[item];
                        if (names != null && names.Length > 0)
                        {
                            foreach (String name in names)
                            {
                                if (!list.Contains(name)) list.Add(name);
                            }
                        }
                    }
                }

                return _Imports = list;
            }
        }
        #endregion
    }
}