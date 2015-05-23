using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using NewLife;
using NewLife.IO;
using NewLife.Reflection;
using XCode.Common;
using XCode.Configuration;

namespace XCode
{
    /// <summary>实体集合，提供批量查询和批量操作实体等操作。若需要使用Linq，需要先用<see cref="ToList"/>方法。</summary>
    /// <remarks>
    /// 强烈建议所有返回实体集合的方法，在没有满足条件的数据时返回空集合而不是null，以减少各种判断！
    /// 
    /// 在.Net 2.0时代，没有Linq可用时，该类的对象查询等方法发挥了很大的作用。
    /// 但是弱类型比较的写法，不太方便，并且有时会带来非常难以查找的错误。
    /// 比如Object.Equal比较Int16和Int32两个数字，是不相等的，也就是说，如果有个Int32字段，传入Int16的数字是无法找到任何匹配项的。
    /// 
    /// 后来在.Net 2.0上实现了Linq，该类的对象查询方法将会逐步淡出，建议优先考虑Linq。
    /// </remarks>
    [Serializable]
    public partial class EntityList<T> : List<T>, IEntityList, IList, IList<IEntity>, IListSource, IEnumerable, ICloneable where T : IEntity
    {
        #region 构造函数
        /// <summary>构造一个实体对象集合</summary>
        public EntityList() { }

        /// <summary>构造一个实体对象集合</summary>
        /// <param name="collection"></param>
        public EntityList(IEnumerable<T> collection) : base(collection) { }

        /// <summary>构造一个实体对象集合</summary>
        /// <param name="capacity"></param>
        public EntityList(Int32 capacity) : base(capacity) { }

        /// <summary>初始化</summary>
        /// <param name="collection"></param>
        public EntityList(IEnumerable collection)
        {
            if (collection != null)
            {
                foreach (T item in collection)
                {
                    Add(item);
                }
            }
        }

        /// <summary>初始化</summary>
        /// <param name="collection"></param>
        /// <param name="startRowIndex"></param>
        /// <param name="maximumRows"></param>
        public EntityList(IEnumerable collection, Int32 startRowIndex, Int32 maximumRows)
        {
            if (collection != null)
            {
                var i = startRowIndex > 0 ? startRowIndex : 0;

                foreach (T item in collection)
                {
                    if (maximumRows > 0 && ++i > maximumRows) break;

                    Add(item);
                }
            }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("EntityList<{0}>[Count={1}]", typeof(T).Name, Count);
        }
        #endregion

        #region 重载运算符
        /// <summary>集合相加</summary>
        /// <param name="entities1">第一个实体集合</param>
        /// <param name="entities2">第二个实体集合</param>
        /// <returns></returns>
        public static EntityList<T> operator +(EntityList<T> entities1, EntityList<T> entities2)
        {
            if ((entities1 == null || entities1.Count < 1) &&
                (entities2 == null || entities2.Count < 1)) return entities1;

            var list = new EntityList<T>();
            if (entities1 != null && entities1.Count > 0) list.AddRange(entities1);
            if (entities2 != null && entities2.Count > 0) list.AddRange(entities2);

            return list;
        }

        /// <summary>集合相减</summary>
        /// <param name="entities1">第一个实体集合</param>
        /// <param name="entities2">第二个实体集合</param>
        /// <returns></returns>
        public static EntityList<T> operator -(EntityList<T> entities1, EntityList<T> entities2)
        {
            if ((entities1 == null || entities1.Count < 1) &&
                (entities2 == null || entities2.Count < 1)) return entities1;

            var list = new EntityList<T>(entities1);
            list.RemoveAll(e => entities2.Contains(e));
            return list;
        }
        #endregion

        #region 集合操作
        /// <summary>从集合中移除另一个集合指定的元素</summary>
        /// <param name="collection"></param>
        public EntityList<T> RemoveRange(IEnumerable<T> collection)
        {
            if (collection == null) return this;

            foreach (T item in collection)
            {
                if (Contains(item)) Remove(item);
            }

            return this;
        }

