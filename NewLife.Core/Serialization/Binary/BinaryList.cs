using System;
using System.Collections;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>列表数据编码</summary>
    public class BinaryList : BinaryHandlerBase
    {
        /// <summary>初始化</summary>
        public BinaryList()
        {
            // 优先级
            Priority = 10;
        }

        /// <summary>写入</summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public override bool Write(object value, Type type)
        {
            if (!typeof(IList).IsAssignableFrom(type)) return false;

            var list = value as IList;
            // 先写入长度
            if (list.Count == 0)
            {
                Host.WriteSize(0);
                return true;
            }

            Host.WriteSize(list.Count);

            // 循环写入数据
            foreach (var item in list)
            {
                Host.Write(item);
            }

            return true;
        }

        /// <summary>读取</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool TryRead(Type type, ref object value)
        {
            if (!typeof(IList).IsAssignableFrom(type)) return false;

            if (value == null && type != null) value = type.CreateInstance();

            // 先读取长度
            var count = Host.ReadSize();
            if (count == 0) return true;

            // 子元素类型
            var elmType = type.GetElementTypeEx();

            var list = value as IList;
            for (int i = 0; i < count; i++)
            {
                Object obj = null;
                if (!Host.TryRead(elmType, ref obj)) return false;
                list.Add(obj);
            }

            return true;
        }
    }
}