using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;

namespace NewLife.Web
{
    /// <summary>提供网页下载支持，在服务端把一个数据流作为附件传给浏览器，带有断点续传和限速的功能</summary>
    public class WebDownload
    {
        #region 属性
        private Stream _Stream;
        /// <summary>数据流</summary>
        public Stream Stream { get { return _Stream; } set { _Stream = value; } }

        private String _FileName;
        /// <summary>属性说明</summary>
        public String FileName { get { return _FileName; } set { _FileName = value; } }

        private String _ContentType = "application/octet-stream";
        /// <summary>内容类型</summary>
        public String ContentType { get { return _ContentType; } set { _ContentType = value; } }

        private DispositionMode _Mode;
        /// <summary>附件配置模式，是在浏览器直接打开，还是提示另存为</summary>
        public DispositionMode Mode { get { return _Mode; } set { _Mode = value; } }

        private Int64 _Speed;
        /// <summary>速度，0表示不限制</summary>
        public Int64 Speed { get { return _Speed; } set { _Speed = value; } }
        #endregion

        #region 枚举
        /// <summary>附件配置模式</summary>
        public enum DispositionMode
        {
            /// <summary>内联模式，在浏览器直接打开</summary>
            Inline,

            /// <summary>附件模式，提示另存为</summary>
            Attachment
        }

        /// <summary>分析模式</summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static DispositionMode ParseMode(String mode)
        {
            return (WebDownload.DispositionMode)Enum.Parse(typeof(WebDownload.DispositionMode), mode);
        }
        #endregion

        /// <summary>输出数据流</summary>
        public virtual void Render()
        {
            var Request = HttpContext.Current.Request;
            var Response = HttpContext.Current.Response;

            var stream = Stream;

            // 速度
            long speed = Speed;
            // 包大小
            int pack = 1024000;
            // 计算睡眠时间
            int sleep = speed > 0 ? (int)Math.Floor(1000 * (double)pack / speed) + 1 : 0;

            // 输出Accept-Ranges，表示支持断点
            Response.AddHeader("Accept-Ranges", "bytes");
            Response.Buffer = false;
            long fileLength = stream.Length;
            long startBytes = 0;

            // 如果请求里面指定范围，表示需要断点
            if (Request.Headers["Range"] != null)
            {
                Response.StatusCode = 206;
                string[] range = Request.Headers["Range"].Split(new char[] { '=', '-' });
                startBytes = Convert.ToInt64(range[1]);
            }
            // 计算真正的长度
            Response.AddHeader("Content-Length", (fileLength - startBytes).ToString());
            if (startBytes != 0)
            {
                // 指定数据范围
                Response.AddHeader("Content-Range", string.Format(" bytes {0}-{1}/{2}", startBytes, fileLength - 1, fileLength));
            }
            Response.AddHeader("Connection", "Keep-Alive");
            Response.ContentType = ContentType;
            Response.AddHeader("Content-Disposition", Mode + ";filename=" + HttpUtility.UrlEncode(FileName, Encoding.UTF8));

            //stream.Seek(startBytes, SeekOrigin.Begin);
            int maxCount = (int)Math.Floor((fileLength - startBytes) / (double)pack) + 1;
            Byte[] buffer = new Byte[pack];
            for (int i = 0; i < maxCount; i++)
            {
                if (!Response.IsClientConnected) break;

                Int32 count = stream.Read(buffer, 0, buffer.Length);
                if (count == pack)
                    Response.BinaryWrite(buffer);
                else
                {
                    Byte[] data = new Byte[count];
                    Buffer.BlockCopy(buffer, 0, data, 0, count);
                    Response.BinaryWrite(data);
                }
                if (sleep > 0) Thread.Sleep(sleep);
            }
        }
    }
}