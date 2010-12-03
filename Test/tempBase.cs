using System;
using System.Collections.Generic;
using System.Text;
using XTemplate.Templating;

namespace Test2
{
    public class tempBase : TemplateBase
    {
        private String _Name;
        /// <summary>名称</summary>
        public String Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Data.ContainsKey("Name")) Name = (String)Data["Name"];
        }
    }
}
