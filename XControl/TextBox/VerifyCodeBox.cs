using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using NewLife.Configuration;
using Image = System.Web.UI.WebControls.Image;

[assembly: WebResource("XControl.TextBox.VerifyCode.js", "text/javascript")]

namespace XControl
{
    /// <summary>验证码图片控件</summary>
    /// <remarks>
    /// 典型的使用方法,在web.config中配置一个httpHandler
    /// <code>
    ///     configuration
    ///         system.web
    ///             httpHandlers
    ///                 &lt;add verb="GET" path="VerifyCodeImage.aspx" type="XControl.VerifyCodeImageHttpHandler, XControl"/&gt;
    /// </code>
    ///
    /// 如果上述配置的path没有使用默认值,则需要配置这个路径
    /// <code>
    ///     configuration
    ///         appSettings
    ///             &lt;add key="XControl.VerifyCode.DefaultImageHandler" value="~/VerifyCodeImage.aspx"/&gt;
    /// </code>
    ///
    /// 在页面上使用方法如下
    /// <code>
    ///     &lt;asp:TextBox runat="server" ID="TextBox1" Text=""&gt;&lt;/asp:TextBox&gt;
    ///     &lt;XCL:VerifyCodeBox runat="server" ID="VerifyCodeBox1" ControlToValidate="TextBox1"&gt;&lt;/XCL:VerifyCodeBox&gt;
    ///     &lt;asp:Button runat="server" ID="Button1" Text="提交"/&gt;
    /// </code>
    ///
    /// 输出到前端的标签结构如下,注意label的id和内部span的id变化
    /// <code>
    ///     &lt;label id="ClientIDContainer"&gt;&lt;img src="verifyCode.aspx"/&gt;&lt;span id="ClientID"&gt;ErrorMessage&lt;/span&gt;&lt;/label&gt;
    /// </code>
    /// </remarks>
    [Description("验证码图片控件")]
    [ToolboxData("<{0}:VerifyCodeBox runat=\"server\" ControlToValidate=\"\"></{0}:VerifyCodeBox>")]
    [ToolboxBitmap(typeof(Image))]
    public class VerifyCodeBox : BaseValidator
    {
        private static string _DefaultImageHandler;
        /// <summary>
        /// 获取验证码图片当前配置的默认路径
        /// </summary>
        public static string DefaultImageHandler
        {
            get
            {
                if (string.IsNullOrEmpty(_DefaultImageHandler))
                {
                    _DefaultImageHandler = Config.GetConfig<string>("XControl.VerifyCode.DefaultImageHandler", "~/VerifyCodeImage.aspx");
                }
                return _DefaultImageHandler;
            }
        }

        /// <summary>验证码图片控件构造方法</summary>
        public VerifyCodeBox()
        {
            ToolTip = "看不清? 点击图片另换一个。";
            ErrorMessage = "请输入图中的字符！";
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Page.ClientScript.RegisterClientScriptResource(this.GetType(), "XControl.TextBox.VerifyCode.js");
        }

        /// <summary>验证</summary>
        /// <returns></returns>
        protected override bool EvaluateIsValid()
        {
            string input = GetControlValidationValue(ControlToValidate);
            if (!DesignMode)
            {
                var ret = VerifyCodeImageHttpHandler.VerifyCode(input, VerifyGUID, Context);
                if (!ret) VerifyCodeImageHttpHandler.ResetVerifyCode(VerifyGUID, Context);
                return ret;
            }
            return false;
        }

        #region 控件状态的保存和加载

        /// <summary>已重载</summary>
        /// <param name="e"></param>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            if (!DesignMode)
            {
                Page.RegisterRequiresControlState(this);
            }
        }

        /// <summary>
        /// 控件状态类
        /// </summary>
        [Serializable]
        private class ControlState
        {
            private object _ParentState;
            public object ParentState
            {
                get { return _ParentState; }
                set { _ParentState = value; }
            }

            private string _VerifyGUID;
            public string VerifyGUID
            {
                get { return _VerifyGUID; }
                set { _VerifyGUID = value; }
            }

            //private string _ContainerIDPostfix;
            //public string ContainerIDPostfix
            //{
            //    get { return _ContainerIDPostfix; }
            //    set { _ContainerIDPostfix = value; }
            //}

