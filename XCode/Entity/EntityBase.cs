using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.IO;
using NewLife.Reflection;
using XCode.Common;
using NewLife.Xml;

namespace XCode
{
    /// <summary>数据实体基类的基类</summary>
    [Serializable]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract partial class EntityBase : /*BinaryAccessor,*/ IEntity, ICloneable
    {
        #region 初始化数据
        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal protected virtual void InitData() { }
        #endregion

        #region 填充数据
        /// <summary>从一个数据行对象加载数据。不加载关联对象。</summary>
        /// <param name="dr">数据行</param>
        public abstract void LoadData(DataRow dr);

        /// <summary>从一个数据行对象加载数据。不加载关联对象。</summary>
        /// <param name="dr">数据读写器</param>
        public abstract void LoadDataReader(IDataReader dr);

        /// <summary>填充数据完成时调用。默认设定标记<see cref="_IsFromDatabase"/></summary>
        internal protected virtual void OnLoad() { _IsFromDatabase = true; }
        #endregion

        #region 操作
        /// <summary>把该对象持久化到数据库</summary>
        /// <returns></returns>
        public abstract Int32 Insert();

        /// <summary>更新数据库</summary>
        /// <returns></returns>
        public abstract Int32 Update();

        /// <summary>从数据库中删除该对象</summary>
        /// <returns></returns>
        public abstract Int32 Delete();

        /// <summary>保存。根据主键检查数据库中是否已存在该对象，再决定调用Insert或Update</summary>
        /// <returns></returns>
        public abstract Int32 Save();

        /// <summary>不需要验证的保存</summary>
        /// <returns></returns>
        public abstract Int32 SaveWithoutValid();
        #endregion

        #region 获取/设置 字段值
        /// <summary>
        /// 获取/设置 字段值。
        /// 一个索引，反射实现。
        /// 派生实体类可重写该索引，以避免发射带来的性能损耗。
        /// 基类已经实现了通用的快速访问，但是这里仍然重写，以增加控制，
        /// 比如字段名是属性名前面加上_，并且要求是实体字段才允许这样访问，否则一律按属性处理。
        /// </summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public abstract Object this[String name] { get; set; }

        /// <summary>设置字段值，该方法影响脏数据。</summary>
        /// <param name="name">字段名</param>
        /// <param name="value">值</param>
        /// <returns>返回是否成功设置了数据</returns>
        public Boolean SetItem(String name, Object value)
        {
            Boolean b = OnPropertyChanging(name, value);
            if (b)
            {
                // OnPropertyChanging中根据新旧值是否相同来影响脏数据
                // SetItem作为必定影响脏数据的代替者
                this[name] = value;
                Dirtys[name] = true;
            }
            return b;
        }
        #endregion

        #region 导入导出XML
        /// <summary>导出XML</summary>
        /// <returns></returns>
        public virtual String ToXml() { return this.ToXml(Encoding.UTF8, "", ""); }
        #endregion

        #region 导入导出Json
        /// <summary>导出Json</summary>
        /// <returns></returns>
        [Obsolete("该成员在后续版本中将不再被支持！")]
        public virtual String ToJson()
        {
            Json json = new Json();
            return json.Serialize(this);
        }
        #endregion

        #region 克隆
        /// <summary>创建当前对象的克隆对象，仅拷贝基本字段</summary>
        /// <returns></returns>
        public abstract Object Clone();

        /// <summary>克隆实体。创建当前对象的克隆对象，仅拷贝基本字段</summary>
        /// <param name="setDirty">是否设置脏数据</param>
        /// <returns></returns>
        IEntity IEntity.CloneEntity(Boolean setDirty) { return CloneEntityInternal(setDirty); }

        /// <summary>克隆实体</summary>
        /// <param name="setDirty"></param>
        /// <returns></returns>
        internal protected abstract IEntity CloneEntityInternal(Boolean setDirty = true);

        /// <summary>复制来自指定实体的成员，可以是不同类型的实体，只复制共有的基本字段，影响脏数据</summary>
        /// <param name="entity">来源实体对象</param>
        /// <param name="setDirty">是否设置脏数据</param>
        /// <returns>实际复制成员数</returns>
        public virtual Int32 CopyFrom(IEntity entity, Boolean setDirty = true)
        {
            IEntity src = this;
            var names1 = EntityFactory.CreateOperate(src.GetType()).FieldNames;
            if (names1 == null || names1.Count < 1) return 0;
            var names2 = EntityFactory.CreateOperate(entity.GetType()).FieldNames;
            if (names2 == null || names2.Count < 1) return 0;

            Int32 n = 0;
            foreach (var item in names1)
            {
                if (names2.Contains(item))
                {
                    if (setDirty)
                        src.SetItem(item, entity[item]);
                    else
                        src[item] = entity[item];

                    n++;
                }
            }
            // 赋值扩展数据
            if (entity.Extends != null)
            {
                foreach (var item in entity.Extends)
                {
                    src.Extends[item.Key] = item.Value;
                    if (setDirty) Dirtys[item.Key] = true;

                    n++;
                }
            }
            return n;
        }
        #endregion

        #region 脏数据
        [NonSerialized]
        private DirtyCollection _Dirtys;
        /// <summary>脏属性。存储哪些属性的数据被修改过了。</summary>
        [XmlIgnore]
        internal protected IDictionary<String, Boolean> Dirtys
        {
            get
            {
                if (_Dirtys == null) _Dirtys = new DirtyCollection();
                return _Dirtys;
            }
            //set { _Dirtys = value; }
        }

        /// <summary>脏属性。存储哪些属性的数据被修改过了。</summary>
        IDictionary<String, Boolean> IEntity.Dirtys { get { return Dirtys; } }

        /// <summary>设置所有数据的脏属性</summary>
        /// <param name="isDirty">改变脏属性的属性个数</param>
        /// <returns></returns>
        protected virtual Int32 SetDirty(Boolean isDirty)
        {
            var ds = _Dirtys;
            if (ds == null || ds.Count < 1) return 0;

            Int32 count = 0;
            foreach (String item in ds.Keys)
            {
                if (ds[item] != isDirty)
                {
                    ds[item] = isDirty;
                    count++;
                }
            }
            return count;
        }

        /// <summary>属性改变。重载时记得调用基类的该方法，以设置脏数据属性，否则数据将无法Update到数据库。</summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="newValue">新属性值</param>
        /// <returns>是否允许改变</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("改为使用OnPropertyChanging")]
        protected virtual Boolean OnPropertyChange(String fieldName, Object newValue)
        {
            //if (_PropertyChanging != null) _PropertyChanging(this, new PropertyChangingEventArgs(fieldName));
            //// 如果数据没有改变，不应该影响脏数据
            ////Dirtys[fieldName] = true;
            //if (!Object.Equals(this[fieldName], newValue)) Dirtys[fieldName] = true;
            //return true;

            return OnPropertyChanging(fieldName, newValue);
        }
        #endregion

        #region 扩展属性
        private EntityExtend _Extends;
        /// <summary>扩展属性</summary>
        public EntityExtend Extends { get { return _Extends ?? (_Extends = new EntityExtend()); } set { _Extends = value; } }

        /// <summary>扩展属性</summary>
        IDictionary<String, Object> IEntity.Extends { get { return Extends; } }
        #endregion

        #region 累加
        [NonSerialized]
        private IDictionary<String, Object> _Additions;

        /// <summary>设置累加字段。如果是第一次设置该字段，则保存该字段当前数据作为累加基础数据</summary>
        /// <param name="name">字段名称</param>
        /// <param name="reset">是否重置。可以保存当前数据作为累加基础数据</param>
        /// <returns>是否成功设置累加字段。如果不是第一次设置，并且没有重置数据，那么返回失败</returns>
        public Boolean SetAdditionalField(String name, Boolean reset = false)
        {
            // 检查集合是否为空
            if (_Additions == null)
            {
                //_Additions = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
                _Additions = new Dictionary<String, Object>();
            }

            lock (_Additions)
            {
                if (reset || !_Additions.ContainsKey(name))
                {
                    _Additions[name] = this[name];
                    return true;
                }
                else
                    return false;
            }
        }

        /// <summary>删除累加字段。</summary>
        /// <param name="name">字段名称</param>
        /// <param name="restore">是否恢复数据</param>
        /// <returns>是否成功删除累加字段</returns>
        public Boolean RemoveAdditionalField(String name, Boolean restore = false)
        {
            if (_Additions == null) return false;

            Object obj = null;
            if (!_Additions.TryGetValue(name, out obj)) return false;

            if (restore) this[name] = obj;

            return true;
        }

        /// <summary>尝试获取累加数据</summary>
        /// <param name="name">字段名称</param>
        /// <param name="value">累加数据绝对值</param>
        /// <param name="sign">正负</param>
        /// <returns>是否获取指定字段的累加数据</returns>
        public Boolean TryGetAdditionalValue(String name, out Object value, out Boolean sign)
        {
            value = null;
            sign = true;
            if (_Additions == null) return false;

            if (!_Additions.TryGetValue(name, out value)) return false;

            // 计算累加数据
            var current = this[name];
            var type = current.GetType();
            var code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Char:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    {
                        var v = Convert.ToInt64(current) - Convert.ToInt64(value);
                        if (v < 0)
                        {
                            v *= -1;
                            sign = false;
                        }
                        //value = Convert.ChangeType(v, type);
                        value = v;
                    }
                    break;
                case TypeCode.Single:
                    {
                        var v = (Single)current - (Single)value;
                        if (v < 0)
                        {
                            v *= -1;
                            sign = false;
                        }
                        value = v;
                    }
                    break;
                case TypeCode.Double:
                    {
                        var v = (Double)current - (Double)value;
                        if (v < 0)
                        {
                            v *= -1;
                            sign = false;
                        }
                        value = v;
                    }
                    break;
                case TypeCode.Decimal:
                    {
                        var v = (Decimal)current - (Decimal)value;
                        if (v < 0)
                        {
                            v *= -1;
                            sign = false;
                        }
                        value = v;
                    }
                    break;
                default:
                    break;
            }

            return true;
        }

