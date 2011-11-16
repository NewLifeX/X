using System;
using System.Web;
using XCode.Configuration;
using XCode.Exceptions;

namespace XCode.Accessors
{
    /// <summary>Http实体访问器，只读不写。</summary>
    class HttpEntityAccessor : EntityAccessorBase
    {
        #region 属性
        private HttpRequest _Request;
        /// <summary>请求</summary>
        public HttpRequest Request
        {
            get { return _Request ?? (_Request = HttpContext.Current.Request); }
            private set { _Request = value; }
        }

        private String _ItemPrefix = "frm";
        /// <summary>前缀，只用于Form</summary>
        public String ItemPrefix
        {
            get { return _ItemPrefix; }
            set { _ItemPrefix = value; }
        }

        private Int64 _MaxLength = 10 * 1024 * 1024;
        /// <summary>最大文件大小，默认10M</summary>
        public Int64 MaxLength
        {
            get { return _MaxLength; }
            set { _MaxLength = value; }
        }
        #endregion

        #region 构造
        ///// <summary>
        ///// 实例化一个Http实体访问器
        ///// </summary>
        ///// <param name="request"></param>
        //public HttpEntityAccessor(HttpRequest request)
        //{
        //    if (request == null) throw new ArgumentNullException("request");

        //    Request = request;
        //}
        #endregion

        #region IEntityAccessor 成员
        /// <summary>
        /// 设置参数。返回自身，方便链式写法。
        /// </summary>
        /// <param name="name">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        public override IEntityAccessor SetConfig(string name, object value)
        {
            if (name.EqualIgnoreCase(EntityAccessorOptions.Request))
                Request = value as HttpRequest;
            else if (name.EqualIgnoreCase(EntityAccessorOptions.MaxLength))
                MaxLength = (Int64)value;
            else if (name.EqualIgnoreCase(EntityAccessorOptions.ItemPrefix))
                ItemPrefix = (String)value;

            return base.SetConfig(name, value);
        }

        /// <summary>是否支持把信息写入到外部</summary>
        public override bool CanWrite { get { return false; } }

        /// <summary>
        /// 外部=>实体，从外部读取指定实体字段的信息
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="item">实体字段</param>
        protected override void ReadItem(IEntity entity, FieldItem item)
        {
            Object v = GetRequestItem(item);
            if (v == null) return;

            if (v is String)
            {
                entity.SetItem(item.Name, v);
            }
            else
            {
                // 处理上传的文件
                HttpPostedFile file = v as HttpPostedFile;

                if (file != null)
                {
                    // 把文件内容读出来
                    if (file.ContentLength > MaxLength) throw new XCodeException("文件大小{0}超过了最大限制{1}！", file.ContentLength, MaxLength);
                    Byte[] bts = new Byte[file.ContentLength];
                    file.InputStream.Read(bts, 0, bts.Length);

                    if (item.Type == typeof(Byte[]))
                        entity.SetItem(item.Name, bts);
                    else if (item.Type == typeof(String))
                        entity.SetItem(item.Name, Convert.ToBase64String(bts));
                }
            }
        }

        protected virtual Object GetRequestItem(FieldItem item)
        {
            if (item == null) return null;

            String value = GetRequest(item.Name);
            if (value == null && item.Name != item.ColumnName) value = GetRequest(item.ColumnName);

            if (value != null) return value;

            // 处理上传的文件
            HttpPostedFile file = Request.Files[item.Name];
            if (file == null && item.Name != item.ColumnName) file = Request.Files[item.ColumnName];

            return file;
        }

        String GetRequest(String name)
        {
            String value = Request.QueryString[name];
            if (!String.IsNullOrEmpty(value)) return value;

            value = Request.Form[name];
            if (!String.IsNullOrEmpty(value)) return value;

            // 加上前缀再试试
            value = Request.Form[ItemPrefix + name];
            if (!String.IsNullOrEmpty(value)) return value;

            HttpCookie cookie = Request.Cookies[name];
            if (cookie != null) value = cookie.Value;
            if (!String.IsNullOrEmpty(value)) return value;

            return null;
        }
        #endregion
    }
}