        /// <summary>分页</summary>
        /// <param name="startRowIndex">起始索引，0开始</param>
        /// <param name="maximumRows">最大个数</param>
        /// <returns></returns>
        public EntityList<T> Page(Int32 startRowIndex, Int32 maximumRows)
        {
            if (Count <= 0) return this;

            if (startRowIndex <= 0 && (maximumRows <= 0 || maximumRows >= Count)) return this;

            // 先转数组再构造分页，避免多线程版本冲突
            //return new EntityList<T>(ToArray(), startRowIndex, maximumRows);
            return new EntityList<T>(ToList().Skip(startRowIndex).Take(maximumRows));
        }
        #endregion

        #region 对象查询
        /// <summary>根据指定项查找。没有数据时返回空集合而不是null</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public EntityList<T> FindAll(String name, Object value)
        {
            if (Count < 1) return this;

            // 先排除掉没有必要的查找，唯一键空值查找没有意义
            FieldItem field = Factory.Table.FindByName(name);
            if (field != null && (field.IsIdentity || field.PrimaryKey))
            {
                // 唯一键为空时，比如自增且参数小于等于0时，返回空
                if (Helper.IsNullKey(value, field.Type)) return new EntityList<T>();
            }

            // 特殊处理整数类型，避免出现相同值不同整型而导致结果不同
            if (value != null && value.GetType().IsIntType())
            {
                // 整型统一转为Int64后再比较，因为即使数值相等，类型不同的对象也是不等的
                var v6 = Convert.ToInt64(value);
                var list = base.FindAll(e => Convert.ToInt64(e[name]) == v6);
                return new EntityList<T>(list);
            }
            else
            {
                var list = base.FindAll(e => Object.Equals(e[name], value));
                return new EntityList<T>(list);
            }
        }

        /// <summary>根据指定项查找。没有数据时返回空集合而不是null</summary>
        /// <param name="names">属性名集合</param>
        /// <param name="values">属性值集合</param>
        /// <param name="ignoreCase">对于字符串字段是否忽略大小写</param>
        /// <returns></returns>
        public EntityList<T> FindAll(String[] names, Object[] values, Boolean ignoreCase = false)
        {
            if (Count < 1) return this;

            FieldItem field = Factory.Table.FindByName(names[0]);
            if (field != null && (field.IsIdentity || field.PrimaryKey))
            {
                // 唯一键为自增且参数小于等于0时，返回空
                if (Helper.IsNullKey(values[0], field.Type)) return new EntityList<T>();
            }

            // 特殊处理字符忽略大小写的情况
            var ss = new Boolean[values.Length];
            // 特殊处理整数类型，避免出现相同值不同整型而导致结果不同
            var ts = new Boolean[values.Length];
            var vs = new Int64[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                field = Factory.Table.FindByName(names[i]);
                if (field != null)
                {
                    ss[i] = field.Type == typeof(String);
                    ts[i] = field.Type.IsIntType();
                }

                if (values[i] == null) continue;

                // 整型统一转为Int64后再比较，因为即使数值相等，类型不同的对象也是不等的
                ts[i] |= values[i].GetType().IsIntType();
                if (ts[i]) vs[i] = Convert.ToInt64(values[i]);

                ss[i] |= values[i].GetType() == typeof(String);
            }

            var list = new EntityList<T>();
            for (int k = 0; k < Count; k++)
            {
                var item = this[k];
                if (item == null) continue;

                var b = true;
                for (int i = 0; i < names.Length; i++)
                {
                    var iv = item[names[i]];
                    if (!Object.Equals(iv, values[i]) &&
                        // 整数相等比较
                        !(ts[i] && Convert.ToInt64(iv) == vs[i]) &&
                        // 字符串不区分大小写比较，判定""和null为相等
                        !(ss[i] && ignoreCase && (iv + "").EqualIgnoreCase(values[i] + "")))
                    {
                        b = false;
                        break;
                    }
                }
                if (b) list.Add(item);
            }
            return list;
        }