            private string _ContainerTagName;
            public string ContainerTagName
            {
                get { return _ContainerTagName; }
                set { _ContainerTagName = value; }
            }

            private string _ImageHandlerUrl;
            public string ImageHandlerUrl
            {
                get { return _ImageHandlerUrl; }
                set { _ImageHandlerUrl = value; }
            }

            public ControlState(object parent)
            {
                ParentState = parent;
            }
        }

        /// <summary>已重载,保存控件状态</summary>
        /// <returns></returns>
        protected override object SaveControlState()
        {
            return new ControlState(base.SaveControlState())
            {
                VerifyGUID = VerifyGUID,
                ImageHandlerUrl = ImageHandlerUrl,
                //ContainerIDPostfix = ContainerIDPostfix,
                ContainerTagName = ContainerTagName
            };
        }

        /// <summary>已重载,保存控件状态</summary>
        /// <param name="savedState"></param>
        protected override void LoadControlState(object savedState)
        {
            if (savedState == null) return;
            var state = savedState as ControlState;
            if (state == null)
            {
                base.LoadControlState(savedState);
            }
            else
            {
                base.LoadControlState(state.ParentState);
                VerifyGUID = state.VerifyGUID;
                ImageHandlerUrl = state.ImageHandlerUrl;
                ContainerTagName = state.ContainerTagName;
                //ContainerIDPostfix = state.ContainerIDPostfix;
            }
        }

        #endregion

        #region 呈现

        private HtmlGenericControl SpanCtl;

        /// <summary>
        /// 已重载
        /// </summary>
        protected override void CreateChildControls()
        {
            if (string.IsNullOrEmpty(VerifyGUID))
            {
                VerifyGUID = Guid.NewGuid().ToString();
            }
            // 图片
            var img = new HtmlImage();
            if (!DesignMode)
            {
                var src = ResolveUrl(ImageHandlerUrl ?? DefaultImageHandler);
                src += string.Format("{0}verify={1}&rnd={2}", src.Contains("?") ? "&" : "?", VerifyGUID,
                    DateTime.Now.TimeOfDay.TotalMilliseconds);
                img.Src = src;
                img.Attributes.Add("onclick", "VerifyCodeBox_Refresh(this,'" + GetControlRenderID(ControlToValidate) + "')");
                img.Style.Add(HtmlTextWriterStyle.Cursor, "pointer");
            }
            else
            {
                var path = Path.Combine(Path.GetTempPath(), "VerifyCodeImage.gif");
                if (!File.Exists(path))
                {
                    using (var stream = GetType().Assembly.GetManifestResourceStream("XControl.TextBox.VerifyCodeImage.gif"))
                    {
                        using (var file = File.Create(path))
                        {
                            var buffer = new byte[2048];
                            var n = 0;
                            while ((n = stream.Read(buffer, 0, buffer.Length)) != 0)
                            {
                                file.Write(buffer, 0, n);
                            }
                        }
                    }
                }
                img.Src = path;
                img.Width = 143;
                img.Height = 30;
            }
            Controls.Add(img);

            // 文本
            SpanCtl = new HtmlGenericControl("span")
            {
                InnerText = string.IsNullOrEmpty(Text) ? ErrorMessage : Text
            };
            Controls.Add(SpanCtl);

            base.CreateChildControls();
        }

        /// <summary>
        /// 已重载
        /// </summary>
        /// <param name="writer"></param>
        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            if (Style["display"] != null)
            {
                SpanCtl.Style.Add("display", Style["display"]);
                Style.Remove("display");
            }
            else if (Style["visibility"] != null)
            {
                SpanCtl.Style.Add("visibility", Style["visibility"]);
                Style.Remove("visibility");
            }

