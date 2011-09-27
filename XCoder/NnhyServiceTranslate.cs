using System;
using System.IO;
using System.Net;
using System.Text;
using NewLife.ServiceLib;
using NewLife.Xml;
using TextTrans = NewLife.ServiceLib.TranslateResult.TextTrans;
using System.Web;

namespace XCoder
{
    /// <summary>
    /// 使用s.nnhy.org的翻译服务翻译指定词汇
    /// </summary>
    class NnhyServiceTranslate : ITranslate
    {
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
            WebClient web = new WebClient();
            StringBuilder url = new StringBuilder("http://s.nnhy.org/Translate.ashx?");
            string text = multi ? string.Join("\u0000", words) : words[0];
            string args = (multi ? "&MultiText=" : "") + "&OneTrans=&NoWords=";
            url.AppendFormat("Text={0}&Kind={1}{2}", HttpUtility.UrlEncode(text), "1", args);
            TranslateResult result = null;
            try
            {
                byte[] buffer = web.DownloadData(url.ToString());
                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    XmlReaderX reader = new XmlReaderX();
                    reader.Stream = stream;
                    result = reader.ReadObject(typeof(TranslateResult)) as TranslateResult;
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
    }
}
