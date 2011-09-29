using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using NewLife.ServiceLib;
using NewLife.Xml;
using TextTrans = NewLife.ServiceLib.TranslateResult.TextTrans;

namespace XCoder
{
    /// <summary>
    /// 使用s.nnhy.org的翻译服务翻译指定词汇
    /// </summary>
    class NnhyServiceTranslate : ITranslate
    {

        static string UrlPrefix = "http://s.nnhy.org";

        public string Translate(string word)
        {
            string[] ret = Translate(new string[] { word });
            if (ret != null && ret.Length > 0)
            {
                return ret[0];
            }
            return null;
        }

        public string[] Translate(string[] words)
        {
            if (words == null || words.Length == 0) return null;
            bool multi = words.Length > 1;
            string text = multi ? string.Join("\u0000", words) : words[0];

            StringBuilder url = new StringBuilder(UrlPrefix);
            url.AppendFormat("/Translate.ashx?Text={0}&Kind={1}&OneTrans=&NoWords=", HttpUtility.UrlEncode(text), "1");
            if (multi)
            {
                url.Append("&MultiText=");
            }
            TranslateResult result = null;
            try
            {
                using (WebClient web = new WebClient())
                {
                    byte[] buffer = web.DownloadData(url.ToString());
                    using (MemoryStream stream = new MemoryStream(buffer))
                    {
                        XmlReaderX reader = new XmlReaderX();
                        reader.Stream = stream;
                        result = reader.ReadObject(typeof(TranslateResult)) as TranslateResult;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("访问在线翻译服务时发生了异常", ex);
            }
            if (result.Status == 0)
            {
                if (result.TextTranslations != null && result.TextTranslations.Count > 0)
                {
                    string[] ret = result.TextTranslations.ConvertAll<string>(delegate(TextTrans t)
                    {
                        if (t.Translations != null && t.Translations.Count > 0)
                        {
                            return t.Translations[0].Text;
                        }
                        return t.Original;
                    }).ToArray();
                    if (ret != null && ret.Length == 0) ret = null;
                    return ret;
                }
            }
            return null;
        }
        /// <summary>
        /// 向翻译服务添加新的翻译条目
        /// </summary>
        /// <param name="trans"></param>
        /// <returns></returns>
        public int TranslateNew(string Kind, params string[] trans)
        {
            return TranslateNewWithSource(Kind, "XCoder.exe", trans);
        }
        /// <summary>
        /// 向翻译服务添加新的翻译条目
        /// </summary>
        /// <param name="Kind"></param>
        /// <param name="Source"></param>
        /// <param name="trans"></param>
        /// <returns></returns>
        public int TranslateNewWithSource(string Kind, string Source, params string[] trans)
        {
            if (Kind != null) Kind = Kind.Trim();
            if (string.IsNullOrEmpty(Kind)) throw new ArgumentException("翻译类型不能为空", "Kind");
            if (trans == null || trans.Length == 0) return 0;
            if ((trans.Length & 1) == 1)
            {
                throw new Exception("翻译条目不是成对的,条目数量必须是2的倍数");
            }

#if DEBUG
            UrlPrefix = "http://localhost:9005/Web";
#endif

            string url = UrlPrefix + string.Format("/TranslateNew.ashx?Kind={0}&Source={1}", HttpUtility.UrlEncode(Kind), HttpUtility.UrlEncode(Source));

            StringBuilder data = new StringBuilder();
            for (int i = 0; i < trans.Length; i += 2)
            {
                string o = trans[i], t = trans[i + 1];
                if (!string.IsNullOrEmpty(o) && !string.IsNullOrEmpty(t))
                {
                    data.AppendFormat("&O={0}&T={1}", HttpUtility.UrlEncode(o), HttpUtility.UrlEncode(t));
                }
            }
            if (data.Length > 1)
            {
                data.Remove(0, 1);
            }
            else if (data.Length == 0)
            {
                throw new Exception("没有可添加的翻译条目");
            }

            WebClient web = new WebClient();
            web.Encoding = Encoding.UTF8;
            web.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            web.UploadString(url, data.ToString());
            // TODO 是否需要按需要抛出异常

            return trans.Length;
        }
    }
}
