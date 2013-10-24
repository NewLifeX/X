using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Reflection;

namespace NewLife.ServiceLib
{
    /// <summary>用于返回翻译结果</summary>
    [XmlRoot("Result")]
    public class TranslateResult
    {
        /// <summary>结果代码,为0表示正常,非0表示有异常,Message将提供详细的异常信息</summary>
        public int Status { get; set; }

        /// <summary>结果信息,如果发生异常,将包含异常信息</summary>
        public List<string> Messages { get; set; }

        /// <summary>翻译的输入参数,原始参数</summary>
        public TranslateParams Params { get; set; }

        /// <summary>输入文本的翻译结果</summary>
        public List<TextTrans> TextTranslations { get; set; }

        /// <summary>构造方法</summary>
        public TranslateResult() { }

        #region 相关容器类
        /// <summary>文本的翻译结果</summary>
        public class TextTrans
        {
            /// <summary>待翻译的原文</summary>
            public string Original { get; set; }
            /// <summary>文本的译文结果</summary>
            public List<Trans> Translations { get; set; }
            /// <summary>原文的每个单词翻译结果</summary>
            public List<Word> Words { get; set; }
            /// <summary>构造方法</summary>
            public TextTrans() { }
            /// <summary>构造方法</summary>
            /// <param name="Original"></param>
            public TextTrans(string Original)
            {
                this.Original = Original;
                Translations = new List<Trans>();
            }
        }
        /// <summary>单词的翻译结果</summary>
        public class Word
        {
            /// <summary>单词的原文</summary>
            public string Original { get; set; }
            /// <summary>这个单词的翻译结果,可能是多个可选的结果</summary>
            public List<Trans> Translations { get; set; }
            /// <summary>构造方法</summary>
            public Word() { }
            /// <summary>构造方法</summary>
            /// <param name="w"></param>
            public Word(string w)
            {
                Original = w;
                Translations = new List<Trans>();
            }
        }
        /// <summary>翻译条目,最终的翻译结果</summary>
        public class Trans
        {
            /// <summary>当前待翻译的原文,已经格式化的</summary>
            public string Original { get; set; }
            /// <summary>译文</summary>
            public string Text { get; set; }
            /// <summary>权值</summary>
            public int Weight { get; set; }
            /// <summary>翻译来源</summary>
            public string Source { get; set; }
            /// <summary>构造方法</summary>
            public Trans() { }
            /// <summary>设置翻译来源</summary>
            /// <param name="type">类型</param>
            /// <param name="args"></param>
            /// <returns></returns>
            public Trans SetSourceType(string type, string extstr)
            {
                Source = SourceTypes.Get(type, extstr);
                return this;
            }
            /// <summary>返回当前的翻译来源,不包含扩展信息</summary>
            /// <returns></returns>
            public string GetSourceType()
            {
                string t, p;
                if (SourceTypes.TryGetKnowType(Source, out t, out p))
                {
                    return t;
                }
                return Source;
            }
        }
        #endregion

        #region 工具类
        /// <summary>翻译的来源类型</summary>
        public class SourceTypes
        {
            private static string[] _KnowTypes;
            /// <summary>当前所有已知的来源类型</summary>
            public static string[] KnowTypes
            {
                get
                {
                    if (_KnowTypes == null)
                    {
                        List<string> types = new List<string>();
                        foreach (FieldInfo f in typeof(SourceTypes).GetFields())
                        {
                            if (f.IsPublic && f.IsStatic && f.IsLiteral && f.FieldType == typeof(string))
                            {
                                types.Add((string)FieldInfoX.Create(f).GetValue(null));
                            }
                        }
                        _KnowTypes = types.ToArray();
                    }
                    return _KnowTypes;
                }
            }

            /// <summary>手工翻译的</summary>
            public const string Manual = "Manual";
            /// <summary>
            /// 在线翻译服务的,一般会在后续描述明确的在线翻译服务类型
            /// 
            /// 比较应使用 IsTranslateWS 方法
            /// </summary>
            public const string TranslateWS = "TranslateWS";
            /// <summary>混合的翻译结果,多个词汇由不同的翻译来源合并得到的</summary>
            public const string Mixed = "Mixed";
            /// <summary>通过翻译条目添加接口添加的,一般会在后续描述明确的添加来源</summary>
            public const string TranslateNew = "TranslateNew";
            /// <summary>未翻译的</summary>
            public const string Untranslated = "Untranslated";

