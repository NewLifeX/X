using System;
using NewLife.Model;
using XCode.Model;

namespace XCode.Accessors
{
    /// <summary>实体访问器工厂</summary>
    public static class EntityAccessorFactory
    {
        internal static void Reg(IObjectContainer container)
        {
            // 注册内置访问器
            container
                .Register<IEntityAccessor, HttpEntityAccessor>(EntityAccessorTypes.Http)
                .Register<IEntityAccessor, WebFormEntityAccessor>(EntityAccessorTypes.WebForm)
                .Register<IEntityAccessor, WinFormEntityAccessor>(EntityAccessorTypes.WinForm)
                .Register<IEntityAccessor, BinaryEntityAccessor>(EntityAccessorTypes.Binary)
                .Register<IEntityAccessor, XmlEntityAccessor>(EntityAccessorTypes.Xml)
                .Register<IEntityAccessor, JsonEntityAccessor>(EntityAccessorTypes.Json);
        }

        /// <summary>创建指定类型的实体访问器</summary>>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IEntityAccessor Create(String name)
        {
            return XCodeService.Resolve<IEntityAccessor>(Enum.Parse(typeof(EntityAccessorTypes), name));
        }

        /// <summary>创建指定类型的实体访问器</summary>>
        /// <param name="kind"></param>
        /// <returns></returns>
        public static IEntityAccessor Create(EntityAccessorTypes kind)
        {
            //return Create(kind);
            return XCodeService.Resolve<IEntityAccessor>(kind);
        }

        internal static Boolean EqualIgnoreCase(this String str, EntityAccessorOptions option)
        {
            //if (String.IsNullOrEmpty(str)) return false;

            return String.Equals(str, option.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}