using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using NewLife.Reflection;
using XCode.Configuration;
using XCode.DataAccessLayer;
using System.ComponentModel;
using NewLife.IO;

namespace XCode
{
    /// <summary>
    /// 实体集合
    /// </summary>
    [Serializable]
    public partial class EntityList<T> : List<T>, IListSource where T : IEntity
    {
        #region 构造函数
        /// <summary>
        /// 构造一个实体对象集合
        /// </summary>
        public EntityList() { }

        /// <summary>
        /// 构造一个实体对象集合
        /// </summary>
        /// <param name="collection"></param>
        public EntityList(IEnumerable<T> collection)
            : base(collection)
        {
        }

        /// <summary>
        /// 构造一个实体对象集合
        /// </summary>
        /// <param name="capacity"></param>
        public EntityList(Int32 capacity) : base(capacity) { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="collection"></param>
        public EntityList(IEnumerable collection)
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
            return String.Format("EntityList<{0}>[Count={1}]", typeof(T).Name, Count);
        }
        #endregion

        #region 重载运算符
        /// <summary>
        /// 集合相加
        /// </summary>
        /// <param name="entities1">第一个实体集合</param>
        /// <param name="entities2">第二个实体集合</param>
        /// <returns></returns>
        public static EntityList<T> operator +(EntityList<T> entities1, EntityList<T> entities2)
        {
            if ((entities1 == null || entities1.Count < 1) && (entities2 == null || entities2.Count < 1)) return null;

            EntityList<T> list = new EntityList<T>();
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
        public static EntityList<T> operator -(EntityList<T> entities1, EntityList<T> entities2)
        {
            if (entities1 == null || entities1.Count < 1) return null;

            EntityList<T> list = new EntityList<T>();
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
        public EntityList<T> FindAll(String name, Object value)
        {
            if (Count < 1) return null;

            EntityList<T> list = new EntityList<T>();
            foreach (T item in this)
            {
                if (item == null) continue;
                if (Object.Equals(item[name], value)) list.Add(item);
            }
            if (list == null || list.Count < 1) return null;
            return list;
        }

        /// <summary>
        /// 检索与指定谓词定义的条件匹配的所有元素。
        /// </summary>
        /// <param name="match">条件</param>
        /// <returns></returns>
        public new EntityList<T> FindAll(Predicate<T> match)
        {
            if (Count < 1) return null;

            EntityList<T> list = new EntityList<T>();
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
                if (Object.Equals(item[name], value)) return item;
            }
            return default(T);
        }

        /// <summary>
        /// 根据指定项查找字符串。忽略大小写
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public EntityList<T> FindAllIgnoreCase(String name, String value)
        {
            if (Count < 1) return null;

            EntityList<T> list = new EntityList<T>();
            foreach (T item in this)
            {
                if (item == null) continue;
                if (String.Equals((String)item[name], value, StringComparison.OrdinalIgnoreCase)) list.Add(item);
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
                if (String.Equals((String)item[name], value, StringComparison.OrdinalIgnoreCase)) return item;
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
                if (Object.Equals(item[name], value)) return true;
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

            Type type = GetItemType(name);
            if (!typeof(IComparable).IsAssignableFrom(type)) throw new Exception("不支持比较！");

            Int32 n = 1;
            if (isDesc) n = -1;

            Sort(delegate(T item1, T item2)
            {
                // Object.Equals可以有效的处理两个元素都为空的问题
                if (Object.Equals(item1[name], item2[name])) return 0;
                return (item1[name] as IComparable).CompareTo(item2[name]) * n;
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

                Type type = GetItemType(name);
                if (!typeof(IComparable).IsAssignableFrom(type)) throw new Exception("不支持比较！");
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

                    // Object.Equals可以有效的处理两个元素都为空的问题
                    if (Object.Equals(item1[name], item2[name]))
                        n = 0;
                    else
                        n = (item1[name] as IComparable).CompareTo(item2[name]) * n;
                    if (n != 0) return n;
                }
                return 0;
            });
        }

        Type GetItemType(String name)
        {
            if (String.IsNullOrEmpty(name) || EntityType == null) return null;

            PropertyInfoX pi = PropertyInfoX.Create(EntityType, name);
            if (pi != null) return pi.Type;

            FieldInfoX fi = FieldInfoX.Create(EntityType, name);
            if (fi != null) return fi.Type;

            return null;
        }
        #endregion

        #region 对象操作
        /// <summary>
        /// 把整个集合插入到数据库
        /// </summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public Int32 Insert(Boolean useTransition)
        {
            if (Count < 1) return 0;

            Int32 count = 0;

            if (useTransition)
            {
                IEntityOperate dal = Factory;
                dal.BeginTransaction();
                try
                {
                    foreach (T item in this)
                    {
                        count += item.Insert();
                    }

                    dal.Commit();
                }
                catch
                {
                    dal.Rollback();
                    throw;
                }
            }
            else
            {
                foreach (T item in this)
                {
                    count += item.Insert();
                }
            }

            return count;
        }

        /// <summary>
        /// 把整个集合插入到数据库
        /// </summary>
        /// <returns></returns>
        public Int32 Insert()
        {
            return Insert(true);
        }

        /// <summary>
        /// 把整个集合更新到数据库
        /// </summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public Int32 Update(Boolean useTransition)
        {
            if (Count < 1) return 0;

            Int32 count = 0;

            if (useTransition)
            {
                IEntityOperate dal = Factory;
                dal.BeginTransaction();
                try
                {
                    foreach (T item in this)
                    {
                        count += item.Update();
                    }

                    dal.Commit();
                }
                catch
                {
                    dal.Rollback();
                    throw;
                }
            }
            else
            {
                foreach (T item in this)
                {
                    count += item.Update();
                }
            }

            return count;
        }

        /// <summary>
        /// 把整个集合更新到数据库
        /// </summary>
        /// <returns></returns>
        public Int32 Update()
        {
            return Update(true);
        }

        /// <summary>
        /// 把整个保存更新到数据库
        /// </summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public Int32 Save(Boolean useTransition)
        {
            if (Count < 1) return 0;

            Int32 count = 0;

            if (useTransition)
            {
                IEntityOperate dal = Factory;
                dal.BeginTransaction();
                try
                {
                    foreach (T item in this)
                    {
                        count += item.Save();
                    }

                    dal.Commit();
                }
                catch
                {
                    dal.Rollback();
                    throw;
                }
            }
            else
            {
                foreach (T item in this)
                {
                    count += item.Save();
                }
            }

            return count;
        }

        /// <summary>
        /// 把整个集合保存到数据库
        /// </summary>
        /// <returns></returns>
        public Int32 Save()
        {
            return Save(true);
        }

        /// <summary>
        /// 把整个集合从数据库中删除
        /// </summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public Int32 Delete(Boolean useTransition)
        {
            if (Count < 1) return 0;

            Int32 count = 0;

            if (useTransition)
            {
                IEntityOperate dal = Factory;
                dal.BeginTransaction();
                try
                {
                    foreach (T item in this)
                    {
                        count += item.Delete();
                    }

                    dal.Commit();
                }
                catch
                {
                    dal.Rollback();
                    throw;
                }
            }
            else
            {
                foreach (T item in this)
                {
                    count += item.Delete();
                }
            }

            return count;
        }

        /// <summary>
        /// 把整个集合从数据库中删除
        /// </summary>
        /// <returns></returns>
        public Int32 Delete()
        {
            return Delete(true);
        }

        /// <summary>
        /// 设置所有实体中指定项的值
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetItem(String name, Object value)
        {
            if (Count < 1) return;

            foreach (T item in this)
            {
                if (item == null) continue;
                if (!Object.Equals(item[name], value)) item.SetItem(name, value);
            }
        }

        /// <summary>
        /// 获取所有实体中指定项的值
        /// </summary>
        /// <typeparam name="TResult">指定项的类型</typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<TResult> GetItem<TResult>(String name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            if (Count < 1) return null;

            List<TResult> list = new List<TResult>();
            foreach (T item in this)
            {
                if (item == null) continue;
                list.Add((TResult)item[name]);
            }
            return list;
        }

