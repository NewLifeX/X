using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Model;
using NewLife.CommonEntity.Web;

namespace NewLife.CommonEntity
{
    class CommonService : ServiceContainer<CommonService>
    {
        static CommonService()
        {
            Container
                .Register<IManageProvider, ManageProvider>()
                .Register<ICommonManageProvider, CommonManageProvider>()
                .Register<IEntityForm, EntityForm2>()
                .Register<IManagerPage, ManagerPage>();
        }
    }
}