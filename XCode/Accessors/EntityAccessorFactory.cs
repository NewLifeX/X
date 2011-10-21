using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Model;
using XCode.Model;

namespace XCode.Accessors
{
    /// <summary>实体访问器工厂</summary>
    public class EntityAccessorFactory
    {
        internal static void Reg(IObjectContainer container)
        {
            // 注册内置访问器
            container
                .Register<IEntityAccessor, HttpEntityAccessor>(EntityAccessorTypes.Http.ToString(), false)
                .Register<IEntityAccessor, WebFormEntityAccessor>(EntityAccessorTypes.WebForm.ToString(), false)
                .Register<IEntityAccessor, WinFormEntityAccessor>(EntityAccessorTypes.WinForm.ToString(), false)
                .Register<IEntityAccessor, BinaryEntityAccessor>(EntityAccessorTypes.Binary.ToString(), false)
                .Register<IEntityAccessor, XmlEntityAccessor>(EntityAccessorTypes.Xml.ToString(), false)
                .Register<IEntityAccessor, JsonEntityAccessor>(EntityAccessorTypes.Json.ToString(), false);
        }

        /// <summary>
        /// 创建指定类型的实体访问器
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static IEntityAccessor Create(String name, IDictionary<String, Object> ps = null)
        {
            IEntityAccessor accessor = XCodeService.Resolve<IEntityAccessor>(name);
            accessor.Init(ps);
            return accessor;
        }

        /// <summary>
        /// 创建指定类型的实体访问器
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static IEntityAccessor Create(EntityAccessorTypes kind, IDictionary<String, Object> ps = null)
        {
            return Create(kind.ToString(), ps);
        }
    }
}