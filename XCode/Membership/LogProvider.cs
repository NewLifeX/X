using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Configuration;
using NewLife.Model;

namespace XCode.Membership
{
    /// <summary>日志提供者。提供业务日志输出到数据库的功能</summary>
    public abstract class LogProvider
    {
        #region 基本功能
        /// <summary>写日志</summary>
        /// <param name="type">类型</param>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public abstract void WriteLog(Type type, String action, String remark);

        /// <summary>输出实体对象日志</summary>
        /// <param name="action"></param>
        /// <param name="entity"></param>
        public void WriteLog(String action, IEntity entity)
        {
            var fact = EntityFactory.CreateOperate(entity.GetType());

            // 构造字段数据的字符串表示形式
            var sb = new StringBuilder();
            foreach (var fi in fact.Fields)
            {
                if (action == "修改" && !fi.PrimaryKey && !entity.Dirtys[fi.Name]) continue;
                var v = entity[fi.Name];
                // 空字符串不写日志
                if (action == "添加" || action == "删除")
                {
                    if (v + "" == "") continue;
                    if (v is Boolean && (Boolean)v == false) continue;
                    if (v is Int32 && (Int32)v == 0) continue;
                    if (v is DateTime && (DateTime)v == DateTime.MinValue) continue;
                }

                // 日志里面不要出现密码
                if (fi.Name.EqualIgnoreCase("pass", "password")) v = null;

                sb.Separate(",").AppendFormat("{0}={1}", fi.Name, v);
            }

            WriteLog(entity.GetType(), action, sb.ToString());
        }

        private Boolean _Enable = true;
        /// <summary>是否使用日志</summary>
        public Boolean Enable { get { return _Enable; } set { _Enable = value; } }
        #endregion

        #region 静态属性
        static LogProvider()
        {
            ObjectContainer.Current.AutoRegister<LogProvider, DefaultLogProvider>();
        }

        private static LogProvider _Provider;
        /// <summary>当前成员提供者</summary>
        public static LogProvider Provider
        {
            get
            {
                if (_Provider == null) _Provider = ObjectContainer.Current.Resolve<LogProvider>();
                return _Provider;
            }
        }
        #endregion
    }

    /// <summary>泛型日志提供者，使用泛型日志实体基类作为派生</summary>
    /// <typeparam name="TLog"></typeparam>
    public class LogProvider<TLog> : LogProvider where TLog : Log<TLog>, new()
    {
        /// <summary>写日志</summary>
        /// <param name="type">类型</param>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public override void WriteLog(Type type, String action, String remark)
        {
            if (!Enable) return;

            if (type == null) throw new ArgumentNullException("type");

            var factory = EntityFactory.CreateOperate(typeof(TLog));
            var log = (factory.Default as ILog).Create(type, action);

            log.Remark = remark;
            log.Save();
        }
    }

    /// <summary>默认日志提供者，使用实体类<seealso cref="Log"/></summary>
    class DefaultLogProvider : LogProvider<Log> { }
}