using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NewLife.Collections;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;
using XCode.Model;

namespace XCode.DataAccessLayer
{
    partial class DAL
    {
        #region Sql日志输出
        private static Boolean? _Debug;
        /// <summary>是否调试</summary>
        public static Boolean Debug
        {
            get
            {
                if (_Debug != null) return _Debug.Value;

                _Debug = Config.GetMutilConfig<Boolean>(false, "XCode.Debug", "OrmDebug");

                return _Debug.Value;
            }
            set { _Debug = value; }
        }

        private static Boolean? _ShowSQL;
        /// <summary>是否输出SQL语句，默认为XCode调试开关XCode.Debug</summary>
        public static Boolean ShowSQL
        {
            get
            {
                if (_ShowSQL != null) return _ShowSQL.Value;

                _ShowSQL = Config.GetConfig<Boolean>("XCode.ShowSQL", DAL.Debug);

                return _ShowSQL.Value;
            }
            set { _ShowSQL = value; }
        }

        private static String _SQLPath;
        /// <summary>设置SQL输出的单独目录，默认为空，SQL输出到当前日志中</summary>
        public static String SQLPath
        {
            get
            {
                if (_SQLPath != null) return _SQLPath;

                _SQLPath = Config.GetConfig<String>("XCode.SQLPath", String.Empty);

                return _SQLPath;
            }
            set { _SQLPath = value; }
        }

        /// <summary>输出日志</summary>
        /// <param name="msg"></param>
        public static void WriteLog(String msg)
        {
            InitLog();
            XTrace.WriteLine(msg);
        }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args)
        {
            InitLog();
            XTrace.WriteLine(format, args);
        }

        /// <summary>输出日志</summary>
        /// <param name="msg"></param>
        [Conditional("DEBUG")]
        public static void WriteDebugLog(String msg)
        {
            InitLog();
            XTrace.WriteLine(msg);
        }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [Conditional("DEBUG")]
        public static void WriteDebugLog(String format, params Object[] args)
        {
            InitLog();
            XTrace.WriteLine(format, args);
        }

        static Int32 hasInitLog = 0;
        private static void InitLog()
        {
            if (Interlocked.CompareExchange(ref hasInitLog, 1, 0) > 0) return;

            // 输出当前版本
            var asm = AssemblyX.Create(System.Reflection.Assembly.GetExecutingAssembly());
            XTrace.WriteLine("NewLife.{0} v{1} Build {2:yyyy-MM-dd HH:mm:ss}", asm.Name, asm.FileVersion, asm.Compile);

            if (DAL.Debug && DAL.NegativeEnable)
            {
                if (DAL.NegativeCheckOnly) WriteLog("XCode.Negative.CheckOnly设置为True，只是检查不对数据库进行操作");
                if (DAL.NegativeNoDelete) WriteLog("XCode.Negative.NoDelete设置为True，不会删除数据表多余字段");
            }
        }
        #endregion

        #region 辅助函数
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return Db.ToString(); }

        /// <summary>建立数据表对象</summary>
        /// <returns></returns>
        internal static IDataTable CreateTable() { return XCodeService.CreateTable(); }
        #endregion

        #region 设置
        private static Boolean? _NegativeEnable;
        /// <summary>是否启用数据架构</summary>
        public static Boolean NegativeEnable
        {
            get
            {
                if (_NegativeEnable.HasValue) return _NegativeEnable.Value;

                _NegativeEnable = Config.GetMutilConfig<Boolean>(false, "XCode.Negative.Enable", "XCode.Schema.Enable", "DatabaseSchema_Enable");

                return _NegativeEnable.Value;
            }
            set { _NegativeEnable = value; }
        }

        private static Boolean? _NegativeCheckOnly;
        /// <summary>是否只检查不操作，默认不启用</summary>
        public static Boolean NegativeCheckOnly
        {
            get
            {
                if (_NegativeCheckOnly.HasValue) return _NegativeCheckOnly.Value;

                _NegativeCheckOnly = Config.GetConfig<Boolean>("XCode.Negative.CheckOnly");

                return _NegativeCheckOnly.Value;
            }
            set { _NegativeCheckOnly = value; }
        }

        private static Boolean? _NegativeNoDelete;
        /// <summary>是否启用不删除字段</summary>
        public static Boolean NegativeNoDelete
        {
            get
            {
                if (_NegativeNoDelete.HasValue) return _NegativeNoDelete.Value;

                _NegativeNoDelete = Config.GetMutilConfig<Boolean>(false, "XCode.Negative.NoDelete", "XCode.Schema.NoDelete", "DatabaseSchema_NoDelete");

                return _NegativeNoDelete.Value;
            }
            set { _NegativeNoDelete = value; }
        }

        private static ICollection<String> _NegativeExclude;
        /// <summary>要排除的链接名</summary>
        public static ICollection<String> NegativeExclude
        {
            get
            {
                if (_NegativeExclude != null) return _NegativeExclude;

                String str = Config.GetMutilConfig<String>(null, "XCode.Negative.Exclude", "XCode.Schema.Exclude", "DatabaseSchema_Exclude");

                if (String.IsNullOrEmpty(str))
                    _NegativeExclude = new HashSet<String>();
                else
                {
                    _NegativeExclude = new HashSet<String>(str.Split(new Char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
                }

                return _NegativeExclude;
            }
        }

        private static Int32? _TraceSQLTime;
        /// <summary>跟踪SQL执行时间，大于该阀值将输出日志，默认0毫秒不跟踪。</summary>
        public static Int32 TraceSQLTime
        {
            get
            {
                if (_TraceSQLTime != null) return _TraceSQLTime.Value;

                _TraceSQLTime = Config.GetConfig<Int32>("XCode.TraceSQLTime", 0);

                return _TraceSQLTime.Value;
            }
            set { _TraceSQLTime = value; }
        }
        #endregion
    }
}