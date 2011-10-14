using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Model;
using XCode.DataAccessLayer;

namespace XCode
{
    class Cctor
    {
        public static void Init()
        {
        }

        public static void Finish()
        {
            ObjectContainer.Current.Register<IDataTable, XTable>();
        }
    }
}