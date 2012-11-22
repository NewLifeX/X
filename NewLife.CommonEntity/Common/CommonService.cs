using NewLife.CommonEntity.Web;
using NewLife.Model;
using NewLife.Web;

namespace NewLife.CommonEntity
{
    class CommonService : ServiceContainer<CommonService>
    {
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