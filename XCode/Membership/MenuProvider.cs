using System;
using NewLife;

namespace XCode.Membership
{
    /// <summary>菜单提供者。提供菜单相关操作的功能</summary>
    public class MenuProvider
    {
        #region 基本功能
        /// <summary>写日志</summary>
        /// <param name="type">类型</param>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public virtual void WriteLog(Type type, String action, String remark)
        {
            if (!Enable) return;

            if (type == null) throw new ArgumentNullException(nameof(type));

            var factory = EntityFactory.CreateOperate(typeof(Log));
            var log = factory.Create() as Log;
            log.Category = type.GetDisplayName() ?? type.GetDescription() ?? type.Name;
            log.Action = action;
            log.Remark = remark;
            log.Save();
        }

        /// <summary>是否使用日志</summary>
        public Boolean Enable { get; set; } = true;
        #endregion

        #region 静态属性
        /// <summary>当前成员提供者</summary>
        public static MenuProvider Provider { get; set; } = new MenuProvider();
        #endregion
    }
}