            if (TagKey == HtmlTextWriterTag.Label)
            {
                Attributes.Add("for", GetControlRenderID(this.ControlToValidate));
            }
            //if (!DesignMode)
            //{
            //    CheckCallClientID = true;
            //}
            //try
            //{
                base.AddAttributesToRender(writer);
            //}
            //finally
            //{
            //    CheckCallClientID = false;
            //}
            if (!DesignMode)
            {
                //SpanCtl.ID = ID;
                Page.ClientScript.RegisterExpandoAttribute(ClientID, "evaluationfunction", "RequiredFieldValidatorEvaluateIsValid", false);
                Page.ClientScript.RegisterExpandoAttribute(ClientID, "initialvalue", "", false);
            }
        }

        //private AttributeCollection origAttr;

        ///// <summary>已重载</summary>
        ///// <param name="writer"></param>
        //protected override void Render(HtmlTextWriter writer)
        //{
        //    if (origAttr == null) // 保存标签属性的旧值,用于输出容器标签
        //    {
        //        origAttr = new AttributeCollection(new StateBag());
        //    }

        //    CopyCollection(Attributes, origAttr,
        //        new GetItemByKey<AttributeCollection, string>(GetAttributeCollectionItem),
        //        new SetItemByKey<AttributeCollection, string>(SetAttributeCollectionItem),
        //        new CopyFilter<AttributeCollection, AttributeCollection, string>(FilterStyleAttribute),
        //        Attributes.Keys);
        //    CopyCollection(Style, origAttr.CssStyle,
        //        new GetItemByKey<CssStyleCollection, string>(GetCssStyleCollectionItem),
        //        new SetItemByKey<CssStyleCollection, string>(SetCssStyleCollectionItem),
        //        null,
        //        Style.Keys);

        //    base.Render(writer);
        //}

        //private string oldErrorMessage;
        //private string[] ErrorMessagePacks = { "<span class=\"error-message-text\">", "", "</span>" };

        ///// <summary>已重载</summary>
        ///// <param name="writer"></param>
        //public override void RenderBeginTag(HtmlTextWriter writer)
        //{
        //    // TODO 修改关于页面会输出2个id一样的标签的问题span和img
        //    // TODO 修改前端效验使始终返回false 以避免被隐藏
        //    writer.AddAttribute(HtmlTextWriterAttribute.Id, this.ClientID);
        //    writer.AddStyleAttribute(HtmlTextWriterStyle.Color, ForeColor.Name);
        //    origAttr.AddAttributes(writer);
        //    writer.RenderBeginTag(ContainerTag);

        //    Attributes.Clear();
        //    Style.Clear();

        //    if (DesignMode)
        //    {
        //    }
        //    else
        //    {
        //        string src = ResolveUrl(ImageHandlerUrl != null ? ImageHandlerUrl : VerifyCodeImageHttpHandler.DefaultPath);
        //        src += string.Format("{0}verify={1}&rnd={2}",
        //            src.Contains("?") ? "&" : "?",
        //            VerifyGUID,
        //            DateTime.Now.TimeOfDay.TotalMilliseconds
        //            );

        //        writer.AddAttribute(HtmlTextWriterAttribute.Src, src, false);
        //        writer.AddAttribute(HtmlTextWriterAttribute.Onclick, "VerifyCodeBox_Refresh(this);");
        //        writer.AddStyleAttribute(HtmlTextWriterStyle.Cursor, "pointer");
        //    }

        //    base.RenderBeginTag(writer);

        //    if (!DesignMode && !string.IsNullOrEmpty(ErrorMessage) && (!Page.IsPostBack || IsValid)) // 第一次访问和通过效验的情况下隐藏错误信息
        //    {
        //        oldErrorMessage = ErrorMessage;
        //        ErrorMessage = "";
        //    }
        //    if (!string.IsNullOrEmpty(ErrorMessage)) // 只要显示错误信息,就一定使用额外的包装显示
        //    {
        //        ErrorMessagePacks[1] = ErrorMessage;
        //        ErrorMessage = string.Join("", ErrorMessagePacks);
        //    }
        //}

        ///// <summary>已重载</summary>
        ///// <param name="writer"></param>
        //public override void RenderEndTag(HtmlTextWriter writer)
        //{
        //    if (!string.IsNullOrEmpty(ErrorMessage)) // 恢复显示错误信息的包装
        //    {
        //        ErrorMessage = ErrorMessage.Substring(ErrorMessagePacks[0].Length, ErrorMessagePacks[1].Length);
        //    }

        //    if (!DesignMode && !string.IsNullOrEmpty(oldErrorMessage) && (!Page.IsPostBack || IsValid)) // 恢复隐藏的错误信息
        //    {
        //        ErrorMessage = oldErrorMessage;
        //        oldErrorMessage = null;
        //    }

        //    base.RenderEndTag(writer);

        //    writer.RenderEndTag();
        //}

        #endregion

        #region 控件属性

        //private bool CheckCallClientID = false;
        /// <summary>内部使用的,用于标识一个表单请求,以便在表单提交时获得当前表单的验证码</summary>
        private string VerifyGUID { get; set; }

        private HtmlTextWriterTag? _TagKey;
        /// <summary>已重载</summary>
        protected override HtmlTextWriterTag TagKey
        {
            get
            {
                if (_TagKey == null)
                {
                    try
                    {
                        _TagKey = (HtmlTextWriterTag)Enum.Parse(typeof(HtmlTextWriterTag), ContainerTagName, true);
                    }
                    catch
                    {
                        _TagKey = HtmlTextWriterTag.Label;
                    }
                }
                return _TagKey.Value;
            }
        }

        ///// <summary>
        ///// 已重载,客户端的ID
        ///// </summary>
        //public override string ClientID
        //{
        //    get
        //    {
        //        var postfix = "";
        //        if (CheckCallClientID)
        //        {
        //            var st = new StackTrace(1, false);
        //            if (st.GetFrame(0).GetMethod() == typeof(WebControl).GetMethod("AddAttributesToRender",
        //                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
        //                null, new Type[] { typeof(HtmlTextWriter) }, null))
        //            {
        //                postfix = string.IsNullOrEmpty(ContainerIDPostfix) ? "Container" : ContainerIDPostfix;
        //            };
        //        }
        //        return base.ClientID + postfix;
        //    }
        //}

        /// <summary>
        /// 效验码图片地址,建议在web.config中设置XControl.VerifyCode.DefaultImageHandler,除非需要特别设置
        /// </summary>
        [Description("效验码图片地址,建议在web.config中设置XControl.VerifyCode.DefaultImageHandler,除非需要特别设置")]
        [Category(" 专用属性")]
        [DefaultValue("")]
        public string ImageHandlerUrl { get; set; }

        ///// <summary>
        ///// 验证码的容器标签ID后缀,默认为Container
        ///// </summary>
        //[Description("验证码的容器标签ID后缀,默认为Container")]
        //[Category(" 专用属性")]
        //[DefaultValue("Container")]
        //public string ContainerIDPostfix { get; set; }

        private string _ContainerTagName;
        /// <summary>
        /// 验证码的容器标签名,默认为span
        /// </summary>
        [Description("验证码的容器标签名,默认为label")]
        [Category(" 专用属性")]
        [DefaultValue("label")]
        public string ContainerTagName
        {
            get
            {
                return _ContainerTagName;
            }
            set
            {
                _ContainerTagName = value;
                _TagKey = null;
            }
        }

        #endregion

        //#region 工具方法

        ///// <summary>根据key从集合获取元素的委托</summary>
        ///// <typeparam name="T"></typeparam>
        ///// <typeparam name="TValue"></typeparam>
        ///// <param name="getcoll"></param>
        ///// <param name="key"></param>
        ///// <returns></returns>
        //public delegate TValue GetItemByKey<T, TValue>(T getcoll, object key);

        ///// <summary>根据key向集合写入元素的委托</summary>
        ///// <typeparam name="T"></typeparam>
        ///// <typeparam name="TValue"></typeparam>
        ///// <param name="setcoll"></param>
        ///// <param name="key"></param>
        ///// <param name="value"></param>
        //public delegate void SetItemByKey<T, TValue>(T setcoll, object key, TValue value);

        ///// <summary>复制集合的过滤器,返回是否不过滤掉,即返回true保留,返回false不保留</summary>
        ///// <typeparam name="TSource"></typeparam>
        ///// <typeparam name="TDest"></typeparam>
        ///// <typeparam name="TValue"></typeparam>
        ///// <param name="source"></param>
        ///// <param name="dest"></param>
        ///// <param name="key"></param>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //public delegate bool CopyFilter<TSource, TDest, TValue>(TSource source, TDest dest, object key, TValue value);

        ///// <summary>复制集合,根据ICollection接口的key</summary>
        ///// <typeparam name="TSource"></typeparam>
        ///// <typeparam name="TDest"></typeparam>
        ///// <typeparam name="TValue"></typeparam>
        ///// <param name="source">复制来源</param>
        ///// <param name="dest">复制目标</param>
        ///// <param name="getItem">复制来源的元素获取方法</param>
        ///// <param name="setItem">复制目标的元素获取方法</param>
        ///// <param name="filter">复制数据过滤器</param>
        ///// <param name="keys">需要赋值的键</param>
        //public static void CopyCollection<TSource, TDest, TValue>(TSource source, TDest dest, GetItemByKey<TSource, TValue> getItem, SetItemByKey<TDest, TValue> setItem, CopyFilter<TSource, TDest, TValue> filter, System.Collections.ICollection keys)
        //{
        //    object[] ary = new object[keys.Count];
        //    keys.CopyTo(ary, 0);
        //    CopyCollection(source, dest, getItem, setItem, filter, ary);
        //}

        ///// <summary>复制集合,根据传递的keys键</summary>
        ///// <typeparam name="TSource"></typeparam>
        ///// <typeparam name="TDest"></typeparam>
        ///// <typeparam name="TValue"></typeparam>
        ///// <param name="source">复制来源</param>
        ///// <param name="dest">复制目标</param>
        ///// <param name="getItem">复制来源的元素获取方法</param>
        ///// <param name="setItem">复制目标的元素获取方法</param>
        ///// <param name="filter">复制数据过滤器</param>
        ///// <param name="keys">需要赋值的键</param>
        //public static void CopyCollection<TSource, TDest, TValue>(TSource source, TDest dest, GetItemByKey<TSource, TValue> getItem, SetItemByKey<TDest, TValue> setItem, CopyFilter<TSource, TDest, TValue> filter, params object[] keys)
        //{
        //    TValue v;
        //    foreach (object k in keys)
        //    {
        //        v = getItem(source, k);
        //        if (filter == null || filter(source, dest, k, v))
        //        {
        //            setItem(dest, k, v);
        //        }
        //    }
        //}

        ///// <summary>AttributeCollection的get方法</summary>
        ///// <param name="coll"></param>
        ///// <param name="key"></param>
        ///// <returns></returns>
        //public static string GetAttributeCollectionItem(AttributeCollection coll, object key)
        //{
        //    return coll[key.ToString()];
        //}

        ///// <summary>AttributeCollection的set方法</summary>
        ///// <param name="coll"></param>
        ///// <param name="key"></param>
        ///// <param name="value"></param>
        //public static void SetAttributeCollectionItem(AttributeCollection coll, object key, string value)
        //{
        //    coll[key.ToString()] = value;
        //}

        ///// <summary>过滤掉html属性中的style属性</summary>
        ///// <param name="s"></param>
        ///// <param name="d"></param>
        ///// <param name="key"></param>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //public static bool FilterStyleAttribute(AttributeCollection s, AttributeCollection d, object key, string value)
        //{
        //    if (key != null)
        //    {
        //        string k = key.ToString();
        //        if (k.Trim().ToLower() == "style")
        //        {
        //            return false;
        //        }
        //    }
        //    return true;
        //}

        ///// <summary>CssStyleCollection的Get方法</summary>
        ///// <param name="coll"></param>
        ///// <param name="key"></param>
        ///// <returns></returns>
        //public static string GetCssStyleCollectionItem(CssStyleCollection coll, object key)
        //{
        //    if (key is HtmlTextWriterStyle)
        //    {
        //        return coll[(HtmlTextWriterStyle)key];
        //    }
        //    else
        //    {
        //        return coll[key.ToString()];
        //    }
        //}

        ///// <summary>CssStyleCollection的Set方法</summary>
        ///// <param name="coll"></param>
        ///// <param name="key"></param>
        ///// <param name="value"></param>
        //public static void SetCssStyleCollectionItem(CssStyleCollection coll, object key, string value)
        //{
        //    if (key is HtmlTextWriterStyle)
        //    {
        //        coll[(HtmlTextWriterStyle)key] = value;
        //    }
        //    else
        //    {
        //        coll[key.ToString()] = value;
        //    }
        //}

        //#endregion
    }
}