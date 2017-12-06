using NewLife.Log;
using NewLife.Model;
using NewLife.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace NewLife.Cube
{
    /// <summary>
    ///提示
    /// </summary>
    public class WeuiJS : Js
    {
        static WeuiJS()
        {
            Current = ObjectContainer.Current.AutoRegister<IJs, WeuiJS>().Resolve<IJs>();

            if (Current == null)
                Current = new Js();
            else if (XTrace.Debug && Current.GetType() != typeof(WeuiJS))
                XTrace.WriteLine("Js提供者：{0}", Current.GetType());
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static String Toast(String message)
        {
            return "$.toast(\"" + Encode(message) + "\");";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static String Alert(String message)
        {
            return "$.alert(\"" + Encode(message) + "\");";
        }
    }
}