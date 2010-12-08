using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using NewLife.Reflection;

namespace NewLife.Collections
{
    /// <summary>
    /// 增强的强类型列表
    /// </summary>
    [Serializable]
    public class ListX<T> : List<T> //where T : IIndexAccessor
    {
        #region 构造函数
        /// <summary>
        /// 构造一个实体对象集合
        /// </summary>
        public ListX() { }

        /// <summary>
        /// 构造一个实体对象集合
        /// </summary>
        /// <param name="collection"></param>
        public ListX(IEnumerable<T> collection)
            : base(collection)
        {
        }

        /// <summary>
        /// 构造一个实体对象集合
        /// </summary>
        /// <param name="capacity"></param>
        public ListX(Int32 capacity) : base(capacity) { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="collection"></param>
        public ListX(IEnumerable collection)
        {
            foreach (T item in collection)
            {
                Add(item);
            }
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("ListX<{0}>[Count={1}]", typeof(T).Name, Count);
        }
        #endregion

        #region 重载运算符
        /// <summary>
        /// 集合相加
        /// </summary>
        /// <param name="entities1">第一个实体集合</param>
        /// <param name="entities2">第二个实体集合</param>
        /// <returns></returns>
        public static ListX<T> operator +(ListX<T> entities1, ListX<T> entities2)
        {
            if ((entities1 == null || entities1.Count < 1) && (entities2 == null || entities2.Count < 1)) return null;

            ListX<T> list = new ListX<T>();
            if (entities1 != null && entities1.Count > 0) list.AddRange(entities1);
            if (entities2 != null && entities2.Count > 0) list.AddRange(entities2);

            return list;
        }

        /// <summary>
        /// 集合相减
        /// </summary>
        /// <param name="entities1">第一个实体集合</param>
        /// <param name="entities2">第二个实体集合</param>
        /// <returns></returns>
        public static ListX<T> operator -(ListX<T> entities1, ListX<T> entities2)
        {
            if (entities1 == null || entities1.Count < 1) return null;

            ListX<T> list = new ListX<T>();
            foreach (T item in entities1)
            {
                if (entities2 != null && !entities2.Contains(item)) list.Add(item);
            }

            if (list == null || list.Count < 1) return null;

            return list;
        }
        #endregion

        #region 集合操作
        /// <summary>
        /// 从集合中移除另一个集合指定的元素
        /// </summary>
        /// <param name="collection"></param>
        public void RemoveRange(IEnumerable<T> collection)
        {
            if (collection == null) return;

            foreach (T item in collection)
            {
                if (Contains(item)) Remove(item);
            }
        }
        #endregion

        #region 对象查询
        /// <summary>
        /// 根据指定项查找
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public ListX<T> FindAll(String name, Object value)
        {
            if (Count < 1) return null;

            ListX<T> list = new ListX<T>();
            foreach (T item in this)
            {
                if (item == null) continue;
                //if (Object.Equals(GetValue(item, name), value)) list.Add(item);
                if (Object.Equals(GetValue(item, name), value)) list.Add(item);
            }
            if (list == null || list.Count < 1) return null;
            return list;
        }

        /// <summary>
        /// 检索与指定谓词定义的条件匹配的所有元素。
        /// </summary>
        /// <param name="match">条件</param>
        /// <returns></returns>
        public new ListX<T> FindAll(Predicate<T> match)
        {
            if (Count < 1) return null;

            ListX<T> list = new ListX<T>();
            foreach (T item in this)
            {
                if (item == null) continue;
                if (match(item)) list.Add(item);
            }
            if (list == null || list.Count < 1) return null;
            return list;
        }

        /// <summary>
        /// 根据指定项查找
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public T Find(String name, Object value)
        {
            if (Count < 1) return default(T);

            foreach (T item in this)
            {
                if (item == null) continue;
                if (Object.Equals(GetValue(item, name), value)) return item;
            }
            return default(T);
        }

        /// <summary>
        /// 根据指定项查找字符串。忽略大小写
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public ListX<T> FindAllIgnoreCase(String name, String value)
        {
            if (Count < 1) return null;

            ListX<T> list = new ListX<T>();
            foreach (T item in this)
            {
                if (item == null) continue;
                if (String.Equals((String)GetValue(item, name), value, StringComparison.OrdinalIgnoreCase)) list.Add(item);
            }
            if (list == null || list.Count < 1) return null;
            return list;
        }

        /// <summary>
        /// 根据指定项查找字符串。忽略大小写
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public T FindIgnoreCase(String name, String value)
        {
            if (Count < 1) return default(T);

            foreach (T item in this)
            {
                if (item == null) continue;
                if (String.Equals((String)GetValue(item, name), value, StringComparison.OrdinalIgnoreCase)) return item;
            }
            return default(T);
        }

        /// <summary>
        /// 集合是否包含指定项
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean Exists(String name, Object value)
        {
            if (Count < 1) return false;

            foreach (T item in this)
            {
                if (item == null) continue;
                if (Object.Equals(GetValue(item, name), value)) return true;
            }
            return false;
        }

        /// <summary>
        /// 按指定字段排序
        /// </summary>
        /// <param name="name">字段</param>
        /// <param name="isDesc">是否降序</param>
        public void Sort(String name, Boolean isDesc)
        {
            if (Count < 1) return;

            if (!(GetValue(this[0], name) is IComparable)) throw new Exception("不支持比较！");

            Int32 n = 1;
            if (isDesc) n = -1;

            Sort(delegate(T item1, T item2)
            {
                return (GetValue(item1, name) as IComparable).CompareTo(GetValue(item2, name)) * n;
            });
        }

        /// <summary>
        /// 按指定字段数组排序
        /// </summary>
        /// <param name="names">字段</param>
        /// <param name="isDescs">是否降序</param>
        public void Sort(String[] names, Boolean[] isDescs)
        {
            if (Count < 1) return;

            for (int i = 0; i < names.Length; i++)
            {
                String name = names[i];
                Boolean isDesc = isDescs[i];

                if (!(GetValue(this[0], name) is IComparable)) throw new Exception("不支持比较！");
            }

            Sort(delegate(T item1, T item2)
            {
                // 逐层对比
                for (int i = 0; i < names.Length; i++)
                {
                    String name = names[i];
                    Boolean isDesc = isDescs[i];

                    Int32 n = 1;
                    if (isDesc) n = -1;

                    n = (GetValue(item1, name) as IComparable).CompareTo(GetValue(item2, name)) * n;
                    if (n != 0) return n;
                }
                return 0;
            });
        }
        #endregion

        #region 对象操作
        /// <summary>
        /// 获取所有实体中指定项的值
        /// </summary>
        /// <typeparam name="TResult">指定项的类型</typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<TResult> GetItem<TResult>(String name)
        {
            if (Count < 1) return null;

            List<TResult> list = new List<TResult>();
            foreach (T item in this)
            {
                if (item == null) continue;
                list.Add((TResult)GetValue(item, name));
            }
            return list;
        }
        #endregion

        #region 导入导出
        /// <summary>
        /// 导出
        /// </summary>
        /// <param name="writer"></param>
        public virtual void Export(TextWriter writer)
        {
            //XmlAttributeOverrides ovs = new XmlAttributeOverrides();
            //foreach (FieldItem item in (this[0] as IEntityOperate).Fields)
            //{
            //    XmlAttributes atts = new XmlAttributes();
            //    atts.XmlAttribute = new XmlAttributeAttribute();
            //    ovs.Add(item.Property.DeclaringType, item.Name, atts);
            //}
            //XmlSerializer serial = new XmlSerializer(this.GetType(), ovs);

            XmlSerializer serial = CreateXmlSerializer();
            serial.Serialize(writer, this);

            //XmlDocument doc = new XmlDocument();
            //doc.WriteTo(new XmlTextWriter(writer));

            //XmlTextWriter xmlwriter = new XmlTextWriter(writer);
            //xmlwriter.WriteStartDocument();
            //xmlwriter.WriteStartElement(typeof(T).Name);
            //foreach (T item in this)
            //{
            //    item.ToXml(xmlwriter);
            //    break;
            //}
            //xmlwriter.WriteEndElement();
            //xmlwriter.WriteEndDocument();
        }

        /// <summary>
        /// 导入
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public virtual void Import(TextReader reader)
        {
            //XmlAttributeOverrides ovs = new XmlAttributeOverrides();
            //foreach (FieldItem item in (this[0] as IEntityOperate).Fields)
            //{
            //    XmlAttributes atts = new XmlAttributes();
            //    atts.XmlAttribute = new XmlAttributeAttribute();
            //    ovs.Add(item.Property.DeclaringType, item.Name, atts);
            //}
            //XmlSerializer serial = new XmlSerializer(this.GetType(), ovs);

            XmlSerializer serial = CreateXmlSerializer();
            ListX<T> list = serial.Deserialize(reader) as ListX<T>;

            AddRange(list);
        }

        private XmlSerializer CreateXmlSerializer()
        {
            //XmlAttributeOverrides ovs = new XmlAttributeOverrides();
            //IEntityOperate factory = EntityFactory.CreateOperate(typeof(T));
            //IEntity entity = factory.Create();
            //foreach (FieldItem item in factory.Fields)
            //{
            //    XmlAttributes atts = new XmlAttributes();
            //    atts.XmlAttribute = new XmlAttributeAttribute();
            //    atts.XmlDefaultValue = entity[item.Name];
            //    ovs.Add(item.Property.DeclaringType, item.Name, atts);
            //}
            //return new XmlSerializer(this.GetType(), ovs);
            return new XmlSerializer(this.GetType());
        }

        /// <summary>
        /// 导出Xml文本
        /// </summary>
        /// <returns></returns>
        public virtual String ToXml()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
                Export(writer);
                Byte[] bts = stream.ToArray();
                String xml = Encoding.UTF8.GetString(bts);
                writer.Close();
                if (!String.IsNullOrEmpty(xml)) xml = xml.Trim();
                return xml;
            }
        }

