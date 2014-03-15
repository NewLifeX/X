using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NewLife.Collections;
using NewLife.Configuration;
using NewLife.Log;

namespace XTemplate.Templating
{
    partial class Template
    {
        #region 辅助函数
        /// <summary>MD5散列</summary>
        /// <param name="str"></param>
        /// <returns></returns>
        protected static String Hash(String str)
        {
            if (String.IsNullOrEmpty(str)) return null;

            MD5 md5 = MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(str))).Replace("-", null);
        }

        static String _Filter = @"\/.- $";
        /// <summary>把名称处理为标准类名</summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static String GetClassName(String fileName)
        {
            String name = fileName;
            //if (name.Contains(".")) name = name.Substring(0, name.LastIndexOf("."));
            //name = name.Replace(@"\", "_").Replace(@"/", "_").Replace(".", "_").Replace("-", "_");
            foreach (var item in _Filter)
            {
                name = name.Replace(item, '_');
            }
            name = name.Replace(Path.VolumeSeparatorChar, '_');
            name = name.Replace(Path.PathSeparator, '_');
            return name;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} [{1}]", AssemblyName ?? NameSpace, Templates.Count);
            //return base.ToString();
        }
        #endregion

        #region 调试
        private static Boolean? _Debug;
        /// <summary>是否调试</summary>
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

        /// <summary>输出日志</summary>
        /// <param name="msg"></param>
        public static void WriteLog(String msg)
        {
            XTrace.WriteLine(msg);
        }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args)
        {
            XTrace.WriteLine(format, args);
        }
        #endregion

        #region 配置
        private static String _BaseClassName;
        /// <summary>默认基类名称</summary>
        public static String BaseClassName
        {
            get { return _BaseClassName ?? (_BaseClassName = Config.GetConfig<String>("XTemplate.BaseClassName", String.Empty)); }
            set { _BaseClassName = value; }
        }

        private static List<String> _References;
        /// <summary>标准程序集引用</summary>
        public static List<String> References
        {
            get
            {
                if (_References != null) return _References;

                // 程序集路径
                var list = new List<String>();
                // 程序集名称，小写，用于重复判断
                var names = new List<String>();

                // 加入配置的程序集
                var ss = Config.GetConfigSplit<String>("XTemplate.References", null);
                if (ss != null && ss.Length > 0)
                {
                    foreach (var item in ss)
                    {
                        list.Add(item);

                        names.Add(item.ToLower());
                    }
                }

                // 当前应用程序域所有程序集，虽然加上了很多引用，但是编译时会自动忽略没有实际引用的程序集！
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                if (asms == null || asms.Length < 1) return null;
                foreach (var item in asms)
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
        /// <summary>标准命名空间引用</summary>
        public static List<String> Imports
        {
            get
            {
                if (_Imports != null) return _Imports;

                // 命名空间
                var list = new List<String>();
                // 尽快赋值，避免重入
                _Imports = list;

                // 加入配置的命名空间
                var ss = Config.GetConfigSplit<String>("XTemplate.Imports", null);
                if (ss != null && ss.Length > 0) list.AddRange(ss);

                // 常用命名空间
                var names = new String[] { "System", "System.Collections", "System.Collections.Generic", "System.Text" };
                if (names != null && names.Length > 0)
                {
                    foreach (var item in names)
                    {
                        if (!list.Contains(item)) list.Add(item);
                    }
                }

                // 特别支持
                var supports = new Dictionary<String, String[]>();
                supports.Add("System.Web", new String[] { "System.Web" });
                supports.Add("System.Xml", new String[] { "System.Xml" });

                var asms = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var item in supports)
                {
                    foreach (var asm in asms)
                    {
                        if (!asm.FullName.StartsWith(item.Key + ",")) continue;

                        names = item.Value;
                        if (names != null && names.Length > 0)
                        {
                            foreach (var name in names)
                            {
                                if (!list.Contains(name)) list.Add(name);
                            }
                        }
                    }
                }

                // 特别支持，导入它们的所有命名空间
                var maps = new HashSet<String>();
                maps.Add("XCode");
                maps.Add("NewLife.Core");
                maps.Add("NewLife.CommonEntity");

                foreach (var item in maps)
                {
                    foreach (var asm in asms)
                    {
                        if (!asm.FullName.StartsWith(item + ",")) continue;

                        // 遍历所有公开类，导入它们的所有命名空间
                        foreach (var type in asm.GetTypes())
                        {
                            String name = type.Namespace;
                            if (String.IsNullOrEmpty(name)) continue;
                            //if (!name.StartsWith("XCode") && !name.StartsWith("NewLife")) continue;
                            if (!list.Contains(name)) list.Add(name);
                        }
                    }
                }

                return _Imports = list;
            }
        }
        #endregion
    }
}