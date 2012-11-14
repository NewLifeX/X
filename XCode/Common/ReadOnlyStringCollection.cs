using System;
using NewLife.Linq;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace XCode.Common
{
    class ReadOnlyStringCollection : ReadOnlyCollection<String>, IList<String>
    {
        Boolean ICollection<String>.Contains(String value)
        {
            if (base.Contains(value)) return true;

            return this.Contains(value, StringComparer.OrdinalIgnoreCase);
        }
    }
}