using System;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>通用实体类基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    [Serializable]
    public class CommonEntityBase<TEntity> : Entity<TEntity> where TEntity : CommonEntityBase<TEntity>, new()
    {
        #region 日志
        /// <summary>写日志</summary>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public static void WriteLog(String action, String remark)
        {
            ManageProvider.Provider.WriteLog(typeof(TEntity), action, remark);
        }
        #endregion
    }
}