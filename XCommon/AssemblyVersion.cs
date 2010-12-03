using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace XCommon
{
    /// <summary>
    /// 程序集版本
    /// </summary>
    public class AssemblyVersion
    {
        private Assembly asm;

        /// <summary>
        /// 使用程序集初始化一个程序集版本实例
        /// </summary>
        /// <param name="a"></param>
        public AssemblyVersion(Assembly a)
        {
            if (a == null)
                asm = Assembly.GetExecutingAssembly();
            else
                asm = a;
        }

        private String _Name;
        /// <summary>名称</summary>
        public String Name
        {
            get
            {
                if (String.IsNullOrEmpty(_Name))
                {
                    _Name = asm.GetName().Name;
                    if (String.IsNullOrEmpty(_Name)) _Name = "未知";
                }
                return _Name;
            }
            set { _Name = value; }
        }

        private String _Version;
        /// <summary>
        /// 程序集版本
        /// </summary>
        public String Version
        {
            get
            {
                if (String.IsNullOrEmpty(_Version))
                {
                    //Assembly asm = Assembly.GetExecutingAssembly();
                    _Version = asm.GetName().Version.ToString();
                    if (String.IsNullOrEmpty(_Version)) _Version = "1.0";
                }
                return _Version;
            }
        }

        private String _Title;
        /// <summary>程序集标题</summary>
        public String Title
        {
            get
            {
                if (String.IsNullOrEmpty(_Title))
                {
                    AssemblyTitleAttribute av = Attribute.GetCustomAttribute(asm, typeof(AssemblyTitleAttribute)) as AssemblyTitleAttribute;
                    if (av != null) _Title = av.Title;
                    if (String.IsNullOrEmpty(_Title)) _Title = "未命名";
                }
                return _Title;
            }
            set { _Title = value; }
        }

        private String _FileVersion;
        /// <summary>
        /// 文件版本
        /// </summary>
        public String FileVersion
        {
            get
            {
                if (String.IsNullOrEmpty(_FileVersion))
                {
                    //Assembly asm = Assembly.GetExecutingAssembly();
                    AssemblyFileVersionAttribute av = Attribute.GetCustomAttribute(asm, typeof(AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute;
                    if (av != null) _FileVersion = av.Version;
                    if (String.IsNullOrEmpty(_FileVersion)) _FileVersion = "1.0";
                }
                return _FileVersion;
            }
        }

        private DateTime _Compile;
        /// <summary>编译时间</summary>
        public DateTime Compile
        {
            get
            {
                if (_Compile <= DateTime.MinValue)
                {
                    String[] ss = Version.Split(new Char[] { '.' });
                    Int32 d = Convert.ToInt32(ss[2]);
                    Int32 s = Convert.ToInt32(ss[3]);

                    DateTime dt = new DateTime(2000, 1, 1);
                    dt = dt.AddDays(d).AddSeconds(s * 2);

                    _Compile = dt;
                }
                return _Compile;
            }
        }

        private String _Company;
        /// <summary>公司名称</summary>
        public String Company
        {
            get
            {
                if (String.IsNullOrEmpty(_Company))
                {
                    AssemblyCompanyAttribute av = Attribute.GetCustomAttribute(asm, typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
                    if (av != null) _Company = av.Company;
                    if (String.IsNullOrEmpty(_Company)) _Company = "未知";
                }
                return _Company;
            }
            set { _Company = value; }
        }

        private String _Description;
        /// <summary>说明</summary>
        public String Description
        {
            get
            {
                if (String.IsNullOrEmpty(_Description))
                {
                    AssemblyDescriptionAttribute av = Attribute.GetCustomAttribute(asm, typeof(AssemblyDescriptionAttribute)) as AssemblyDescriptionAttribute;
                    if (av != null) _Description = av.Description;
                    if (String.IsNullOrEmpty(_Description)) _Description = "未知";
                }
                return _Company;
            }
            set { _Description = value; }
        }

        /// <summary>
        /// 获取程序集
        /// </summary>
        /// <returns></returns>
        public static List<Assembly> GetAssemblys()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;

            Assembly[] assems = currentDomain.GetAssemblies();
            if (assems != null && assems.Length > 0)
                return new List<Assembly>(assems);
            return null;
        }
    }
}