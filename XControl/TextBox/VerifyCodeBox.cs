using System;
using System.ComponentModel;
using System.Drawing;
using System.Web.UI;
using System.Web.UI.WebControls;
using AttributeCollection = System.Web.UI.AttributeCollection;
using Image = System.Web.UI.WebControls.Image;

[assembly: WebResource("XControl.TextBox.VerifyCode.js", "text/javascript")]

namespace XControl
{
    /// <summary>验证码图片控件</summary>
    [Description("验证码图片控件")]
    [ToolboxData("<{0}:VerifyCodeBox runat=server></{0}:VerifyCodeBox>")]
    [ToolboxBitmap(typeof(Image))]
    public class VerifyCodeBox : BaseValidator
    {
        /// <summary>验证码图片控件构造方法</summary>
        public VerifyCodeBox()
            : base()
        {
            ToolTip = "看不清? 点击另换一个.";
            ContainerTag = HtmlTextWriterTag.Span;
        }

        /// <summary>验证</summary>
        /// <returns></returns>
        protected override bool EvaluateIsValid()
        {
            string input = GetControlValidationValue(ControlToValidate);
            IsValid = VerifyCodeImageHttpHandler.VerifyCode(input, VerifyGUID, Context);
            if (!IsValid) VerifyCodeImageHttpHandler.ResetVerifyCode(VerifyGUID, Context);
            return IsValid;
        }

        /// <summary></summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Page.ClientScript.RegisterClientScriptResource(this.GetType(), "XControl.TextBox.VerifyCode.js");
        }

        #region 控件呈现
        /// <summary>已重载</summary>
        protected override HtmlTextWriterTag TagKey
        {
            get
            {
                return HtmlTextWriterTag.Img;
            }
        }

        /// <summary>容器标签名,如果设置有,则将整个图片和提示信息包含到这个标签内</summary>
        [Description("容器标签名,如果设置有,则将整个图片和提示信息包含到这个标签内")]
        [Category(" 专用属性")]
        [DefaultValue(0)]
        public HtmlTextWriterTag ContainerTag { get; set; }

        /// <summary>已重载</summary>
        /// <param name="e"></param>
        protected override void OnPreRender(EventArgs e)
        {
            if (DesignMode)
            {
                Width = Unit.Pixel(143);
                Height = Unit.Pixel(30);
            }
            else
            {
                if (string.IsNullOrEmpty(VerifyGUID))
                {
                    VerifyGUID = Guid.NewGuid().ToString();
                }
            }

            base.OnPreRender(e);
        }