        /// <summary>
        /// 导入Xml文本
        /// </summary>
        /// <param name="xml"></param>
        public virtual void FromXml(String xml)
        {
            if (String.IsNullOrEmpty(xml)) return;
            xml = xml.Trim();
            using (StringReader reader = new StringReader(xml))
            {
                Import(reader);
            }
        }
        #endregion

        #region 转换
        /// <summary>
        /// 任意集合转为实体集合
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static ListX<T> From(IEnumerable collection)
        {
            if (collection == null || collection.GetEnumerator() == null) return null;

            return new ListX<T>(collection);
        }

        ///// <summary>
        ///// 拥有指定类型转换器的转换
        ///// </summary>
        ///// <param name="collection"></param>
        ///// <param name="func"></param>
        ///// <returns></returns>
        //public static ListX<T> From(IEnumerable collection, Func<Object, T> func)
        //{
        //    if (collection == null || collection.GetEnumerator() == null || collection.GetEnumerator().Current == null) return null;

        //    ListX<T> list = new ListX<T>();
        //    foreach (Object item in collection)
        //    {
        //        if (func == null)
        //            list.Add((T)item);
        //        else
        //            list.Add(func(item));
        //    }
        //    if (list == null || list.Count < 1) return null;
        //    return list;
        //}

        /// <summary>
        /// 拥有指定类型转换器的转换
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="collection"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static ListX<T> From<T2>(IEnumerable collection, Func<T2, T> func)
        {
            if (collection == null || collection.GetEnumerator() == null) return null;

            ListX<T> list = new ListX<T>();
            foreach (T2 item in collection)
            {
                if (item == null) continue;
                T entity = default(T);
                if (func == null)
                    entity = (T)(Object)item;
                else
                    entity = func(item);
                if (entity != null) list.Add(entity);
            }
            if (list == null || list.Count < 1) return null;
            return list;
        }
        #endregion

        #region 快速访问
        /// <summary>
        /// 快速取值，如果实现了IIndexAccessor接口，则优先采用
        /// </summary>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        static Object GetValue(Object target, String name)
        {
            if (target is IIndexAccessor)
                return (target as IIndexAccessor)[name];
            else
                return FastIndexAccessor.GetValue(target, name);
        }

        //static void SetValue(Object target, String name, Object value)
        //{
        //    FastIndexAccessor.SetValue(target, name, value);
        //}
        #endregion
    }
}