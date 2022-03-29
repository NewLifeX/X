using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Configuration;

namespace Test
{
    public class ConfigTest : Config<ConfigTest>
    {
        public List<String> Names { get; set; }

        public String Sex { get; set; }

        public List<XYF> xyf { get; set; }
    }

    public class XYF
    {
        public String name { get; set; }

        //public List<String> gradu { get; set; }
    }
}
