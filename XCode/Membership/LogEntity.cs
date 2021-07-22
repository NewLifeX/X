using System;

namespace XCode.Membership
{
    /// <summary>日志实体类基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    [Serializable]
    public class LogEntity<TEntity> : Entity<TEntity> where TEntity : LogEntity<TEntity>, new()
    {
        #region 改动时写日志
        /// <summary>添加时写日志</summary>
        /// <returns></returns>
        public override Int32 Insert()
        {
            var err = "";
            try
            {
                return base.Insert();
            }
            catch (Exception ex)
            {
                err = ex.Message;
                throw;
            }
            finally
            {
                LogProvider.Provider.WriteLog("添加", this, err);
            }
        }

        /// <summary>修改时写日志</summary>
        /// <returns></returns>
        public override Int32 Update()
        {
            // 必须提前写修改日志，否则修改后脏数据失效，保存的日志为空
            if (HasDirty) LogProvider.Provider.WriteLog("修改", this);

            try
            {
                return base.Update();
            }
            catch (Exception ex)
            {
                LogProvider.Provider.WriteLog("修改", this, ex.Message);
                throw;
            }
        }

        /// <summary>删除时写日志</summary>
        /// <returns></returns>
        public override Int32 Delete()
        {
            var err = "";
            try
            {
                return base.Delete();
            }
            catch (Exception ex)
            {
                err = ex.Message;
                throw;
            }
            finally
            {
                LogProvider.Provider.WriteLog("删除", this, err);
            }
        }
        #endregion

        #region 日志
        /// <summary>写日志</summary>
        /// <param name="action">操作</param>
        /// <param name="success">成功</param>
        /// <param name="remark">备注</param>
        public static void WriteLog(String action, Boolean success, String remark) => LogProvider.Provider.WriteLog(typeof(TEntity), action, success, remark);
        #endregion
    }
}