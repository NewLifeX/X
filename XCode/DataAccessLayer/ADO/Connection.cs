using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace XCode.DataAccessLayer.ADO
{
    public class Connection : Base
    {
        #region 属性
        public override string TypeName
        {
            get { return "ADODB.Connection"; }
        }

        private String _ConnectionString;
        /// <summary>连接字符串</summary>
        public String ConnectionString
        {
            get { return _ConnectionString; }
            set { _ConnectionString = value; }
        }

        #endregion

        #region 方法
        public void Open()
        {
            //Type.InvokeMember("Open", BindingFlags.Public | BindingFlags.Instance, null, Obj, new Object[] { ConnectionString, null, null, -1 });
            InvokeMethod("Open", new Object[] { ConnectionString, null, null, -1 });
        }

        public void Close()
        {
            InvokeMethod("Close", null);
        }
        #endregion
    }
}
