using System;
using System.Web;
using XCode.Configuration;
using XCode.Exceptions;
using XCode.Common;

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

        #region IEntityAccessor 成员
        /// <summary>
        /// 设置参数。返回自身，方便链式写法。
        /// </summary>
        /// <param name="name">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        public override IEntityAccessor SetConfig(string name, object value)
        {
            if (name.EqualIgnoreCase(EntityAccessorOptions.MaxLength))
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
                #region 检查数据类型是否满足目标类型，如果不满足则跳过，以免内部赋值异常导致程序处理出错
                TypeCode code = Type.GetTypeCode(item.Type);
                if (code >= TypeCode.Int16 && code <= TypeCode.UInt64)
                {
                    Int64 n = 0;
                    if (!Int64.TryParse((String)v, out n)) return;
                    v = n;
                }
                else if (code == TypeCode.Single || code == TypeCode.Double)
                {
                    Double d = 0;
                    if (!Double.TryParse((String)v, out d)) return;
                    v = d;
                }
                else if (code == TypeCode.Decimal)
                {
                    Decimal d = 0;
                    if (!Decimal.TryParse((String)v, out d)) return;
                    v = d;
                }
                else if (code == TypeCode.Boolean)
                {
                    Boolean b;
                    if (!Boolean.TryParse((String)v, out b)) return;
                    v = b;
                }
                else if (code == TypeCode.DateTime)
                {
                    DateTime d;
                    if (!DateTime.TryParse((String)v, out d)) return;
                    v = d;
                }
                #endregion

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

        /// <summary>
        /// 采用该方法而不再用Request[name]，主要是后者还处理服务器变量ServerVar，这是不需要的，还有可能得到错误的数据
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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