        /// <summary>清除累加字段数据。Update后调用该方法</summary>
        public void ClearAdditionalValues()
        {
            if (_Additions == null) return;

            foreach (var item in _Additions.Keys.ToArray())
            {
                _Additions[item] = this[item];
            }
        }
        #endregion

        #region 主键为空
        /// <summary>主键是否为空</summary>
        Boolean IEntity.IsNullKey
        {
            get
            {
                return Helper.IsEntityNullKey(this);
            }
        }

        /// <summary>设置主键为空。Save将调用Insert</summary>
        void IEntity.SetNullKey()
        {
            IEntityOperate eop = EntityFactory.CreateOperate(GetType());
            foreach (var item in eop.Fields)
            {
                if (item.PrimaryKey || item.IsIdentity) this[item.Name] = null;
            }
        }
        #endregion

        #region 实体相等
        /// <summary>判断两个实体是否相等。有可能是同一条数据的两个实体对象</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Boolean IEntity.EqualTo(IEntity entity)
        {
            if (entity == null || this.GetType() != entity.GetType()) return false;
            if (this == entity) return true;

            // 判断是否所有主键相等
            var op = EntityFactory.CreateOperate(this.GetType());
            foreach (var item in op.Table.PrimaryKeys)
            {
                var v1 = this[item.Name];
                var v2 = entity[item.Name];
                // 特殊处理整数类型，避免出现相同值不同整型而导致结果不同
                if (item.Type.IsIntType() && Convert.ToInt64(v1) != Convert.ToInt64(v2)) return false;

                if (item.Type == typeof(String)) { v1 += ""; v2 += ""; }

                if (!Object.Equals(v1, v2)) return false;
            }

            return true;
        }
        #endregion
    }
}