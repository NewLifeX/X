using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using XCode.Configuration;
using XCode.Exceptions;

namespace XCode.Accessors
{
    /// <summary>
    /// Http实体访问器，只读不写。
    /// </summary>
    public class HttpEntityAccessor : EntityAccessorBase
    {
        #region 属性
        private HttpRequest _Request;
        /// <summary>请求</summary>
        public HttpRequest Request
        {
            get { return _Request; }
            private set { _Request = value; }
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
        /// <summary>
        /// 实例化一个Http实体访问器
        /// </summary>
        /// <param name="request"></param>
        public HttpEntityAccessor(HttpRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");

            Request = request;
        }
        #endregion

        #region IEntityAccessor 成员
        /// <summary>是否支持从实体对象读取信息</summary>
        public override bool CanRead { get { return false; } }

        /// <summary>
        /// 把指定实体字段的信息写入到实体对象
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="item">实体字段</param>
        protected override void OnWriteItem(IEntity entity, FieldItem item)
        {
            //base.OnWriteItem(entity, item);

            String value = Request[item.Name];
            if (value == null && item.Name != item.ColumnName) value = Request[item.ColumnName];

            if (value != null)
                entity.SetItem(item.Name, value);
            else
            {
                // 处理上传的文件
                HttpPostedFile file = Request.Files[item.Name];
                if (file == null && item.Name != item.ColumnName) file = Request.Files[item.ColumnName];

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
        #endregion
    }
}