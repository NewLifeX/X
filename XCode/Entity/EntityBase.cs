using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Reflection;
using XCode.Common;
using XCode.Configuration;
using XCode.Model;

namespace XCode
{
    /// <summary>数据实体基类的基类</summary>
    [Serializable]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract partial class EntityBase : IEntity, ICloneable
    {
        #region 初始化数据
        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal protected virtual void InitData() { }
        #endregion

        #region 填充数据
        /// <summary>填充数据完成时调用。默认设定标记<see cref="IsFromDatabase"/></summary>
        protected virtual void OnLoad() => IsFromDatabase = true;

        /// <summary>是否来自数据库。设置相同属性值时不改变脏数据</summary>
        protected virtual Boolean IsFromDatabase { get; set; }

        /// <summary>是否来自数据库。设置相同属性值时不改变脏数据</summary>
        Boolean IEntity.IsFromDatabase => IsFromDatabase;
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

        /// <summary>异步保存。实现延迟保存，大事务保存。主要面向日志表和频繁更新的在线记录表</summary>
        /// <param name="msDelay">延迟保存的时间。默认0ms近实时保存</param>
        /// <returns>是否成功加入异步队列</returns>
        public abstract Boolean SaveAsync(Int32 msDelay = 0);

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <remarks>建议重写者调用基类的实现，因为基类根据数据字段的唯一索引进行数据验证。</remarks>
        /// <param name="isNew">是否新数据</param>
        public abstract void Valid(Boolean isNew);
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public abstract Object this[String name] { get; set; }

        /// <summary>设置字段值，该方法影响脏数据。</summary>
        /// <param name="name">字段名</param>
        /// <param name="value">值</param>
        /// <returns>返回是否成功设置了数据</returns>
        public Boolean SetItem(String name, Object value)
        {
            var fact = EntityFactory.CreateOperate(GetType());
            FieldItem fi = fact.Table.FindByName(name);
            // 确保数据类型一致
            if (fi != null) value = value.ChangeType(fi.Type);

            var b = OnPropertyChanging(name, value);
            if (b)
            {
                // OnPropertyChanging中根据新旧值是否相同来影响脏数据
                // SetItem作为必定影响脏数据的代替者
                this[name] = value;
                Dirtys[name] = true;
            }
            return b;
        }

        ///// <summary>设置脏数据项。如果某个键存在并且数据没有脏，则设置</summary>
        ///// <param name="name"></param>
        ///// <param name="value"></param>
        ///// <returns>返回是否成功设置了数据</returns>
        //public Boolean SetNoDirtyItem(String name, Object value)
        //{
        //    var fact = EntityFactory.CreateOperate(GetType());
        //    if (fact.FieldNames.Contains(name) && !Dirtys[name]) return SetItem(name, value);

        //    return false;
        //}
        #endregion

        #region 克隆
        /// <summary>创建当前对象的克隆对象，仅拷贝基本字段</summary>
        /// <returns></returns>
        public abstract Object Clone();

        /// <summary>克隆实体。创建当前对象的克隆对象，仅拷贝基本字段</summary>
        /// <param name="setDirty">是否设置脏数据</param>
        /// <returns></returns>
        IEntity IEntity.CloneEntity(Boolean setDirty) => CloneEntityInternal(setDirty);

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
            if (entity == this) return 0;

            IEntity src = this;
            var nsSrc = EntityFactory.CreateOperate(src.GetType()).FieldNames;
            //if (nsSrc == null || nsSrc.Count < 1) return 0;
            var nsDes = EntityFactory.CreateOperate(entity.GetType()).FieldNames;
            if (nsDes == null || nsDes.Count < 1) return 0;

            var n = 0;
            foreach (var item in nsDes)
            {
                if (nsSrc.Contains(item))
                {
                    if (setDirty)
                        src.SetItem(item, entity[item]);
                    else
                        src[item] = entity[item];
                }
                else
                {
                    // 如果没有该字段，则写入到扩展属性里面去
                    src.Extends[item] = entity[item];
                    if (setDirty) Dirtys[item] = true;
                }

                n++;
            }
            // 赋值扩展数据
            entity.Extends.CopyTo(src.Extends);

            return n;
        }
        #endregion

        #region 脏数据
        [NonSerialized]
        private DirtyCollection _Dirtys;
        /// <summary>脏属性。存储哪些属性的数据被修改过了。</summary>
        [XmlIgnore, ScriptIgnore]
        protected DirtyCollection Dirtys
        {
            get
            {
                if (_Dirtys == null) _Dirtys = new DirtyCollection();
                return _Dirtys;
            }
        }

        /// <summary>脏属性。存储哪些属性的数据被修改过了。</summary>
        DirtyCollection IEntity.Dirtys => Dirtys;

        /// <summary>是否有脏数据。被修改为不同值</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected Boolean IsDirty(String name) => _Dirtys != null && _Dirtys[name];

        /// <summary>是否有脏数据。被修改为不同值</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Boolean IEntity.IsDirty(String name) => _Dirtys != null && _Dirtys[name];

        /// <summary>是否有脏数据。决定是否可以Update</summary>
        protected Boolean HasDirty => _Dirtys != null && _Dirtys.Count > 0;

        /// <summary>是否有脏数据。决定是否可以Update</summary>
        Boolean IEntity.HasDirty => _Dirtys != null && _Dirtys.Count > 0;
        #endregion

        #region 扩展属性
        [NonSerialized]
        internal EntityExtend _Extends;
        /// <summary>扩展属性</summary>
        //[NonSerialized]
        [XmlIgnore, ScriptIgnore]
        public EntityExtend Extends { get { return _Extends ?? (_Extends = new EntityExtend()); } set { _Extends = value; } }
        #endregion

        #region 累加
        [NonSerialized]
        private IEntityAddition _Addition;
        /// <summary>累加</summary>
        [XmlIgnore, ScriptIgnore]
        internal IEntityAddition Addition
        {
            get
            {
                if (_Addition == null)
                {
                    _Addition = XCodeService.Container.Resolve<IEntityAddition>();
                    _Addition.Entity = this;
                }
                return _Addition;
            }
        }

        internal IDictionary<String, Object[]> GetAddition() => _Addition?.Get();
        #endregion

        #region 主键为空
        /// <summary>主键是否为空</summary>
        [XmlIgnore, ScriptIgnore]
        Boolean IEntity.IsNullKey => Helper.IsEntityNullKey(this);

        /// <summary>主键是否为空</summary>
        protected Boolean IsNullKey => Helper.IsEntityNullKey(this);

        /// <summary>设置主键为空。Save将调用Insert</summary>
        void IEntity.SetNullKey()
        {
            var eop = EntityFactory.CreateOperate(GetType());
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
            if (entity == null || GetType() != entity.GetType()) return false;
            if (this == entity) return true;

            // 判断是否所有主键相等
            var op = EntityFactory.CreateOperate(GetType());
            var ps = op.Table.PrimaryKeys;
            // 如果没有主键，则判断所有字段
            if (ps == null || ps.Length < 1) ps = op.Table.Fields;
            foreach (var item in ps)
            {
                var v1 = this[item.Name];
                var v2 = entity[item.Name];
                //// 特殊处理整数类型，避免出现相同值不同整型而导致结果不同
                //if (item.Type.IsIntType() && Convert.ToInt64(v1) != Convert.ToInt64(v2)) return false;

                //if (item.Type == typeof(String)) { v1 += ""; v2 += ""; }

                //if (!Object.Equals(v1, v2)) return false;
                if (!CheckEqual(v1, v2)) return false;
            }

            return true;
        }
        #endregion
    }
}