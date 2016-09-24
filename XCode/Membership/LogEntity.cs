using System;
using System.Text;

namespace XCode.Membership
{
    /// <summary>日志实体类基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    [Serializable]
    public class LogEntity<TEntity> : Entity<TEntity> where TEntity : LogEntity<TEntity>, new()
    {
        #region 改动时写日志
        /// <summary>调用Save时写日志</summary>
        /// <returns></returns>
        public override int Save()
        {
            // 更改日志保存顺序，先保存才能获取到id
            string action = "添加";
            var isNew = IsNullKey;
            if (!isNew)
            {
                // 没有修改时不写日志
                if (!HasDirty) return 0;

                action = "修改";

                // 必须提前写修改日志，否则修改后脏数据失效，保存的日志为空
                LogProvider.Provider.WriteLog(action, this);
            }

            int result = base.Save();

            if (isNew) LogProvider.Provider.WriteLog(action, this);

            return result;
        }

        /// <summary>添加时写日志</summary>
        /// <returns></returns>
        public override Int32 Insert()
        {
            var rs = base.Insert();

            LogProvider.Provider.WriteLog("添加", this);

            return rs;
        }

        /// <summary>修改时写日志</summary>
        /// <returns></returns>
        protected override Int32 OnUpdate()
        {
            if (HasDirty) LogProvider.Provider.WriteLog("修改", this);

            return base.OnUpdate();
        }

        /// <summary>删除时写日志</summary>
        /// <returns></returns>
        public override int Delete()
        {
            LogProvider.Provider.WriteLog("删除", this);

            return base.Delete();
        }
        #endregion

        #region 日志
        /// <summary>写日志</summary>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public static void WriteLog(String action, String remark)
        {
            LogProvider.Provider.WriteLog(typeof(TEntity), action, remark);
        }
        #endregion
    }
}