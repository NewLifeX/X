using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace XCode.DataAccessLayer.ADO
{
    public abstract class Base
    {
        #region 属性
        /// <summary>类型</summary>
        public abstract String TypeName { get; }

        private Type _Type;
        public Type Type
        {
            get
            {
                if (_Type == null) _Type = Type.GetTypeFromProgID(TypeName);
                return _Type;
            }
        }

        private Object _Obj;
        public Object Obj
        {
            get
            {
                if (_Obj == null) _Obj = Activator.CreateInstance(Type);
                return _Obj;
            }
        }
        #endregion

        #region 方法
        public Object InvokeMethod(String name, Object[] ps)
        {
            return Type.InvokeMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, Obj, ps);
        }

        public Object SetProperty(String name, Object[] ps)
        {
            return Type.InvokeMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty, null, Obj, ps);
        }

        public void SetProperty(String name, Object value)
        {
            SetProperty(name, new Object[] { value });
        }

        public Object GetProperty(String name, Object[] ps)
        {
            return Type.InvokeMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty, null, Obj, ps);
        }

        public Object GetProperty(String name)
        {
            return GetProperty(name, null);
        }
        #endregion
    }
}
