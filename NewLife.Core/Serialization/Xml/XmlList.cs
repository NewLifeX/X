using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>列表数据编码</summary>
    public class XmlList : XmlHandlerBase
    {
        /// <summary>初始化</summary>
        public XmlList()
        {
            // 优先级
            Priority = 20;
        }

        /// <summary>写入</summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            if (!type.As<IList>()) return false;

            var list = value as IList;
            if (list == null || list.Count == 0) return true;

            WriteLog("XmlWrite {0} 元素{1}项", type.Name, list.Count);

            Host.Hosts.Push(value);

            //var xml = Host as Xml;
            //xml.WriteStart(type);
            try
            {
                // 循环写入数据
                foreach (var item in list)
                {
                    if (!Host.Write(item)) return false;
                }
            }
            finally
            {
                //xml.WriteEnd();

                Host.Hosts.Pop();
            }

            return true;
        }

        /// <summary>读取</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (!type.As<IList>()) return false;

            var reader = Host.GetReader();
            if (!reader.IsStartElement()) return true;

            // 读一次开始，移动到内部第一个元素
            //reader.ReadStartElement();

            // 子元素类型
            var elmType = type.GetElementTypeEx();

            var list = value as IList;
            if (list == null|| value is Array) list = typeof(List<>).MakeGenericType(elmType).CreateInstance() as IList;

            // 清空已有数据
            list.Clear();

            while (reader.IsStartElement())
            {
                Object obj = null;
                if (!Host.TryRead(elmType, ref obj)) return false;

                list.Add(obj);
            }

            if (value != list)
            {
                // 数组的创建比较特别
                if (type.As<Array>())
                {
                    var arr = Array.CreateInstance(type.GetElementTypeEx(), list.Count);
                    list.CopyTo(arr, 0);
                    value = arr;
                }
                else
                    value = list;
            }

            // 读一次结束
            //if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();

            return true;
        }
    }
}