        /// <summary>
        /// 串联指定成员，方便由实体集合构造用于查询的子字符串
        /// </summary>
        /// <param name="name"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public String Join(String name, String separator)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            if (Count < 1) return null;

            List<String> list = GetItem<String>(name);
            if (list == null || list.Count < 1) return null;

            return String.Join(separator, list.ToArray());
        }

        /// <summary>
        /// 串联
        /// </summary>
        /// <param name="separator"></param>
        /// <returns></returns>
        public String Join(String separator)
        {
            if (Count < 1) return null;

            StringBuilder sb = new StringBuilder();
            foreach (T item in this)
            {
                if (sb.Length > 0) sb.Append(separator);
                sb.Append("" + item);
            }
            return sb.ToString();
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

            IList list = this;
            if (typeof(T).IsInterface) list = (this as IListSource).GetList();

            XmlSerializer serial = CreateXmlSerializer(list.GetType());
            serial.Serialize(writer, list);

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

            XmlSerializer serial = CreateXmlSerializer(this.GetType());
            EntityList<T> list = serial.Deserialize(reader) as EntityList<T>;

            AddRange(list);
        }

        private XmlSerializer CreateXmlSerializer(Type type)
        {
            XmlAttributeOverrides ovs = new XmlAttributeOverrides();
            IEntityOperate factory = Factory;
            IEntity entity = factory.Create();
            foreach (FieldItem item in factory.Fields)
            {
                XmlAttributes atts = new XmlAttributes();
                atts.XmlAttribute = new XmlAttributeAttribute();
                atts.XmlDefaultValue = entity[item.Name];
                ovs.Add(item.Property.DeclaringType, item.Name, atts);
            }
            return new XmlSerializer(type, ovs);
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

        /// <summary>
        /// 导出Json
        /// </summary>
        /// <returns></returns>
        public virtual String ToJson()
        {
            return new Json().Serialize(this);
        }

        /// <summary>
        /// 导入Json
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static EntityList<T> FromJson(String json)
        {
            return new Json().Deserialize<EntityList<T>>(json);
        }
        #endregion

        #region 转换
        /// <summary>
        /// 任意集合转为实体集合
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static EntityList<T> From(IEnumerable collection)
        {
            if (collection == null || collection.GetEnumerator() == null) return null;

            return new EntityList<T>(collection);
        }

        ///// <summary>
        ///// 拥有指定类型转换器的转换
        ///// </summary>
        ///// <param name="collection"></param>
        ///// <param name="func"></param>
        ///// <returns></returns>
        //public static EntityList<T> From(IEnumerable collection, Func<Object, T> func)
        //{
        //    if (collection == null || collection.GetEnumerator() == null || collection.GetEnumerator().Current == null) return null;

        //    EntityList<T> list = new EntityList<T>();
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
        public static EntityList<T> From<T2>(IEnumerable collection, Func<T2, T> func)
        {
            if (collection == null || collection.GetEnumerator() == null) return null;

            EntityList<T> list = new EntityList<T>();
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

        #region IListSource接口
        bool IListSource.ContainsListCollection
        {
            get { return false; }
        }

        IList IListSource.GetList()
        {
            // 如果是接口，创建新的集合，否则返回自身
            if (!typeof(T).IsInterface) return this;

            if (Count < 1) return null;

            return ToArray(null);
        }
        #endregion

        #region 复制
        IList ToArray(Type type)
        {
            if (Count < 1) return null;

            // 元素类型
            if (type == null) type = EntityType;
            // 泛型
            type = typeof(EntityListView<>).MakeGenericType(type);

            // 初始化集合，实际上是创建了一个真正的实体类型
            IList list = TypeX.CreateInstance(type) as IList;
            for (int i = 0; i < Count; i++)
            {
                list.Add(this[i]);
            }

            return list;
        }
        #endregion

        #region 辅助函数
        /// <summary>
        /// 真正的实体类型。有些场合为了需要会使用IEntity。
        /// </summary>
        Type EntityType
        {
            get
            {
                Type type = typeof(T);
                if (!type.IsInterface) return type;

                if (Count > 0) return this[0].GetType();

                return type;
            }
        }

        /// <summary>
        /// 实体操作者
        /// </summary>
        IEntityOperate Factory
        {
            get
            {
                Type type = EntityType;
                if (type.IsInterface) return null;

                return EntityFactory.CreateOperate(type);
            }
        }
        #endregion
    }
}