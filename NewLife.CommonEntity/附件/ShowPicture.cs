using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Web;

namespace NewLife.CommonEntity.Web
{
    /// <summary>显示图片</summary>
    public class ShowPicture : AttachmentHttpHandler
    {
        #region 属性
        private Int32 _Width = 320;
        /// <summary>宽度</summary>
        public virtual Int32 Width
        {
            get { return _Width; }
            set { _Width = value; }
        }

        private Int32 _Height = 240;
        /// <summary>高度</summary>
        public virtual Int32 Height
        {
            get { return _Height; }
            set { _Height = value; }
        }
        #endregion

        /// <summary>未找到</summary>
        /// <param name="context"></param>
        protected override void OnNotFound(HttpContext context)
        {
            //读取资源
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NewLife.CommonEntity.附件.nopic.jpg");

            var entity = new Attachment();
            entity.FileName = "NoPic";
            entity.ContentType = "image/jpeg";

            stream = MakeThumbnail(context, stream);

            OnResponse(context, entity, stream, null);
        }

        /// <summary>取得文件流，判断是否小图片，特殊处理</summary>
        /// <param name="context"></param>
        /// <param name="attachment"></param>
        /// <returns></returns>
        protected override Stream GetStream(HttpContext context, IAttachment attachment)
        {
            var stream = base.GetStream(context, attachment);

            stream = MakeThumbnail(context, stream);

            return stream;
        }

        Stream MakeThumbnail(HttpContext context, Stream stream)
        {
            HttpRequest Request = context.Request;
            String type = Request["Type"];
            if (String.IsNullOrEmpty(type)) type = Request["t"];
            if (!String.IsNullOrEmpty(type)) type = type.ToLower();
            // 小图片
            if (type == "small" || type == "s")
            {
                Int32 n = 0;
                if (Int32.TryParse(Request["Width"], out n)) Width = n;
                if (Int32.TryParse(Request["w"], out n)) Width = n;
                if (Int32.TryParse(Request["Height"], out n)) Height = n;
                if (Int32.TryParse(Request["h"], out n)) Height = n;

                stream = MakeThumbnail(stream, Width, Height, null);
            }
            return stream;
        }

        /// <summary>生成缩略图</summary>
        /// <param name="stream">源图</param>
        /// <param name="width">缩略图宽度</param>
        /// <param name="height">缩略图高度</param>
        /// <param name="mode">生成缩略图的方式</param>    
        /// <returns>缩略图</returns>
        public static Stream MakeThumbnail(Stream stream, int width, int height, string mode)
        {
            Int64 p = stream.Position;

            Image originalImage = Image.FromStream(stream);

            int towidth = width;
            int toheight = height;

            int x = 0;
            int y = 0;
            int ow = originalImage.Width;
            int oh = originalImage.Height;

            // 是否有必要做缩略图
            if (ow <= width && oh <= height && stream.CanSeek)
            {
                originalImage.Dispose();
                stream.Position = p;
                return stream;
            }

            switch (mode)
            {
                case "HW"://指定高宽缩放（可能变形）                
                    towidth = width;
                    toheight = height;
                    break;
                case "W"://指定宽，高按比例                    
                    towidth = width;
                    toheight = towidth * oh / ow;
                    break;
                case "H"://指定高，宽按比例
                    toheight = height;
                    towidth = toheight * ow / oh;
                    break;
                case "Cut"://指定高宽裁减（不变形）                
                    if ((double)ow / (double)oh > (double)width / (double)height)
                    {
                        ow = oh * width / height;
                        y = 0;
                        x = (originalImage.Width - ow) / 2;
                    }
                    else
                    {
                        oh = ow * height / width;
                        x = 0;
                        y = (originalImage.Height - oh) / 2;
                    }
                    break;
                case "MaxHW"://指定最大高宽缩放（不变形）                
                    if ((double)ow / (double)oh > (double)width / (double)height)
                    {
                        // 原始宽高比大于目标宽高比，如4/3 > 1/1，则以较小的目标宽为基准
                        towidth = width;
                        // 高按比例
                        toheight = towidth * oh / ow;
                    }
                    else
                    {
                        // 原始宽高比小于目标宽高比，如1/1 < 4/3，则以较小的目标高为基准
                        toheight = height;
                        // 宽按比例
                        towidth = toheight * ow / oh;
                    }
                    break;
                case "Auto"://指定最大高宽缩小（不变形）                
                default:
                    if ((double)ow / (double)oh > (double)width / (double)height)
                    {
                        // 原始宽高比大于目标宽高比，如4/3 > 1/1，则以较小的目标宽为基准
                        towidth = width < ow ? width : ow;
                        // 高按比例
                        toheight = towidth * oh / ow;
                    }
                    else
                    {
                        // 原始宽高比小于目标宽高比，如1/1 < 4/3，则以较小的目标高为基准
                        toheight = height < oh ? height : oh;
                        // 宽按比例
                        towidth = toheight * ow / oh;
                    }
                    break;
            }

            //新建一个bmp图片
            Image bitmap = new Bitmap(towidth, toheight);

            //新建一个画板
            Graphics g = Graphics.FromImage(bitmap);

            //设置高质量插值法
            g.InterpolationMode = InterpolationMode.High;

            //设置高质量,低速度呈现平滑程度
            g.SmoothingMode = SmoothingMode.HighQuality;

            //清空画布并以透明背景色填充
            g.Clear(Color.Transparent);

            //在指定位置并且按指定大小绘制原图片的指定部分
            g.DrawImage(originalImage, new Rectangle(0, 0, towidth, toheight), new Rectangle(x, y, ow, oh), GraphicsUnit.Pixel);

            try
            {
                //以jpg格式保存缩略图
                MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Jpeg);
                ms.Position = 0;
                return ms;
            }
            finally
            {
                originalImage.Dispose();
                bitmap.Dispose();
                g.Dispose();
            }
        }
    }
}