        /// <summary>已重载</summary>
        /// <param name="writer"></param>
        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            base.AddAttributesToRender(writer);
        }

        private AttributeCollection origAttr;
        /// <summary>已重载</summary>
        /// <param name="writer"></param>
        protected override void Render(HtmlTextWriter writer)
        {
            if (origAttr == null) // 保存标签属性的旧值,用于输出容器标签
            {
                origAttr = new AttributeCollection(new StateBag());
            }

            CopyCollection(Attributes, origAttr,
                new GetItemByKey<AttributeCollection, string>(GetAttributeCollectionItem),
                new SetItemByKey<AttributeCollection, string>(SetAttributeCollectionItem),
                new CopyFilter<AttributeCollection, AttributeCollection, string>(FilterStyleAttribute),
                Attributes.Keys);
            CopyCollection(Style, origAttr.CssStyle,
                new GetItemByKey<CssStyleCollection, string>(GetCssStyleCollectionItem),
                new SetItemByKey<CssStyleCollection, string>(SetCssStyleCollectionItem),
                null,
                Style.Keys);

            base.Render(writer);

        }

        private string oldErrorMessage;
        private string[] ErrorMessagePacks = { "<span class=\"error-message-text\">", "", "</span>" };
        /// <summary>已重载</summary>
        /// <param name="writer"></param>
        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Id, this.ClientID);
            writer.AddStyleAttribute(HtmlTextWriterStyle.Color, ForeColor.Name);
            origAttr.AddAttributes(writer);
            writer.RenderBeginTag(ContainerTag);

            Attributes.Clear();
            Style.Clear();

            if (DesignMode)
            {

            }
            else
            {
                string src = ResolveUrl(ImageHandlerUrl != null ? ImageHandlerUrl : VerifyCodeImageHttpHandler.DefaultPath);
                src += string.Format("{0}verify={1}&rnd={2}",
                    src.Contains("?") ? "&" : "?",
                    VerifyGUID,
                    DateTime.Now.TimeOfDay.TotalMilliseconds
                    );

                writer.AddAttribute(HtmlTextWriterAttribute.Src, src, false);
                writer.AddAttribute(HtmlTextWriterAttribute.Onclick, "VerifyCodeBox_Refresh(this);");
                writer.AddStyleAttribute(HtmlTextWriterStyle.Cursor, "pointer");
            }


            base.RenderBeginTag(writer);


            if (!DesignMode && !string.IsNullOrEmpty(ErrorMessage) && (!Page.IsPostBack || IsValid)) // 第一次访问和通过效验的情况下隐藏错误信息
            {
                oldErrorMessage = ErrorMessage;
                ErrorMessage = "";
            }
            if (!string.IsNullOrEmpty(ErrorMessage)) // 只要显示错误信息,就一定使用额外的包装显示
            {
                ErrorMessagePacks[1] = ErrorMessage;
                ErrorMessage = string.Join("", ErrorMessagePacks);
            }

        }

        /// <summary>已重载</summary>
        /// <param name="writer"></param>
        protected override void RenderContents(HtmlTextWriter writer)
        {
            base.RenderContents(writer);
        }

        /// <summary>已重载</summary>
        /// <param name="writer"></param>
        public override void RenderEndTag(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(ErrorMessage)) // 恢复显示错误信息的包装
            {
                ErrorMessage = ErrorMessage.Substring(ErrorMessagePacks[0].Length, ErrorMessagePacks[1].Length);
            }

            if (!DesignMode && !string.IsNullOrEmpty(oldErrorMessage) && (!Page.IsPostBack || IsValid)) // 恢复隐藏的错误信息
            {
                ErrorMessage = oldErrorMessage;
                oldErrorMessage = null;
            }

            base.RenderEndTag(writer);

            writer.RenderEndTag();
        }
        #endregion

        #region 保持VerifyGUID标识
        /// <summary>已重载</summary>
        /// <param name="e"></param>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            Page.RegisterRequiresControlState(this);
        }
        /// <summary>已重载,保存VerifyGUID属性</summary>
        /// <returns></returns>
        protected override object SaveControlState()
        {
            object obj = base.SaveControlState();
            if (string.IsNullOrEmpty(VerifyGUID))
            {
                return obj;
            }
            if (obj == null)
            {
                return VerifyGUID;
            }
            return new Pair(obj, VerifyGUID);
        }
        /// <summary>已重载,读取VerifyGUID属性</summary>
        /// <param name="savedState"></param>
        protected override void LoadControlState(object savedState)
        {
            if (savedState == null) return;
            Pair p = savedState as Pair;
            if (p == null)
            {
                if (savedState is string)
                {
                    VerifyGUID = (string)savedState;
                }
                else
                {
                    base.LoadControlState(savedState);
                }
                return;
            }

            base.LoadControlState(p.First);
            VerifyGUID = (string)p.Second;
        }
        #endregion

        #region 控件属性
        /// <summary>内部使用的,用于标识一个表单请求,以便在表单提交时获得当前表单的验证码</summary>
        private string VerifyGUID { get; set; }

        /// <summary>效验码图片地址,默认为~/VerifyCodeImage.aspx,其对应于VerifyCodeImageHttpHandler</summary>
        [Description("效验码图片地址,一般建议通过在Web.config中按照约定设置,而不是在这里设置,除非有特别的需要")]
        [Category(" 专用属性")]
        [DefaultValue("")]
        public string ImageHandlerUrl { get; set; }

        /// <summary>需要效验的效验码文本框</summary>
        [Description("需要效验的效验码文本框")]
        [Category(" 专用属性")]
        [DefaultValue("")]
        [TypeConverter(typeof(ValidatedControlConverter))]
        public string ControlToVerify { get; set; }
        #endregion

        #region 工具方法
        /// <summary>根据key从集合获取元素的委托</summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="getcoll"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public delegate TValue GetItemByKey<T, TValue>(T getcoll, object key);

        /// <summary>根据key向集合写入元素的委托</summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="setcoll"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public delegate void SetItemByKey<T, TValue>(T setcoll, object key, TValue value);

        /// <summary>复制集合的过滤器,返回是否不过滤掉,即返回true保留,返回false不保留</summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDest"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public delegate bool CopyFilter<TSource, TDest, TValue>(TSource source, TDest dest, object key, TValue value);

        /// <summary>复制集合,根据ICollection接口的key</summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDest"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="source">复制来源</param>
        /// <param name="dest">复制目标</param>
        /// <param name="getItem">复制来源的元素获取方法</param>
        /// <param name="setItem">复制目标的元素获取方法</param>
        /// <param name="filter">复制数据过滤器</param>
        /// <param name="keys">需要赋值的键</param>
        public static void CopyCollection<TSource, TDest, TValue>(TSource source, TDest dest, GetItemByKey<TSource, TValue> getItem, SetItemByKey<TDest, TValue> setItem, CopyFilter<TSource, TDest, TValue> filter, System.Collections.ICollection keys)
        {
            object[] ary = new object[keys.Count];
            keys.CopyTo(ary, 0);
            CopyCollection(source, dest, getItem, setItem, filter, ary);
        }

        /// <summary>复制集合,根据传递的keys键</summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDest"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="source">复制来源</param>
        /// <param name="dest">复制目标</param>
        /// <param name="getItem">复制来源的元素获取方法</param>
        /// <param name="setItem">复制目标的元素获取方法</param>
        /// <param name="filter">复制数据过滤器</param>
        /// <param name="keys">需要赋值的键</param>
        public static void CopyCollection<TSource, TDest, TValue>(TSource source, TDest dest, GetItemByKey<TSource, TValue> getItem, SetItemByKey<TDest, TValue> setItem, CopyFilter<TSource, TDest, TValue> filter, params object[] keys)
        {
            TValue v;
            foreach (object k in keys)
            {
                v = getItem(source, k);
                if (filter == null || filter(source, dest, k, v))
                {
                    setItem(dest, k, v);
                }
            }
        }

        /// <summary>AttributeCollection的get方法</summary>
        /// <param name="coll"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetAttributeCollectionItem(AttributeCollection coll, object key)
        {
            return coll[key.ToString()];
        }

        /// <summary>AttributeCollection的set方法</summary>
        /// <param name="coll"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetAttributeCollectionItem(AttributeCollection coll, object key, string value)
        {
            coll[key.ToString()] = value;
        }

        /// <summary>过滤掉html属性中的style属性</summary>
        /// <param name="s"></param>
        /// <param name="d"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool FilterStyleAttribute(AttributeCollection s, AttributeCollection d, object key, string value)
        {
            if (key != null)
            {
                string k = key.ToString();
                if (k.Trim().ToLower() == "style")
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>CssStyleCollection的Get方法</summary>
        /// <param name="coll"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetCssStyleCollectionItem(CssStyleCollection coll, object key)
        {
            if (key is HtmlTextWriterStyle)
            {
                return coll[(HtmlTextWriterStyle)key];
            }
            else
            {
                return coll[key.ToString()];
            }
        }

        /// <summary>CssStyleCollection的Set方法</summary>
        /// <param name="coll"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetCssStyleCollectionItem(CssStyleCollection coll, object key, string value)
        {
            if (key is HtmlTextWriterStyle)
            {
                coll[(HtmlTextWriterStyle)key] = value;
            }
            else
            {
                coll[key.ToString()] = value;
            }
        }
        #endregion
    }
}