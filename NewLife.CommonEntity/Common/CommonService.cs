using System;
using NewLife.CommonEntity.Web;
using NewLife.Model;
using NewLife.Web;

namespace NewLife.CommonEntity
{
    /// <summary>通用服务</summary>
    public class CommonService //: ServiceContainer<CommonService>
    {
        #region 当前静态服务容器
        /// <summary>当前对象容器</summary>
        public static IObjectContainer Container { get { return ObjectContainer.Current; } }
        #endregion

        static CommonService()
        {
            Container
                //.AutoRegister(typeof(IManageProvider), typeof(CommonManageProvider), typeof(ManageProvider))
                //.AutoRegister(typeof(IErrorInfoProvider), typeof(CommonManageProvider), typeof(ManageProvider))
                .AutoRegister<IEntityForm, EntityForm2>()
                .AutoRegister<IManagePage, ManagePage>();

            //Container
            //    .AutoRegister<IAdministrator, Administrator>()
            //    .AutoRegister<IRole, Role>()
            //    .AutoRegister<IMenu, Menu>()
            //    .AutoRegister<ILog, Log>();
        }
    }
}