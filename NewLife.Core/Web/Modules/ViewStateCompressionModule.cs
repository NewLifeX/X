//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Web;
//using System.IO;
//using System.Web.UI;
//using NewLife.Configuration;
//using NewLife.IO;

//namespace NewLife.Web
//{
//    /// <summary>ViewState压缩模块</summary>
//    public class ViewStateCompressionModule : IHttpModule
//    {
//        #region IHttpModule Members
//        void IHttpModule.Dispose() { }

//        /// <summary>
//        /// 初始化模块，准备拦截请求。
//        /// </summary>
//        /// <param name="context"></param>
//        void IHttpModule.Init(HttpApplication context)
//        {
//            context.PreRequestHandlerExecute += new EventHandler(context_PreRequestHandlerExecute);
//        }

//        void context_PreRequestHandlerExecute(object sender, EventArgs e)
//        {
//            Page page = HttpContext.Current.Handler as Page;
//            if (page == null) return;


//        }
//        #endregion

//        #region 压缩ViewState
//        /// <summary>
//        /// 设定序列化后的字符串长度为多少后启用压缩
//        /// </summary>
//        private static Int32 LimitLength = 1096;

//        /// <summary>
//        /// 是否压缩ViewState
//        /// </summary>
//        protected static Boolean CompressViewState { get { return Config.GetConfig<Boolean>("NewLife.Web.CompressViewState", true); } }

//        /// <summary>
//        /// 重写保存页的所有视图状态信息
//        /// </summary>
//        /// <param name="state">要在其中存储视图状态信息的对象</param>
//        protected virtual void SavePageStateToPersistenceMedium(Object state)
//        {
//            if (!CompressViewState)
//            {
//                base.SavePageStateToPersistenceMedium(state);
//                return;
//            }

//            MemoryStream ms = new MemoryStream();
//            new LosFormatter().Serialize(ms, state);

//            String vs = null;

//            //判断序列化对象的字符串长度是否超出定义的长度界限
//            if (ms.Length > LimitLength)
//            {
//                MemoryStream ms2 = new MemoryStream();
//                // 必须移到第一位，否则后面读不到数据
//                ms.Position = 0;
//                IOHelper.Compress(ms, ms2);
//                vs = "1$" + Convert.ToBase64String(ms2.ToArray());
//            }
//            else
//                vs = Convert.ToBase64String(ms.ToArray());

//            //注册在页面储存ViewState状态的隐藏文本框，并将内容写入这个文本框
//            ClientScript.RegisterHiddenField("__VSTATE", vs);
//        }

//        /// <summary>
//        /// 重写将所有保存的视图状态信息加载到页面对象
//        /// </summary>
//        /// <returns>保存的视图状态</returns>
//        protected override Object LoadPageStateFromPersistenceMedium()
//        {
//            if (!CompressViewState) return base.LoadPageStateFromPersistenceMedium();

//            //使用Request方法获取序列化的ViewState字符串
//            String vs = Request.Form.Get("__VSTATE");

//            Byte[] bts = null;

//            if (vs.StartsWith("1$"))
//                bts = IOHelper.Decompress(Convert.FromBase64String(vs.Substring(2)));
//            else
//                bts = Convert.FromBase64String(vs);

//            //将指定的视图状态值转换为有限对象序列化 (LOS) 格式化的对象
//            return new LosFormatter().Deserialize(new MemoryStream(bts));
//        }
//        #endregion

//    }
//}