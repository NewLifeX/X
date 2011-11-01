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
                .Register<IManageProvider, ManageProvider>(null, false)
                .Register<ICommonManageProvider, CommonManageProvider>(null, false)
                .Register<IEntityForm, EntityForm2>(null, false)
                .Register<IManagerPage, ManagerPage>(null, false);
        }
    }
}