using System;
using System.Collections.Generic;
using System.Text;

namespace XCoder
{
    interface ITranslate
    {
        String Translate(String word);

        String[] Translate(String[] words);
    }
}