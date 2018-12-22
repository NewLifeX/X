using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Model;

namespace XCode.Membership
{
    /// <summary>菜单提供者。提供菜单相关操作的功能</summary>
    public abstract class MenuProvider
    {
        #region 基本功能
        /// <summary>写日志</summary>
        /// <param name="type">类型</param>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public abstract void WriteLog(Type type, String action, String remark);

        /// <summary>是否使用日志</summary>
        public Boolean Enable { get; set; } = true;
        #endregion

        #region 静态属性
        static MenuProvider()
        {
            ObjectContainer.Current.AutoRegister<MenuProvider, DefaultMenuProvider>();
        }

        private static MenuProvider _Provider;
        /// <summary>当前成员提供者</summary>
        public static MenuProvider Provider
        {
            get
            {
                if (_Provider == null) _Provider = ObjectContainer.Current.Resolve<MenuProvider>();
                return _Provider;
            }
        }
        #endregion
    }

    /// <summary>泛型菜单提供者，使用泛型菜单实体基类作为派生</summary>
    /// <typeparam name="TMenu"></typeparam>
    public class MenuProvider<TMenu> : MenuProvider where TMenu : Menu<TMenu>, new()
    {
        /// <summary>写日志</summary>
        /// <param name="type">类型</param>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public override void WriteLog(Type type, String action, String remark)
        {
            if (!Enable) return;

            if (type == null) throw new ArgumentNullException("type");

            var factory = EntityFactory.CreateOperate(typeof(TMenu));
            var log = factory.Create() as ILog;
            log.Category = type.GetDisplayName() ?? type.GetDescription() ?? type.Name;
            log.Action = action;
            log.Remark = remark;
            log.Save();
        }
    }

    /// <summary>默认菜单提供者，使用实体类<seealso cref="Menu"/></summary>
    class DefaultMenuProvider : MenuProvider<Menu> { }
}