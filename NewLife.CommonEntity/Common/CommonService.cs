using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Model;

namespace NewLife.CommonEntity
{
    class CommonService : ServiceContainer<CommonService>
    {
        static CommonService()
        {
            Container
                .Register<IManageProvider, ManageProvider>(null, false)
                .Register<ICommonManageProvider, CommonManageProvider>(null, false)
                .Register<IAdministrator, Administrator>(null, false)
                .Register<ILog, Log>(null, false)
                .Register<IRole, Role>(null, false)
                .Register<IMenu, Menu>(null, false)
                .Register<IRoleMenu, RoleMenu>(null, false);
        }
    }
}