            /// <summary>尝试从指定字符串中解析出已知来源类型</summary>
            /// <param name="s"></param>
            /// <returns></returns>
            public static bool TryGetKnowType(string s, out string knowType, out string param)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    knowType = Array.Find<string>(KnowTypes, str => s.StartsWith(str, StringComparison.OrdinalIgnoreCase));
                    param = knowType != null ? s.Substring(knowType.Length) : null;
                    if (param != null) param = param.Trim();
                    return knowType != null;
                }
                param = knowType = null;
                return false;
            }
            /// <summary>返回指定字符串是否是某个已知的来源类型</summary>
            /// <param name="type">类型</param>
            /// <returns></returns>
            public static bool IsKnowType(string s)
            {
                string t, p;
                return TryGetKnowType(s, out t, out p);
            }
            /// <summary>返回指定字符串是否是指定的来源类型</summary>
            /// <param name="s"></param>
            /// <param name="type">类型</param>
            /// <returns></returns>
            public static bool Is(string s, string type)
            {
                if (s != null) s = s.Trim();
                if (string.IsNullOrEmpty(s)) return false;
                string t, p;
                if (TryGetKnowType(s, out t, out p))
                {
                    return t.Equals(type, StringComparison.OrdinalIgnoreCase);
                }
                TryGetUnknowType(s, out t, out p);
                return t != null && t.Equals(type, StringComparison.OrdinalIgnoreCase);
            }
            /// <summary>返回指定的来源类型,包含指定的扩展信息</summary>
            /// <param name="type">类型</param>
            /// <param name="s"></param>
            /// <returns></returns>
            public static string Get(string type, string s)
            {
                if (type != null) type = type.Trim();
                if (string.IsNullOrEmpty(type)) return type;
                string t, p;
                if (!TryGetKnowType(type, out t, out p))
                {
                    if (!TryGetUnknowType(type, out t, out p))
                    {
                        t = type;
                    }
                }
                string tt, pp;
                if (TryGetKnowType(s, out tt, out pp))
                {
                    t = t + " " + (pp != null ? pp.Trim() : null);
                }
                else
                {
                    t = t + " " + (s != null ? s.Trim() : null);
                }
                t = t.Trim();
                return t;
            }
            /// <summary>
            /// 格式化来源类型字符串,如果指定的字符串是已知的来源类型则自动修正大小写
            /// 
            /// 不能识别将返回null
            /// </summary>
            /// <param name="s"></param>
            /// <returns></returns>
            public static string FormatSourceType(string s)
            {
                string t, p;
                return TryGetKnowType(s, out t, out p) ? t :
                    TryGetUnknowType(s, out t, out p) ? t : null;
            }
            private static bool TryGetUnknowType(string s, out string type, out string param)
            {
                if (s != null) s = s.Trim();
                if (!string.IsNullOrEmpty(s))
                {
                    string[] ss = s.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    type = ss[0];
                    param = ss.Length > 1 ? ss[1] : null;
                    return true;
                }
                type = param = null;
                return false;
            }
        }
        #endregion
    }

    /// <summary>翻译服务的输入参数,原始参数格式</summary>
    public class TranslateParams
    {
        /// <summary>提交的文本</summary>
        public string Text { get; set; }
        /// <summary>提交文本是否是多个待翻译的文本,使用\u0000分割的</summary>
        public string MultiText { get; set; }
        /// <summary>是否是多个待翻译的文本,使用\u0000分割的 方便使用的方法</summary>
        public bool IsMultiText
        {
            get { return MultiText != null; }
        }
        /// <summary>请求的翻译类型</summary>
        public string Kind { get; set; }
        /// <summary>是否回显当前的输入参数</summary>
        public string Echo { get; set; }
        /// <summary>方便使用的方法</summary>
        public bool IsEcho
        {
            get { return Echo != null; }
        }
        /// <summary>是否始终产生多个单词合并的翻译结果.这个参数将会受OneTrans参数影响,如果已经存在一个翻译的话.</summary>
        public string Mix { get; set; }
        /// <summary>是否始终产生多个单词合并的翻译结果.这个参数将会受OneTrans参数影响,如果已经存在一个翻译的话.方便使用的方法</summary>
        public bool IsMix
        {
            get { return Mix != null; }
        }
        /// <summary>文本翻译结果是否只需要返回一个翻译条目,即TextTrans.Translations.Count&lt;=1</summary>
        public string OneTrans { get; set; }
        /// <summary>是否只需要返回一个翻译条目 方便使用的方法</summary>
        public bool IsOneTrans
        {
            get { return OneTrans != null; }
        }
        /// <summary>是否不需要输出文本每个单词的翻译结果</summary>
        public string NoWords { get; set; }
        /// <summary>是否不需要输出文本每个单词的翻译结果 方便使用的方法</summary>
        public bool IsNoWords
        {
            get { return NoWords != null; }
        }
        /// <summary>构造方法</summary>
        public TranslateParams() { }
    }

    /// <summary>用于提交翻译条目后返回结果</summary>
    [XmlRoot("Result")]
    public class TranslateNewResult
    {
        /// <summary>状态,0表示正常</summary>
        public int Status { get; set; }
        /// <summary>如果发生异常则会包含错误信息</summary>
        public string Message { get; set; }
        /// <summary>接受的条目数量</summary>
        public int Accepted { get; set; }
    }
}