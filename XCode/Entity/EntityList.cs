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
using NewLife.Serialization;
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
    public partial class EntityList<T> : List<T>, IEntityList, IList, IList<IEntity>, IEnumerable, ICloneable where T : IEntity
    {
        #region 构造函数
        /// <summary>构造一个实体对象集合</summary>
        public EntityList() { }

        /// <summary>构造一个实体对象集合</summary>
        /// <param name="collection"></param>
        public EntityList(IEnumerable<T> collection) : base(collection) { }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            return String.Format("EntityList<{0}>[Count={1}]", typeof(T).Name, Count);
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
            if (value != null && value.GetType().IsInt())
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
            for (Int32 i = 0; i < values.Length; i++)
            {
                field = Factory.Table.FindByName(names[i]);
                if (field != null)
                {
                    ss[i] = field.Type == typeof(String);
                    ts[i] = field.Type.IsInt();
                }

                if (values[i] == null) continue;

                // 整型统一转为Int64后再比较，因为即使数值相等，类型不同的对象也是不等的
                ts[i] |= values[i].GetType().IsInt();
                if (ts[i]) vs[i] = Convert.ToInt64(values[i]);

                ss[i] |= values[i].GetType() == typeof(String);
            }

            var list = new EntityList<T>();
            for (Int32 k = 0; k < Count; k++)
            {
                var item = this[k];
                if (item == null) continue;

                var b = true;
                for (Int32 i = 0; i < names.Length; i++)
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
            if (value != null && value.GetType().IsInt())
            {
                // 整型统一转为Int64后再比较，因为即使数值相等，类型不同的对象也是不等的
                var v6 = Convert.ToInt64(value);
                return base.Find(e => e != null && Convert.ToInt64(e[name]) == v6);
            }
            else
            {
                return base.Find(e => e != null && Object.Equals(e[name], value));
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
                    count = DoAction(func, count);

                    trans.Commit();
                }
            }
            else
            {
                count = DoAction(func, count);
            }

            return count;
        }

        private Int32 DoAction(Func<T, Int32> func, Int32 count)
        {
            for (Int32 i = 0; i < Count; i++)
            {
                count += func(this[i]);
            }
            return count;
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
                for (Int32 i = 0; i < Count; i++)
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

        void dt_TableNewRow(Object sender, DataTableNewRowEventArgs e)
        {
            var entity = Factory.FindByKeyForEdit(null);
            var dr = e.Row;
            foreach (var item in Factory.Fields)
            {
                dr[item.Name] = entity[item.Name];
            }
        }

        void dt_RowChanging(Object sender, DataRowChangeEventArgs e)
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

        void dt_RowDeleting(Object sender, DataRowChangeEventArgs e)
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
        #endregion

        #region 辅助函数
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
        private static Boolean IsCompatibleObject(IEntity value)
        {
            if (!(value is T) && value != null || typeof(T).IsValueType) return false;
            return true;
        }

        private static void VerifyValueType(IEntity value)
        {
            if (!IsCompatibleObject(value)) throw new ArgumentException(String.Format("期待{0}类型的参数！", typeof(T).Name), "value");
        }

        Int32 IList<IEntity>.IndexOf(IEntity item)
        {
            if (!IsCompatibleObject(item)) return -1;
            return IndexOf((T)item);
        }

        void IList<IEntity>.Insert(Int32 index, IEntity item)
        {
            VerifyValueType(item);
            Insert(index, (T)item);
        }

        IEntity IList<IEntity>.this[Int32 index] { get { return this[index]; } set { VerifyValueType(value); this[index] = (T)value; } }
        #endregion

        #region ICollection<IEntity> 成员

        void ICollection<IEntity>.Add(IEntity item)
        {
            VerifyValueType(item);
            Add((T)item);
        }

        Boolean ICollection<IEntity>.Contains(IEntity item)
        {
            if (!IsCompatibleObject(item)) return false;

            return Contains((T)item);
        }

        void ICollection<IEntity>.CopyTo(IEntity[] array, Int32 arrayIndex)
        {
            if (array == null || array.Length == 0) return;

            VerifyValueType(array[0]);
            var arr = new T[array.Length];
            CopyTo(arr, arrayIndex);
            for (Int32 i = arrayIndex; i < array.Length; i++)
            {
                array[i] = arr[i];
            }
        }

        Boolean ICollection<IEntity>.IsReadOnly { get { return (this as ICollection<T>).IsReadOnly; } }

        Boolean ICollection<IEntity>.Remove(IEntity item)
        {
            if (!IsCompatibleObject(item)) return false;

            return Remove((T)item);
        }
        #endregion

        #region IEnumerable<IEntity> 成员
        IEnumerator<IEntity> IEnumerable<IEntity>.GetEnumerator() { for (Int32 i = 0; i < Count; i++) yield return this[i]; }
        #endregion

        #region 克隆接口
        /// <summary>把当前列表的元素复制到新列表里面去</summary>
        /// <remarks>其实直接new一个新的列表就好了，但是做克隆方法更方便链式写法</remarks>
        /// <returns></returns>
        public EntityList<T> Clone() { return new EntityList<T>(this); }

        Object ICloneable.Clone() { return Clone(); }
        #endregion
    }
}