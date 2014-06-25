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
        ///// <summary>响应</summary>
        ///// <param name="context"></param>
        ///// <param name="attachment"></param>
        ///// <param name="stream"></param>
        ///// <param name="dispositionMode"></param>
        //protected override void OnResponse(HttpContext context, IAttachment attachment, Stream stream, string dispositionMode)
        //{
        //    var wd = new WebDownload();
        //    wd.Stream = stream;
        //    wd.FileName = attachment.FileName;
        //    //if (!String.IsNullOrEmpty(dispositionMode)) wd.Mode = (WebDownload.DispositionMode)Enum.Parse(typeof(WebDownload.DispositionMode), dispositionMode);
        //    wd.Mode = WebDownload.DispositionMode.Attachment;
        //    if (!String.IsNullOrEmpty(attachment.ContentType)) wd.ContentType = attachment.ContentType;
        //    wd.Speed = 100;
        //    wd.Render();
        //}

        /// <summary>参数准备完毕，输出前</summary>
        /// <param name="wd"></param>
        protected override void OnReader(WebDownload wd)
        {
            base.OnReader(wd);

            wd.Mode = WebDownload.DispositionMode.Attachment;
            wd.Speed = 100;
        }
        #endregion
    }
}