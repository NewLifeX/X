using System;

namespace XCoder
{
    interface ITranslate
    {
        String Translate(String word);

        String[] Translate(String[] words);
    }
}