using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Caching;
using NewLife.Log;

namespace NewLife.CommonEntity.Web
{
    /// <summary>
    /// 附件处理器
    /// </summary>
    public class AttachmentHttpHandler : IHttpHandler
    {
        #region IHttpHandler 成员
        bool IHttpHandler.IsReusable
        {
            get { return false; }
        }

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            OnProcess(context);
        }
        #endregion

        #region 属性
        /// <summary>
        /// 文件编号
        /// </summary>
        public Int32 ID
        {
            get
            {
                String str = HttpContext.Current.Request["ID"];
                if (String.IsNullOrEmpty(str)) return 0;

                Int32 n = 0;
                if (!Int32.TryParse(str, out n)) n = 0;

                return n;
            }
        }

        //private Attachment _Attachment;
        ///// <summary>附件</summary>
        //public Attachment Attachment
        //{
        //    get
        //    {
        //        if (_Attachment == null && ID > 0)
        //        {
        //            _Attachment = Attachment.FindByID(ID);
        //        }
        //        return _Attachment;
        //    }
        //}
        #endregion

        #region 文件流
        /// <summary>
        /// 获取文件数据流。这里可以实现小文件缓存进入内容以减少磁盘IO
        /// </summary>
        /// <param name="context"></param>
        /// <param name="attachment"></param>
        /// <returns></returns>
        protected virtual Stream GetStream(HttpContext context, Attachment attachment)
        {
            if (attachment.Size > MaxFileSize) return new FileStream(attachment.FullFilePath, FileMode.Open, FileAccess.Read);

            String key = "NewLife.Attachment.Cache_" + attachment.ID;
            Stream stream = HttpRuntime.Cache[key] as Stream;
            if (stream != null) return stream;

            FileStream fs = new FileStream(attachment.FullFilePath, FileMode.Open, FileAccess.Read);
            // 再次判断，防止记录的文件大小不正确而导致系统占用大量内存
            if (fs.Length > MaxFileSize) return fs;

            Byte[] buffer = new Byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            fs.Close();

            stream = new MemoryStream(buffer);
            HttpRuntime.Cache.Insert(key, stream, null, Cache.NoAbsoluteExpiration, CacheTime);
            return stream;
        }

        /// <summary>
        /// 小于指定大小才缓存文件
        /// </summary>
        protected virtual Int64 MaxFileSize { get { return 100 * 1024; } }

        /// <summary>
        /// 小文件缓存时间，默认10分钟
        /// </summary>
        /// <returns></returns>
        protected virtual TimeSpan CacheTime { get { return new TimeSpan(0, 10, 0); } }
        #endregion

        #region 方法
        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="context"></param>
        protected virtual void OnProcess(HttpContext context)
        {
            Attachment attachment = GetAttachment();

            if (attachment == null || !attachment.IsEnable || !File.Exists(attachment.FullFilePath))
            {
                OnNotFound(context);
                return;
            }

            // 增加统计
            attachment.Increment(null);

            Stream stream = GetStream(context, attachment);
            try
            {
                OnResponse(context, attachment, stream, null);
            }
            catch (Exception ex)
            {
                XTrace.WriteLine(ex.ToString());
            }
            finally
            {
                // 关闭文件流
                if (stream is FileStream) stream.Close();
            }
        }

        /// <summary>
        /// 取得附件对象
        /// </summary>
        /// <returns></returns>
        protected virtual Attachment GetAttachment()
        {
            if (ID > 0)
                return Attachment.FindByID(ID);
            else
                return null;
        }

        /// <summary>
        /// 没找到附件
        /// </summary>
        /// <param name="context"></param>
        protected virtual void OnNotFound(HttpContext context)
        {
            HttpResponse Response = context.Response;

            Response.StatusCode = 400;
            Response.Status = "文件未找到！";

        }

        /// <summary>
        /// 响应
        /// </summary>
        /// <param name="context"></param>
        /// <param name="attachment"></param>
        /// <param name="stream"></param>
        /// <param name="dispositionMode"></param>
        protected virtual void OnResponse(HttpContext context, Attachment attachment, Stream stream, String dispositionMode)
        {
            HttpRequest Request = context.Request;
            HttpResponse Response = context.Response;

            Response.Buffer = false;
            long fileLength = stream.Length;

            int pack = 1024000;

            Response.AddHeader("Content-Length", fileLength.ToString());
            if (!String.IsNullOrEmpty(attachment.ContentType)) Response.ContentType = attachment.ContentType;
            if (String.IsNullOrEmpty(dispositionMode)) dispositionMode = "inline"; // attachment/inline
            Response.AddHeader("Content-Disposition", dispositionMode + "; filename=" + HttpUtility.UrlEncode(attachment.FileName, Encoding.UTF8));

            Byte[] buffer = new Byte[pack];
            while (true)
            {
                if (!Response.IsClientConnected) break;

                Int32 count = stream.Read(buffer, 0, buffer.Length);
                if (count <= 0) break;

                if (count == pack)
                    Response.BinaryWrite(buffer);
                else
                {
                    Byte[] data = new Byte[count];
                    Buffer.BlockCopy(buffer, 0, data, 0, count);
                    Response.BinaryWrite(data);
                }
            }
        }
        #endregion
    }
}