using System;

namespace XCoder
{
    interface ITranslate
    {
        String Translate(String word);

        String[] Translate(String[] words);

        /// <summary>向翻译服务添加新的翻译条目</summary>
        /// <param name="trans"></param>
        /// <returns></returns>
        int TranslateNew(string Kind, params string[] trans);
    }
}