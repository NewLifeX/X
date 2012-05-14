using NewLife.CommonEntity.Web;
using NewLife.Model;

namespace NewLife.CommonEntity
{
    class CommonService : ServiceContainer<CommonService>
    {
        static CommonService()
        {
            Container
                //.Register<IManageProvider, ManageProvider>()
                //.Register<ICommonManageProvider, CommonManageProvider>()
                .Register<IManageProvider, CommonManageProvider>()
                .Register<IEntityForm, EntityForm2>()
                .Register<IManagePage, ManagePage>();
        }
    }
}