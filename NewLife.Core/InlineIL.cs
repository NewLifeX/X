using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife
{
//#if DEBUG
    class InlineIL
    {
        static void Main()
        {
            int x = 3;
            int y = 4;
            int z = 5;

            // Here's some inline IL
#if IL
        ldloc x
        ldloc y
        add
        ldloc z
        add
        stloc x
#endif
            Console.WriteLine(x + y + z);
        }
    }
//#endif
}