using System;
using System.IO;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>序列化访问器。接口实现者可以在这里完全自定义序列化行为</summary>
    public interface IAccessor
    {
        /// <summary>从数据流中读取消息</summary>
        /// <param name="stream">数据流</param>
        /// <param name="context">上下文</param>
        /// <returns>是否成功</returns>
        Boolean Read(Stream stream, Object context);

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="stream">数据流</param>
        /// <param name="context">上下文</param>
        /// <returns>是否成功</returns>
        Boolean Write(Stream stream, Object context);
    }

    /// <summary>访问器助手</summary>
    public static class AccessorHelper
    {
        /// <summary>支持访问器的对象转数据包</summary>
        /// <param name="accessor">访问器</param>
        /// <param name="context">上下文</param>
        /// <returns></returns>
        public static Packet ToPacket(this IAccessor accessor, Object context = null)
        {
            var ms = new MemoryStream();
            accessor.Write(ms, context);

            ms.Position = 0;
            return new Packet(ms);
        }

        /// <summary>通过访问器读取</summary>
        /// <param name="type"></param>
        /// <param name="pk"></param>
        /// <param name="context">上下文</param>
        /// <returns></returns>
        public static Object AccessorRead(this Type type, Packet pk, Object context = null)
        {
            var obj = type.CreateInstance();
            (obj as IAccessor).Read(pk.GetStream(), context);

            return obj;
        }

        /// <summary>通过访问器转换数据包为实体对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pk"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static T ToEntity<T>(this Packet pk, Object context = null) where T : IAccessor, new()
        {
            //if (!typeof(T).As<IAccessor>()) return default(T);

            var obj = new T();
            obj.Read(pk.GetStream(), context);

            return obj;
        }
    }
}