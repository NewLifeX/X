using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.SessionState;
using System.Web.Configuration;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace XControl
{
    /// <summary>
    /// 用于产生效验码图片自身,需要在web.config中配置为一个httpHandler
    /// 
    /// configuration
    ///     system.web
    ///         httpHandlers
    ///             &lt;add verb="GET" path="VerifyCodeImage.aspx" type="XControl.VerifyCodeImageHttpHandler, XControl"/&gt;
    ///             
    /// 额外的,如果修改配置中的path,可以在appSettings中增加名为VerifyCodeImageHttpHandlerPath的修改后路径,可以使所有使用VerifyCodeImageBox控件的地方默认使用配置的路径
    /// 
    /// </summary>
    public class VerifyCodeImageHttpHandler : IHttpHandler, IRequiresSessionState
    {
        private static string _VerifyCodeSessionPrefix;
        private static string VerifyCodeSessionPrefix
        {
            get
            {
                if (_VerifyCodeSessionPrefix == null)
                {
                    _VerifyCodeSessionPrefix = typeof(VerifyCodeImageHttpHandler).FullName;
                }
                return _VerifyCodeSessionPrefix;
            }
        }
        private static Random rand;
        /// <summary>构造方法</summary>
        public VerifyCodeImageHttpHandler()
        {
            if (rand == null)
            {
                rand = new Random();
            }
        }
        /// <summary>可重用</summary>
        public bool IsReusable
        {
            get { return true; }
        }
        /// <summary>处理请求</summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            HttpRequest Request = context.Request;
            HttpResponse Response=context.Response;
            string verify = Request["verify"];
            string verifyCode = null;
            Response.ContentType = "image/Gif";

            if (string.IsNullOrEmpty(verify))
            {
                
                CreateErrorImage("错误的验证标识",Response.OutputStream);
                return;
            }

            string[] verifycodes = new string[5];
            for (int i = 0; i < verifycodes.Length; i++)
            {
                verifycodes[i] = rand.Next(10).ToString();
            }
            verifyCode = string.Join("", verifycodes);

            CreateVerifyCodeImage(verifyCode,Response.OutputStream);

            if (!string.IsNullOrEmpty(verifyCode))
            {
                context.Session[VerifyCodeSessionPrefix + "_" + verify] = verifyCode.ToLower();
            }
        }
        /// <summary>效验指定输入是否和指定请求产生的验证码相同</summary>
        /// <param name="input">用户输入</param>
        /// <param name="verify">验证标识,一般是VerifyCodeBox.VerifyGUID</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool VerifyCode(string input, string verify, HttpContext context)
        {
            if (string.IsNullOrEmpty(input)) return false;
            return input.ToLower().Equals(context.Session[VerifyCodeSessionPrefix + "_" + verify]);
        }
        /// <summary>复位指定验证标识,除非再次请求验证码,否则任何VerifyCode方法的调用都会返回false</summary>
        /// <param name="verify"></param>
        /// <param name="context"></param>
        public static void ResetVerifyCode(string verify, HttpContext context)
        {
            if (string.IsNullOrEmpty(verify)) return;
            context.Session.Remove(VerifyCodeSessionPrefix + "_" + verify);
        }
        private static Color[] colors = { Color.Black, Color.Red, Color.DarkBlue, Color.Green, Color.Orange, Color.Brown, Color.DarkCyan, Color.Purple };
        private static string[] fonts = { "Verdana", "Microsoft Sans Serif", "Comic Sans MS", "Arial", "宋体" };
        private void CreateVerifyCodeImage(string verifyCode,Stream stream)
        {
            Bitmap image = new Bitmap((int)Math.Ceiling(verifyCode.Length * 28.5), 30);
            Graphics g = Graphics.FromImage(image);


            try
            {
                // 清空图片
                g.Clear(Color.White);
                // 背景噪音线
                for (int i = 0; i < 20; i++)
                {
                    int x1 = rand.Next(image.Width);
                    int x2 = rand.Next(image.Width);
                    int y1 = rand.Next(image.Height);
                    int y2 = rand.Next(image.Height);
                    g.DrawLine(new Pen(colors[rand.Next(colors.Length)]), x1, y1, x2, y2);
                }

                for (int i = 0; i < verifyCode.Length; i++)
                {
                    int cindex = rand.Next(7);
                    int findex = rand.Next(5);
                    
                    Font drawfont = new Font(fonts[rand.Next(fonts.Length)], 18, FontStyle.Bold);
                    SolidBrush drawbrush=new SolidBrush(colors[rand.Next(colors.Length)]);
                    
                    RectangleF drawrect=new RectangleF(
                        5.0F + rand.Next(10) + i*25,
                        .0F+rand.Next(image.Height - 25),
                        20.0F,
                        25.0F
                        );
                    
                    StringFormat drawformat=new StringFormat();
                    drawformat.Alignment=StringAlignment.Center;

                    g.DrawString(verifyCode[i].ToString(), drawfont, drawbrush, drawrect, drawformat);
                }

                for (int i = 0; i < 100; i++)
                {
                    int x = rand.Next(image.Width);
                    int y = rand.Next(image.Height);
                    image.SetPixel(x, y, Color.FromArgb(rand.Next()));
                }

                g.DrawRectangle(new Pen(Color.Silver), 0, 0, image.Width - 1, image.Height - 1);

                image.Save(stream, ImageFormat.Gif);
            }
            finally
            {
                g.Dispose();
                image.Dispose();
            }
        }
        private SolidBrush ErrorBrush;
        private Font ErrorFont;
        Dictionary<string, byte[]> Messages = new Dictionary<string, byte[]>();
        /// <summary>创建错误图片,不要使用这个方法显示动态错误消息</summary>
        /// <param name="msg"></param>
        /// <param name="stream"></param>
        private void CreateErrorImage(string msg, Stream stream)
        {
            if (Messages.Count == 0)
            {
                ErrorFont = new Font(FontFamily.GenericSansSerif, 16);
                ErrorBrush = new SolidBrush(Color.Black);
            }
            if (!Messages.ContainsKey(msg))
            {
                Bitmap image = new Bitmap(msg.Length * 57, 30);
                Graphics g = Graphics.FromImage(image);
                try
                {
                    g.Clear(Color.White);
                    g.DrawString(msg, ErrorFont, ErrorBrush, 0, 0);
                    
                    MemoryStream ms=new MemoryStream();
                    image.Save(ms,ImageFormat.Gif);
                    Messages[msg] = ms.ToArray();
                }
                finally
                {
                    g.Dispose();
                    image.Dispose();

                }
            }
            if (Messages.ContainsKey(msg))
            {
                byte[] data = Messages[msg];
                stream.Write(data, 0, data.Length);
            }
        }

        private static string _DefaultPath;
        /// <summary>获取验证码图片当前配置的默认路径</summary>
        public static string DefaultPath
        {
            get
            {
                if (_DefaultPath == null)
                {
                    string path = WebConfigurationManager.AppSettings["VerifyCodeImageHttpHandlerPath"];
                    if (string.IsNullOrEmpty(path))
                    {
                        path = "~/VerifyCodeImage.aspx";
                    }
                    _DefaultPath=path;
                }
                return _DefaultPath;
            }
        }

    }
}
