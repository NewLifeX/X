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
                .Register<ICommonManageProvider, CommonManageProvider>(null, false);
        }
    }
}