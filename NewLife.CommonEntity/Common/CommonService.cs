using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Model;

namespace NewLife.CommonEntity
{
    class CommonService : ServiceContainer
    {
        static CommonService()
        {
            Container
                .Register<IAdministrator, Administrator>(null, false);
        }
    }
}