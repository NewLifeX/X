using System;
using System.Collections.Generic;
using System.Text;
using XCode.Configuration;

namespace XCode.Accessors
{
    /// <summary>实体访问器事件参数</summary>
    public class EntityAccessorEventArgs : EventArgs
    {
        #region 属性
        private IEntity _Entity;
        /// <summary>实体对象</summary>
        public IEntity Entity { get { return _Entity; } set { _Entity = value; } }

        private FieldItem _Field;
        /// <summary>字段信息</summary>
        public FieldItem Field { get { return _Field; } set { _Field = value; } }

        private Exception _Error;
        /// <summary>异常对象</summary>
        public Exception Error
        {
            get { return _Error; }
            set { _Error = value; }
        }
        #endregion

        //#region 构造
        //public EntityAccessorEventArgs(IEntity entity, FieldItem field)
        //{
        //    Entity = entity;
        //    Field = field;
        //}
        //#endregion
    }
}