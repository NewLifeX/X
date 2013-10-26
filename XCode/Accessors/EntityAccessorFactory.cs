using System;
using NewLife.Model;
using NewLife.Reflection;
using XCode.Model;

namespace XCode.Accessors
{
    /// <summary>实体访问器工厂</summary>
    public static class EntityAccessorFactory
    {
        internal static void Reg(IObjectContainer container)
        {
            Func<Object, Object> callback = e =>
            {
                var ea = e as EntityAccessorBase;
                if (ea != null) return ea.Kind;

                return null;
            };
            // 注册内置访问器
            container
                .AutoRegister<IEntityAccessor, HttpEntityAccessor>(callback, EntityAccessorTypes.Http)
                .AutoRegister<IEntityAccessor, WebFormEntityAccessor>(callback, EntityAccessorTypes.WebForm)
                .AutoRegister<IEntityAccessor, WinFormEntityAccessor>(callback, EntityAccessorTypes.WinForm)
                .AutoRegister<IEntityAccessor, BinaryEntityAccessor>(callback, EntityAccessorTypes.Binary)
                .AutoRegister<IEntityAccessor, XmlEntityAccessor>(callback, EntityAccessorTypes.Xml)
                .AutoRegister<IEntityAccessor, JsonEntityAccessor>(callback, EntityAccessorTypes.Json);
        }

        /// <summary>创建指定类型的实体访问器</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static IEntityAccessor Create(String name)
        {
            return XCodeService.Resolve<IEntityAccessor>(Enum.Parse(typeof(EntityAccessorTypes), name));
        }

        /// <summary>创建指定类型的实体访问器</summary>
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

            return str.EqualIgnoreCase(option.ToString());
        }
    }
}