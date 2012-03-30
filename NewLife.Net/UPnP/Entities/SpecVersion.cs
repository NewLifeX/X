using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.UPnP
{
    /// <summary>SpecVersion</summary>>
    public class SpecVersion
    {
        private Int32 _major;
        /// <summary>UPnP 设备架构主版本</summary>
        public Int32 major
        {
            get { return _major; }
            set { _major = value; }
        }

        private Int32 _minor;
        /// <summary>UPnP 设备架构副版本</summary>
        public Int32 minor
        {
            get { return _minor; }
            set { _minor = value; }
        }

    }
}