        /// <summary>检索与指定谓词定义的条件匹配的所有元素。没有数据时返回空集合而不是null</summary>
        /// <param name="match">条件</param>
        /// <returns></returns>
        public new EntityList<T> FindAll(Predicate<T> match)
        {
            if (Count < 1) return new EntityList<T>();

            var list = base.FindAll(match);
            return new EntityList<T>(list);
        }

        /// <summary>根据指定项查找</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public T Find(String name, Object value)
        {
            if (Count < 1) return default(T);

            // 特殊处理整数类型，避免出现相同值不同整型而导致结果不同
            if (value != null && value.GetType().IsIntType())
            {
                // 整型统一转为Int64后再比较，因为即使数值相等，类型不同的对象也是不等的
                var v6 = Convert.ToInt64(value);
                return base.Find(e => Convert.ToInt64(e[name]) == v6);
            }
            else
            {
                return base.Find(e => Object.Equals(e[name], value));
            }
        }

        /// <summary>根据指定项查找字符串，忽略大小写，非字符串属性将报错。没有数据时返回空集合而不是null</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public EntityList<T> FindAllIgnoreCase(String name, String value)
        {
            if (Count < 1) return this;

            var list = base.FindAll(e => ((String)e[name]).EqualIgnoreCase(value));
            return new EntityList<T>(list);
        }

        /// <summary>根据指定项查找字符串。忽略大小写</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public T FindIgnoreCase(String name, String value)
        {
            if (Count < 1) return default(T);

            return Find(e => ((String)e[name]).EqualIgnoreCase(value));
        }

        /// <summary>集合是否包含指定项</summary>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Boolean Exists(String name, Object value)
        {
            if (Count < 1) return false;
            return Find(name, value) != null;

        }
        #endregion

        #region IEntityList接口
        /// <summary>根据指定项查找</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        IEntityList IEntityList.FindAll(String name, Object value) { return FindAll(name, value); }

        /// <summary>根据指定项查找</summary>
        /// <param name="names">属性名</param>
        /// <param name="values">属性值</param>
        /// <returns></returns>
        IEntityList IEntityList.FindAll(String[] names, Object[] values) { return FindAll(names, values); }

        /// <summary>根据指定项查找</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        IEntity IEntityList.Find(String name, Object value) { return Find(name, value); }

        /// <summary>根据指定项查找字符串。忽略大小写</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        IEntityList IEntityList.FindAllIgnoreCase(String name, String value) { return FindAllIgnoreCase(name, value); }

        /// <summary>根据指定项查找字符串。忽略大小写</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        IEntity IEntityList.FindIgnoreCase(String name, String value) { return FindIgnoreCase(name, value); }

        /// <summary>设置所有实体中指定项的值</summary>
        /// <param name="name">指定项的名称</param>
        /// <param name="value">数值</param>
        IEntityList IEntityList.SetItem(String name, Object value) { return SetItem(name, value); }

        IEntityList IEntityList.FromXml(String xml) { return FromXml(xml); }

        /// <summary>分页</summary>
        /// <param name="startRowIndex">起始索引，0开始</param>
        /// <param name="maximumRows">最大个数</param>
        /// <returns></returns>
        IEntityList IEntityList.Page(Int32 startRowIndex, Int32 maximumRows) { return Page(startRowIndex, maximumRows); }
        #endregion

        #region 对象操作
        /// <summary>把整个集合插入到数据库</summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public Int32 Insert(Boolean useTransition = true) { return DoAction(useTransition, e => e.Insert()); }

        /// <summary>把整个集合插入到数据库</summary>
        /// <returns></returns>
        public Int32 Insert() { return Insert(true); }

        /// <summary>把整个集合更新到数据库</summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public Int32 Update(Boolean useTransition = true) { return DoAction(useTransition, e => e.Update()); }

        /// <summary>把整个集合更新到数据库</summary>
        /// <returns></returns>
        public Int32 Update() { return Update(true); }

