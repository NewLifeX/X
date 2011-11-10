using System;
using System.IO;
using System.Web;
using NewLife.Web;

namespace NewLife.CommonEntity.Web
{
    /// <summary>文件下载处理器，可以直接使用，也可以继承</summary>
    public class DownloadFile : AttachmentHttpHandler
    {
        #region 业务
        /// <summary>
        /// 响应
        /// </summary>
        /// <param name="context"></param>
        /// <param name="attachment"></param>
        /// <param name="stream"></param>
        /// <param name="dispositionMode"></param>
        protected override void OnResponse(HttpContext context, Attachment attachment, Stream stream, string dispositionMode)
        {
            WebDownload wd = new WebDownload();
            wd.Stream = stream;
            wd.FileName = attachment.FileName;
            if (!String.IsNullOrEmpty(dispositionMode)) wd.Mode = (WebDownload.DispositionMode)Enum.Parse(typeof(WebDownload.DispositionMode), dispositionMode);
            if (!String.IsNullOrEmpty(attachment.ContentType)) wd.ContentType = attachment.ContentType;
            wd.Speed = 1024;
            wd.Render();

            //HttpRequest Request = context.Request;
            //HttpResponse Response = context.Response;

            //// 速度
            //long speed = 1024;

            //Response.AddHeader("Accept-Ranges", "bytes");
            //Response.Buffer = false;
            //long fileLength = stream.Length;
            //long startBytes = 0;

            //int pack = 1024000;

            //int sleep = (int)Math.Floor(1000 * (double)pack / speed) + 1;
            //if (Request.Headers["Range"] != null)
            //{
            //    Response.StatusCode = 206;
            //    string[] range = Request.Headers["Range"].Split(new char[] { '=', '-' });
            //    startBytes = Convert.ToInt64(range[1]);
            //}
            //Response.AddHeader("Content-Length", (fileLength - startBytes).ToString());
            //if (startBytes != 0)
            //{
            //    Response.AddHeader("Content-Range", string.Format(" bytes {0}-{1}/{2}", startBytes, fileLength - 1, fileLength));
            //}
            //Response.AddHeader("Connection", "Keep-Alive");
            //Response.ContentType = "application/octet-stream";
            //Response.AddHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(attachment.FileName, Encoding.UTF8));

            //stream.Seek(startBytes, SeekOrigin.Begin);
            //int maxCount = (int)Math.Floor((fileLength - startBytes) / (double)pack) + 1;
            //Byte[] buffer = new Byte[pack];
            //for (int i = 0; i < maxCount; i++)
            //{
            //    if (!Response.IsClientConnected) break;

            //    Int32 count = stream.Read(buffer, 0, buffer.Length);
            //    if (count == pack)
            //        Response.BinaryWrite(buffer);
            //    else
            //    {
            //        Byte[] data = new Byte[count];
            //        Buffer.BlockCopy(buffer, 0, data, 0, count);
            //        Response.BinaryWrite(data);
            //    }
            //    Thread.Sleep(sleep);
            //}
        }
        #endregion
    }
}