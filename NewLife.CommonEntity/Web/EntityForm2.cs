using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;
using XCode.Accessors;
using XCode;
using System.Linq;
using XCode.Configuration;
using NewLife.Log;
using System.Web;

namespace NewLife.CommonEntity.Web
{
    /// <summary>第二代实体表单</summary>
    /// <remarks>
    /// 作为第二代实体表单，必须解决几个问题：
    /// 1，不能使用泛型；
    /// 2，不能占用页面基类；
    /// </remarks>
    public class EntityForm2
    {
        #region 属性
        private Control _Container;
        /// <summary>容器</summary>
        public Control Container
        {
            get { return _Container; }
            set { _Container = value; }
        }

        private Type _EntityType;
        /// <summary>实体类型</summary>
        public Type EntityType
        {
            get { return _EntityType; }
            set { _EntityType = value; }
        }
        #endregion

        #region 构造
        //public EntityForm2() { }
        /// <summary>
        /// 指定控件容器和实体类型，实例化一个实体表单
        /// </summary>
        /// <param name="container"></param>
        /// <param name="type"></param>
        public EntityForm2(Control container, Type type)
        {
            if (container == null)
            {
                if (HttpContext.Current.Handler is Page) container = HttpContext.Current.Handler as Page;
            }

            Container = container;
            EntityType = type;
        }
        #endregion

        #region 扩展属性
        private IEntityAccessor _Accessor;
        /// <summary>访问器</summary>
        public IEntityAccessor Accessor
        {
            get
            {
                if (_Accessor == null)
                {
                    _Accessor = EntityAccessorFactory.Create(EntityAccessorTypes.WebForm)
                        .SetConfig(EntityAccessorOptions.Container, Container);
                }
                return _Accessor;
            }
            set { _Accessor = value; }
        }

        private IEntityOperate _Factory;
        /// <summary>实体操作者</summary>
        public IEntityOperate Factory
        {
            get { return _Factory; }
            set { _Factory = value; }
        }
        #endregion

        #region 实体相关
        private String _KeyName;
        /// <summary>键名</summary>
        public String KeyName
        {
            get
            {
                if (_KeyName != null) return _KeyName;

                if (Factory.Unique != null)
                    _KeyName = Factory.Unique.Name;
                else
                {
                    FieldItem[] fis = Factory.Fields.Where(f => f.PrimaryKey).ToArray();
                    if (fis != null && fis.Length > 1)
                    {
                        if (XTrace.Debug) XTrace.WriteLine("实体表单默认不支持多主键（实体类{0}），需要手工给Entity赋值！", EntityType.Name);
                    }
                }

                return _KeyName;
            }
            set { _KeyName = value; }
        }

        /// <summary>主键</summary>
        public Object EntityID
        {
            get
            {
                String str = HttpContext.Current.Request[KeyName];
                if (String.IsNullOrEmpty(str)) return null;

                FieldItem fi = Factory.Unique;
                if (fi != null)
                {
                    Type type = Factory.Unique.Type;
                    if (type == typeof(Int32) || type == typeof(Int64))
                    {
                        Int32 id = 0;
                        if (!Int32.TryParse(str, out id)) id = 0;
                        return (Object)id;
                    }
                    else if (type == typeof(String))
                    {
                        return (Object)str;
                    }
                }
                throw new NotSupportedException("仅支持整数和字符串类型！");
            }
        }

        private IEntity _Entity;
        /// <summary>数据实体</summary>
        public virtual IEntity Entity
        {
            get { return _Entity ?? (_Entity = GetEntity()); }
            set { _Entity = value; }
        }

        /// <summary>获取数据实体，允许页面重载改变实体</summary>
        protected virtual IEntity GetEntity()
        {
            return Factory.FindByKeyForEdit(EntityID);
        }
        #endregion
    }
}