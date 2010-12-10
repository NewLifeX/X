using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.IO;
using System.Web.Caching;
using NewLife.Log;
using System.Threading;

namespace NewLife.CommonEntity.Web
{
    /// <summary>
    /// 文件下载处理器，可以直接使用，也可以继承
    /// </summary>
    public class DownloadFile : IHttpHandler
    {
        #region IHttpHandler 成员

        bool IHttpHandler.IsReusable
        {
            get { return false; }
        }

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            Download(context);
        }

        #endregion

        #region 业务
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

        private Attachment _Attachment;
        /// <summary>附件</summary>
        public Attachment Attachment
        {
            get
            {
                if (_Attachment == null && ID > 0)
                {
                    _Attachment = Attachment.FindByID(ID);
                }
                return _Attachment;
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="context"></param>
        protected virtual void Download(HttpContext context)
        {
            HttpRequest Request = context.Request;
            HttpResponse Response = context.Response;

            if (Attachment == null)
            {
                Response.StatusCode = 400;
                Response.Status = "文件未找到！";
                return;
            }

            // 增加统计
            Attachment.Increment(null);

            // 速度
            long speed = 1024;

            Stream stream = GetStream();
            try
            {
                Response.AddHeader("Accept-Ranges", "bytes");
                Response.Buffer = false;
                long fileLength = stream.Length;
                long startBytes = 0;

                int pack = 1024000;

                int sleep = (int)Math.Floor(1000 * (double)pack / speed) + 1;
                if (Request.Headers["Range"] != null)
                {
                    Response.StatusCode = 206;
                    string[] range = Request.Headers["Range"].Split(new char[] { '=', '-' });
                    startBytes = Convert.ToInt64(range[1]);
                }
                Response.AddHeader("Content-Length", (fileLength - startBytes).ToString());
                if (startBytes != 0)
                {
                    Response.AddHeader("Content-Range", string.Format(" bytes {0}-{1}/{2}", startBytes, fileLength - 1, fileLength));
                }
                Response.AddHeader("Connection", "Keep-Alive");
                Response.ContentType = "application/octet-stream";
                Response.AddHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(Attachment.FileName, Encoding.UTF8));

                stream.Seek(startBytes, SeekOrigin.Begin);
                int maxCount = (int)Math.Floor((fileLength - startBytes) / (double)pack) + 1;
                Byte[] buffer = new Byte[pack];
                for (int i = 0; i < maxCount; i++)
                {
                    if (Response.IsClientConnected)
                    {
                        Int32 count = stream.Read(buffer, 0, buffer.Length);
                        if (count == pack)
                            Response.BinaryWrite(buffer);
                        else
                        {
                            Byte[] data = new Byte[count];
                            Buffer.BlockCopy(buffer, 0, data, 0, count);
                            Response.BinaryWrite(data);
                        }
                        Thread.Sleep(sleep);
                    }
                    else
                    {
                        //i = maxCount;
                        break;
                    }
                }
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
        /// 获取文件数据流。这里可以实现小文件缓存进入内容以减少磁盘IO
        /// </summary>
        /// <returns></returns>
        protected Stream GetStream()
        {
            if (Attachment.Size > 100 * 1024) return new FileStream(Attachment.FullFilePath, FileMode.Open, FileAccess.Read);

            Stream stream = HttpRuntime.Cache["NewLife.DownloadFile.Cache_" + Attachment.ID] as Stream;
            if (stream != null) return stream;

            FileStream fs = new FileStream(Attachment.FullFilePath, FileMode.Open, FileAccess.Read);
            // 再次判断，防止记录的文件大小不正确而导致系统占用大量内存
            if (fs.Length > 100 * 1024) return fs;

            Byte[] buffer = new Byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            fs.Close();

            stream = new MemoryStream(buffer);
            HttpRuntime.Cache.Insert(null, stream, null, Cache.NoAbsoluteExpiration, GetSmallFileCacheTime());
            return stream;
        }

        /// <summary>
        /// 小文件缓存时间，默认10分钟
        /// </summary>
        /// <returns></returns>
        protected TimeSpan GetSmallFileCacheTime()
        {
            return new TimeSpan(0, 10, 0);
        }
        #endregion
    }
}
