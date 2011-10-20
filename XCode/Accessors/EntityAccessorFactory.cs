using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Model;
using XCode.Model;

namespace XCode.Accessors
{
    public class EntityAccessorFactory
    {
        internal static void Reg(IObjectContainer container)
        {
            // 注册内置访问器
            container
                .Register<IEntityAccessor, HttpEntityAccessor>(EntityAccessorKinds.Http.ToString(), false)
                .Register<IEntityAccessor, WebFormEntityAccessor>(EntityAccessorKinds.WebForm.ToString(), false)
                .Register<IEntityAccessor, WinFormEntityAccessor>(EntityAccessorKinds.WinForm.ToString(), false);
        }

        public static IEntityAccessor Create(String name, params Object[] ps)
        {
            return XCodeService.Resolve<IEntityAccessor>(name);
        }

        public static IEntityAccessor Create(EntityAccessorKinds kind, params Object[] ps)
        {
            return Create(kind.ToString());
        }
    }
}