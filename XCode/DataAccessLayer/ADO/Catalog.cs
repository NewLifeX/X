using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer.ADO
{
    public class Catalog : Base
    {
        #region 属性
        public override string TypeName
        {
            get { return "ADOX.Catalog"; }
        }

        private Object _ActiveConnection;
        /// <summary>活动链接</summary>
        public Object ActiveConnection
        {
            get { return GetProperty("ActiveConnection"); }
            set { SetProperty("ActiveConnection", value); }
        }

        public Object[] Tables
        {
            get
            {
                Object obj = GetProperty("Tables");
                return (Object[])obj;
            }
        }
        #endregion
    }
}
