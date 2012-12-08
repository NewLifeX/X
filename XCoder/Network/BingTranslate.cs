//using XCoder.com.microsofttranslator.api;

namespace XCoder
{
    //class BingTranslate : ITranslate
    //{
    //    String appId = "A63C992CE2E44371559E8385947BBCDE758F7B10";
    //    String url = "http://api.microsofttranslator.com/V2/soap.svc";

    //    #region ITranslate 成员
    //    DictionaryCache<String, String> _cache = new DictionaryCache<String, String>();

    //    public string Translate(string word)
    //    {
    //        return _cache.GetItem(word, delegate(String key)
    //        {
    //            SoapService client = new SoapService();
    //            client.Url = url;
    //            return client.Translate(appId, key, "en", "zh-cn", "text/plain", "general");
    //        });
    //    }

    //    public string[] Translate(string[] words)
    //    {
    //        SoapService client = new SoapService();
    //        client.Url = url;
    //        TranslateOptions options = new TranslateOptions();
    //        TranslateArrayResponse[] rs = client.TranslateArray(appId, words, "en", "zh-cn", options);
    //        if (rs == null || rs.Length < 1) return null;

    //        String[] arr = new String[rs.Length];
    //        for (int i = 0; i < rs.Length; i++)
    //        {
    //            if (String.IsNullOrEmpty(rs[i].Error))
    //            {
    //                arr[i] = rs[i].TranslatedText;
    //                if (!_cache.ContainsKey(words[i])) _cache.Add(words[i], arr[i]);
    //            }
    //        }
    //        return arr;
    //    }
    //    #endregion
    //}
}