using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    class ReaderWriterState
    {
        #region 属性
        private String _Name;
        /// <summary>名称</summary>
        public String Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        private Object _Target;
        /// <summary>目标对象</summary>
        public Object Target
        {
            get { return _Target; }
            set { _Target = value; }
        }

        private Type _Type;
        /// <summary>类型</summary>
        public Type Type
        {
            get { return _Type; }
            set { _Type = value; }
        }

        private IObjectMemberInfo _Member;
        /// <summary>成员</summary>
        public IObjectMemberInfo Member
        {
            get { return _Member; }
            set { _Member = value; }
        }
        #endregion
    }
}