        /// <summary>把整个保存更新到数据库</summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public Int32 Save(Boolean useTransition = true) { return DoAction(useTransition, e => e.Save()); }

        /// <summary>把整个集合保存到数据库</summary>
        /// <returns></returns>
        public Int32 Save() { return Save(true); }

        /// <summary>把整个保存更新到数据库，保存时不需要验证</summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public Int32 SaveWithoutValid(Boolean useTransition = true) { return DoAction(useTransition, e => e.SaveWithoutValid()); }

        /// <summary>把整个集合保存到数据库，保存时不需要验证</summary>
        /// <returns></returns>
        public Int32 SaveWithoutValid() { return SaveWithoutValid(true); }

        /// <summary>把整个集合从数据库中删除</summary>
        /// <param name="useTransition">是否使用事务保护</param>
        /// <returns></returns>
        public Int32 Delete(Boolean useTransition = true) { return DoAction(useTransition, e => e.Delete()); }

        /// <summary>把整个集合从数据库中删除</summary>
        /// <returns></returns>
        public Int32 Delete() { return DoAction(true, e => e.Delete()); }

        Int32 DoAction(Boolean useTransition, Func<T, Int32> func)
        {
            if (Count < 1) return 0;

            var count = 0;
            if (useTransition)
            {
                using (var trans = Factory.CreateTrans())
                {
                    for (int i = 0; i < Count; i++)
                    {
                        count += func(this[i]);
                    }

                    trans.Commit();
                }
            }
            else
            {
                for (int i = 0; i < Count; i++)
                {
                    count += func(this[i]);
                }
            }

            return count;
        }

        /// <summary>设置所有实体中指定项的值</summary>
        /// <param name="name">指定项的名称</param>
        /// <param name="value">数值</param>
        public EntityList<T> SetItem(String name, Object value)
        {
            if (Count < 1) return this;

            ForEach(e => { if (e != null && !Object.Equals(e[name], value)) e.SetItem(name, value); });

            return this;
        }

        /// <summary>获取所有实体中指定项的值，不允许重复项。无数据时返回空列表而不是null</summary>
        /// <typeparam name="TResult">指定项的类型</typeparam>
        /// <param name="name">指定项的名称</param>
        /// <returns></returns>
        public List<TResult> GetItem<TResult>(String name) { return GetItem<TResult>(name, false); }

        /// <summary>获取所有实体中指定项的值。无数据时返回空列表而不是null</summary>
        /// <remarks>
        /// 有时候只是为了获取某个属性值的集合，可以允许重复项，而有时候是获取唯一主键，作为in操作的参数，不该允许重复项。
        /// </remarks>
        /// <typeparam name="TResult">指定项的类型</typeparam>
        /// <param name="name">指定项的名称</param>
        /// <param name="allowRepeated">是否允许重复项</param>
        /// <returns></returns>
        public List<TResult> GetItem<TResult>(String name, Boolean allowRepeated)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            var list = new List<TResult>();
            if (Count < 1) return list;

            var type = typeof(TResult);
            for (int i = 0; i < Count; i++)
            {
                var item = this[i];
                if (item == null) continue;

                // 避免集合插入了重复项
                var obj = item[name].ChangeType<TResult>();
                if (allowRepeated || !list.Contains(obj)) list.Add(obj);
            }
            return list;
        }

        /// <summary>串联指定成员，方便由实体集合构造用于查询的子字符串</summary>
        /// <param name="name">名称</param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public String Join(String name, String separator)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            if (Count < 1) return null;

            var list = GetItem<String>(name);
            if (list == null || list.Count < 1) return null;

