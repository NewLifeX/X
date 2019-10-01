﻿using System;
using System.Diagnostics;
using System.Threading;
using NewLife.Log;
using XCode.Model;

namespace XCode.DataAccessLayer
{
    partial class DAL
    {
        static DAL() => InitLog();

        #region Sql日志输出
        /// <summary>是否调试</summary>
        public static Boolean Debug { get; set; } = Setting.Current.Debug;

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args)
        {
            if (!Debug) return;

            //InitLog();
            XTrace.WriteLine(format, args);
        }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [Conditional("DEBUG")]
        public static void WriteDebugLog(String format, params Object[] args)
        {
            if (!Debug) return;

            //InitLog();
            XTrace.WriteLine(format, args);
        }

        static Int32 hasInitLog = 0;
        internal static void InitLog()
        {
            if (Interlocked.CompareExchange(ref hasInitLog, 1, 0) > 0) return;

            // 输出当前版本
            System.Reflection.Assembly.GetExecutingAssembly().WriteVersion();
        }
        #endregion

        #region SQL拦截器
        private static ThreadLocal<Action<String>> _filter = new ThreadLocal<Action<String>>();
        /// <summary>本地过滤器（本线程SQL拦截）</summary>
        public static Action<String> LocalFilter { get => _filter.Value; set => _filter.Value = value; }
        #endregion

        #region 辅助函数
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => Db.ToString();

        /// <summary>建立数据表对象</summary>
        /// <returns></returns>
        internal static IDataTable CreateTable() => XCodeService.CreateTable();

        /// <summary>是否支持批操作</summary>
        /// <returns></returns>
        public Boolean SupportBatch
        {
            get
            {
                if (DbType == DatabaseType.MySql || DbType == DatabaseType.Oracle || DbType == DatabaseType.SQLite) return true;

#if !__CORE__
                // SqlServer对批处理有BUG，将在3.0中修复
                // https://github.com/dotnet/corefx/issues/29391
                if (DbType == DatabaseType.SqlServer) return true;
#endif

                return false;
            }
        }
        #endregion
    }
}