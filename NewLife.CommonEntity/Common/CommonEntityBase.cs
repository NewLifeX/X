using System;
using NewLife.Configuration;
using NewLife.Web;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 通用实体类基类
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    [Serializable]
    public class CommonEntityBase<TEntity> : Entity<TEntity> where TEntity : CommonEntityBase<TEntity>, new()
    {
        #region 日志
        /// <summary>
        /// Http状态，名称必须和管理员类中一致
        /// </summary>
        static HttpState<IAdministrator> http = new HttpState<IAdministrator>("Admin");
        internal static IAdministrator DefaultAdministrator;

        /// <summary>
        /// 创建指定动作的日志实体。通过Http状态访问当前管理员对象，创建日志实体
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IEntity CreateLog(String action)
        {
            IAdministrator admin = http.Current;
            if (admin == null) admin = DefaultAdministrator;
            if (admin == null) return null;

            return admin.CreateLog(typeof(TEntity), action);
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public static void WriteLog(String action, String remark)
        {
            if (!Config.GetConfig<Boolean>("NewLife.CommonEntity.WriteEntityLog", true)) return;

            IEntity log = CreateLog(action);
            if (log != null)
            {
                log.SetItem("Remark", remark);
                log.Save();
            }
        }
        #endregion
    }
}