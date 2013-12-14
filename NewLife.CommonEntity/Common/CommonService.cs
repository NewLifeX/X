using System;
using NewLife.CommonEntity.Web;
using NewLife.Model;
using NewLife.Web;

namespace NewLife.CommonEntity
{
    class CommonService //: ServiceContainer<CommonService>
    {
        #region 当前静态服务容器
        /// <summary>当前对象容器</summary>
        public static IObjectContainer Container { get { return ObjectContainer.Current; } }
        #endregion

        static CommonService()
        {
            Container
                //.Register<IManageProvider, ManageProvider>()
                //.Register<ICommonManageProvider, CommonManageProvider>()
                .AutoRegister(typeof(IManageProvider), typeof(CommonManageProvider), typeof(ManageProvider))
                .AutoRegister(typeof(IErrorInfoProvider), typeof(CommonManageProvider), typeof(ManageProvider))
                .AutoRegister<IEntityForm, EntityForm2>()
                .AutoRegister<IManagePage, ManagePage>();
        }
    }
}