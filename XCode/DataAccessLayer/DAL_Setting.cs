using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;
using XCode.Model;

namespace XCode.DataAccessLayer
{
    partial class DAL
    {
        #region Sql日志输出
        /// <summary>是否调试</summary>
        public static Boolean Debug { get; set; } = Setting.Current.Debug;

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args)
        {
            if (!Debug) return;

            InitLog();
            XTrace.WriteLine(format, args);
        }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [Conditional("DEBUG")]
        public static void WriteDebugLog(String format, params Object[] args)
        {
            if (!Debug) return;

            InitLog();
            XTrace.WriteLine(format, args);
        }

        static Int32 hasInitLog = 0;
        private static void InitLog()
        {
            if (Interlocked.CompareExchange(ref hasInitLog, 1, 0) > 0) return;

            // 输出当前版本
            System.Reflection.Assembly.GetExecutingAssembly().WriteVersion();

            var set = Setting.Current.Negative;
            if (DAL.Debug && set.Enable)
            {
                if (set.CheckOnly) WriteLog("XCode.Negative.CheckOnly设置为True，只是检查不对数据库进行操作");
                if (set.NoDelete) WriteLog("XCode.Negative.NoDelete设置为True，不会删除数据表多余字段");
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
        private static ICollection<String> _NegativeExclude;
        /// <summary>要排除的链接名</summary>
        public static ICollection<String> NegativeExclude
        {
            get
            {
                if (_NegativeExclude != null) return _NegativeExclude;

                //String str = Config.GetMutilConfig<String>(null, "XCode.Negative.Exclude", "XCode.Schema.Exclude", "DatabaseSchema_Exclude");
                var str = Setting.Current.Negative.Exclude + "";

                _NegativeExclude = new HashSet<String>(str.Split(), StringComparer.OrdinalIgnoreCase);

                return _NegativeExclude;
            }
        }
        #endregion
    }
}