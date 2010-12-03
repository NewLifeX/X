using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 创建实例触发事件参数
    /// </summary>
    public class ProtocolCreateInstanceEventArgs : EventArgs
    {
        #region 属性
        private ReadContext _Context;
        /// <summary>上下文</summary>
        public ReadContext Context
        {
            get { return _Context; }
            set { _Context = value; }
        }

        private Type _Type;
        /// <summary>要创建实例的类型</summary>
        public Type Type
        {
            get { return _Type; }
            set { _Type = value; }
        }

        private Object _Obj;
        /// <summary>创建的对象</summary>
        public Object Obj
        {
            get { return _Obj; }
            set { _Obj = value; }
        }
        #endregion
    }
}