            return String.Join(separator, list.ToArray());
        }

        /// <summary>串联</summary>
        /// <param name="separator"></param>
        /// <returns></returns>
        public String Join(String separator)
        {
            if (Count < 1) return null;

            var sb = new StringBuilder(Count * 10);
            for (int i = 0; i < Count; i++)
            {
                if (sb.Length > 0) sb.Append(separator);
                sb.Append("" + this[i]);
            }
            return sb.ToString();
        }
        #endregion

        #region 排序
        /// <summary>按指定字段排序</summary>
        /// <param name="name">字段</param>
        /// <param name="isDesc">是否降序</param>
        public EntityList<T> Sort(String name, Boolean isDesc)
        {
            if (Count < 1) return this;

            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            var type = GetItemType(name);
            if (type == null) throw new ArgumentNullException("name", "无法找到字段" + name + "的类型");
            if (!typeof(IComparable).IsAssignableFrom(type)) throw new NotSupportedException(String.Format("排序字段{0}的类型{1}不支持比较！", name, type.FullName));

            var n = 1;
            if (isDesc) n = -1;

            Sort((item1, item2) =>
            {
                // 特殊情况下出现列表元素为空，然后下面比较的时候出错，这里特殊处理一下
                if (item1 == null && item2 == null) return 0;
                if (item1 == null && item2 != null) return -n;   // 把空节点排到前面
                if (item1 != null && item2 == null) return n;

                // Object.Equals可以有效的处理两个元素都为空的问题
                if (Object.Equals(item1[name], item2[name])) return 0;
                // 如果为空，或者不是比较类型，则返回-1，说明小于
                if (item1[name] == null || !(item1[name] is IComparable)) return -1;
                return (item1[name] as IComparable).CompareTo(item2[name]) * n;
            });

            return this;
        }

        /// <summary>按指定字段数组排序</summary>
        /// <param name="names">字段</param>
        /// <param name="isDescs">是否降序</param>
        public EntityList<T> Sort(String[] names, Boolean[] isDescs)
        {
            if (Count < 1) return this;

            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var isDesc = isDescs[i];

                var type = GetItemType(name);
                if (!typeof(IComparable).IsAssignableFrom(type)) throw new NotSupportedException("不支持比较！");
            }

            Sort((item1, item2) =>
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

            return this;
        }

        Type GetItemType(String name)
        {
            if (String.IsNullOrEmpty(name) || EntityType == null) return null;

            var pi = EntityType.GetPropertyEx(name);
            if (pi != null) return pi.PropertyType;

            var fi = EntityType.GetFieldEx(name);
            if (fi != null) return fi.FieldType;

            return null;
        }

        /// <summary>按指定字段排序</summary>
        /// <param name="name">字段</param>
        /// <param name="isDesc">是否降序</param>
        IEntityList IEntityList.Sort(String name, Boolean isDesc) { return Sort(name, isDesc); }

        /// <summary>按指定字段数组排序</summary>
        /// <param name="names">字段</param>
        /// <param name="isDescs">是否降序</param>
        IEntityList IEntityList.Sort(String[] names, Boolean[] isDescs) { return Sort(names, isDescs); }

        /// <summary>提升指定实体在当前列表中的位置，加大排序键的值</summary>
        /// <param name="entity"></param>
        /// <param name="sortKey"></param>
        /// <returns></returns>
        public EntityList<T> Up(T entity, String sortKey)
        {
            if (Count < 1) return this;
            if (entity == null) throw new ArgumentNullException("entity");

            if (String.IsNullOrEmpty(sortKey) && Factory.FieldNames.Contains("Sort")) sortKey = "Sort";
            if (String.IsNullOrEmpty(sortKey)) throw new ArgumentNullException("sortKey");

            // 要先排序
            Sort(sortKey, true);

            for (int i = 0; i < Count; i++)
            {
                Int32 s = Count - i;
                // 当前项，排序增加。原来比较实体相等有问题，也许新旧实体类不对应，现在改为比较主键值
                if (i > 0 && entity.EqualTo(this[i])) s++;
                // 下一项是当前项，排序减少
                if (i < Count - 1 && entity.EqualTo(this[i + 1])) s--;
                if (s > Count) s = Count;
                this[i].SetItem(sortKey, s);
            }
            Save(true);

            return this;
        }

        /// <summary>降低指定实体在当前列表中的位置，减少排序键的值</summary>
        /// <param name="entity"></param>
        /// <param name="sortKey"></param>
        /// <returns></returns>
        public EntityList<T> Down(T entity, String sortKey)
        {
            if (Count < 1) return this;
            if (entity == null) throw new ArgumentNullException("entity");

            if (String.IsNullOrEmpty(sortKey) && Factory.FieldNames.Contains("Sort")) sortKey = "Sort";
            if (String.IsNullOrEmpty(sortKey)) throw new ArgumentNullException("sortKey");

            // 要先排序
            Sort(sortKey, true);

            for (int i = 0; i < Count; i++)
            {
                Int32 s = Count - i;
                // 当前项，排序减少
                if (entity.EqualTo(this[i])) s--;
                // 上一项是当前项，排序增加
                if (i >= 1 && entity.EqualTo(this[i - 1])) s++;
                if (s < 1) s = 1;
                this[i].SetItem(sortKey, s);
            }
            Save(true);

            return this;
        }

        /// <summary>提升指定实体在当前列表中的位置，加大排序键的值</summary>
        /// <param name="entity"></param>
        /// <param name="sortKey"></param>
        /// <returns></returns>
        IEntityList IEntityList.Up(IEntity entity, String sortKey) { return Up((T)entity, sortKey); }

        /// <summary>降低指定实体在当前列表中的位置，减少排序键的值</summary>
        /// <param name="entity"></param>
        /// <param name="sortKey"></param>
        /// <returns></returns>
        IEntityList IEntityList.Down(IEntity entity, String sortKey) { return Down((T)entity, sortKey); }
        #endregion

        #region 导入导出
        /// <summary>导出</summary>
        /// <param name="writer"></param>
        public virtual void Export(TextWriter writer)
        {
            IList list = this;
            if (typeof(T).IsInterface) list = (this as IListSource).GetList();

            var serial = CreateXmlSerializer(list.GetType());
            serial.Serialize(writer, list);
        }

        /// <summary>导入</summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public virtual void Import(TextReader reader)
        {
            var serial = CreateXmlSerializer(this.GetType());
            var list = serial.Deserialize(reader) as EntityList<T>;

            AddRange(list);
        }

        private XmlSerializer CreateXmlSerializer(Type type)
        {
            var ovs = new XmlAttributeOverrides();
            var factory = Factory;
            var entity = factory.Create();
            foreach (var item in factory.Fields)
            {
                var atts = new XmlAttributes();
                atts.XmlAttribute = new XmlAttributeAttribute();
                atts.XmlDefaultValue = entity[item.Name];
                ovs.Add(item.DeclaringType, item.Name, atts);
            }
            return new XmlSerializer(type, ovs);
        }

        /// <summary>导出Xml文本</summary>
        /// <returns></returns>
        public virtual String ToXml()
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream, Encoding.UTF8);
                Export(writer);
                var bts = stream.ToArray();
                var xml = Encoding.UTF8.GetString(bts);
                writer.Close();
                if (!String.IsNullOrEmpty(xml)) xml = xml.Trim();
                return xml;
            }
        }

        /// <summary>导入Xml文本</summary>
        /// <param name="xml"></param>
        public virtual EntityList<T> FromXml(String xml)
        {
            if (String.IsNullOrEmpty(xml)) return this;
            xml = xml.Trim();
            using (var reader = new StringReader(xml))
            {
                Import(reader);
            }

            return this;
        }

        /// <summary>导出Json</summary>
        /// <returns></returns>
        public virtual String ToJson()
        {
            return new Json().Serialize(this);
        }

        /// <summary>导入Json</summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static EntityList<T> FromJson(String json)
        {
            return new Json().Deserialize<EntityList<T>>(json);
        }
        #endregion

        #region 导出DataSet数据集
        /// <summary>转为DataTable</summary>
        /// <param name="allowUpdate">是否允许更新数据，如果允许，将可以对DataTable进行添删改等操作</param>
        /// <returns></returns>
        public DataTable ToDataTable(Boolean allowUpdate = true)
        {
            var dt = new DataTable();
            foreach (var item in Factory.Fields)
            {
                var dc = new DataColumn();
                dc.ColumnName = item.Name;
                dc.DataType = item.Type;
                dc.Caption = item.Description;
                dc.AutoIncrement = item.IsIdentity;

                // 关闭这两项，让DataTable宽松一点
                //dc.Unique = item.PrimaryKey;
                //dc.AllowDBNull = item.IsNullable;

                //if (!item.DataObjectField.IsIdentity) dc.DefaultValue = item.Column.DefaultValue;
                dt.Columns.Add(dc);
            }
            // 判断是否有数据，即使没有数据，也需要创建一个空格DataTable
            if (Count > 0)
            {
                for (int i = 0; i < Count; i++)
                {
                    var entity = this[i];
                    var dr = dt.NewRow();
                    foreach (var item in Factory.Fields)
                    {
                        dr[item.Name] = entity[item.Name];
                    }
                    dt.Rows.Add(dr);
                }
            }

            // 如果允许更新数据，那么绑定三个事件，委托到实体类的更新操作
            if (allowUpdate)
            {
                dt.RowChanging += dt_RowChanging;
                dt.RowDeleting += dt_RowDeleting;
                dt.TableNewRow += dt_TableNewRow;
            }

            return dt;
        }

        void dt_TableNewRow(object sender, DataTableNewRowEventArgs e)
        {
            var entity = Factory.FindByKeyForEdit(null);
            var dr = e.Row;
            foreach (var item in Factory.Fields)
            {
                dr[item.Name] = entity[item.Name];
            }
        }

        void dt_RowChanging(object sender, DataRowChangeEventArgs e)
        {
            var entity = Factory.Create();
            var dr = e.Row;
            foreach (var item in Factory.Fields)
            {
                //entity[item.Name] = dr[item.Name];
                // 影响脏数据
                entity.SetItem(item.Name, dr[item.Name]);
            }

            if (e.Action == DataRowAction.Add)
                entity.Insert();
            else if (e.Action == DataRowAction.Change)
                entity.Update();
            else
            {
                // 不支持
            }
        }

        void dt_RowDeleting(object sender, DataRowChangeEventArgs e)
        {
            var entity = Factory.Create();
            var dr = e.Row;
            foreach (var item in Factory.Fields)
            {
                entity[item.Name] = dr[item.Name];
            }

            entity.Delete();
        }

        /// <summary>转为DataSet</summary>
        /// <returns></returns>
        public DataSet ToDataSet()
        {
            var ds = new DataSet();
            ds.Tables.Add(ToDataTable());
            return ds;
        }
        #endregion

        #region 转换
        /// <summary>转为泛型List，方便进行Linq</summary>
        /// <returns></returns>
        public List<T> ToList() { return this; }

        /// <summary>实体列表转为字典。主键为Key</summary>
        /// <param name="valueField">作为Value部分的字段，默认为空表示整个实体对象为值</param>
        /// <returns></returns>
        public IDictionary ToDictionary(String valueField = null)
        {
            // 构造主键类型和值类型
            var key = Factory.Unique;
            var ktype = key.Type;

            var vtype = EntityType;
            if (!valueField.IsNullOrEmpty())
            {
                var fi = Factory.Table.FindByName(valueField) as FieldItem;
                if (fi == null) throw new XException("无法找到名为{0}的字段", valueField);

                vtype = fi.Type;
            }

            // 创建字典
            var dic = typeof(Dictionary<,>).MakeGenericType(ktype, vtype).CreateInstance() as IDictionary;
            foreach (var item in this)
            {
                if (!valueField.IsNullOrEmpty())
                    dic.Add(item[key.Name], item[valueField]);
                else
                    dic.Add(item[key.Name], item);
            }

            return dic;
        }

        /// <summary>任意集合转为实体集合</summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static EntityList<T> From(IEnumerable collection) { return new EntityList<T>(collection); }

        /// <summary>拥有指定类型转换器的转换</summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="collection"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static EntityList<T> From<T2>(IEnumerable collection, Func<T2, T> func)
        {
            var list = new EntityList<T>();
            if (collection == null) return list;
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
            return list;
        }
        #endregion

        #region IListSource接口
        bool IListSource.ContainsListCollection { get { return Count > 0; } }

        IList IListSource.GetList()
        {
            // 如果是接口，创建新的集合，否则返回自身
            if (!typeof(T).IsInterface) return this;

            // 支持空列表
            // 元素类型
            var type = EntityType;
            // 泛型
            type = typeof(EntityListView<>).MakeGenericType(type);

            // 直接复制集合更快
            var list = new EntityList<T>(ToArray());
            return type.CreateInstance(list) as IList;
        }
        #endregion

        #region 辅助函数
        private static EntityList<T> _Empty;
        /// <summary>空集合</summary>
        [Obsolete("该属性容易让人误操作，比如给空列表加入元素！")]
        public static EntityList<T> Empty { get { return _Empty ?? (_Empty = new EntityList<T>()); } }

        /// <summary>真正的实体类型。有些场合为了需要会使用IEntity。</summary>
        Type EntityType
        {
            get
            {
                var type = typeof(T);
                if (!type.IsInterface) return type;

                if (Count > 0) return this[0].GetType();

                return type;
            }
        }

        /// <summary>实体操作者</summary>
        IEntityOperate Factory
        {
            get
            {
                var type = EntityType;
                if (type.IsInterface) return null;

                return EntityFactory.CreateOperate(type);
            }
        }
        #endregion

        #region IList<IEntity> 成员
        private static bool IsCompatibleObject(IEntity value)
        {
            if (!(value is T) && value != null || typeof(T).IsValueType) return false;
            return true;
        }

        private static void VerifyValueType(IEntity value)
        {
            if (!IsCompatibleObject(value)) throw new ArgumentException(String.Format("期待{0}类型的参数！", typeof(T).Name), "value");
        }

        int IList<IEntity>.IndexOf(IEntity item)
        {
            if (!IsCompatibleObject(item)) return -1;
            return IndexOf((T)item);
        }

        void IList<IEntity>.Insert(int index, IEntity item)
        {
            VerifyValueType(item);
            Insert(index, (T)item);
        }

        IEntity IList<IEntity>.this[int index] { get { return this[index]; } set { VerifyValueType(value); this[index] = (T)value; } }
        #endregion

        #region ICollection<IEntity> 成员

        void ICollection<IEntity>.Add(IEntity item)
        {
            VerifyValueType(item);
            Add((T)item);
        }

        bool ICollection<IEntity>.Contains(IEntity item)
        {
            if (!IsCompatibleObject(item)) return false;

            return Contains((T)item);
        }

        void ICollection<IEntity>.CopyTo(IEntity[] array, int arrayIndex)
        {
            if (array == null || array.Length == 0) return;

            VerifyValueType(array[0]);
            var arr = new T[array.Length];
            CopyTo(arr, arrayIndex);
            for (int i = arrayIndex; i < array.Length; i++)
            {
                array[i] = arr[i];
            }
        }

        bool ICollection<IEntity>.IsReadOnly { get { return (this as ICollection<T>).IsReadOnly; } }

        bool ICollection<IEntity>.Remove(IEntity item)
        {
            if (!IsCompatibleObject(item)) return false;

            return Remove((T)item);
        }
        #endregion

        #region IEnumerable<IEntity> 成员
        IEnumerator<IEntity> IEnumerable<IEntity>.GetEnumerator() { for (int i = 0; i < Count; i++) yield return this[i]; }
        #endregion

        #region 克隆接口
        /// <summary>把当前列表的元素复制到新列表里面去</summary>
        /// <remarks>其实直接new一个新的列表就好了，但是做克隆方法更方便链式写法</remarks>
        /// <returns></returns>
        public EntityList<T> Clone() { return new EntityList<T>(this); }

        object ICloneable.Clone() { return Clone(); }
        #endregion
    }
}