using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace NewLife.Cube.Admin
{
    /// <summary>自定义视图引擎。为了让系统优先查找当前区域目录</summary>
    public class RazorViewEngineX : RazorViewEngine
    {
        /// <summary>实例化，修改Areas搜索逻辑</summary>
        public RazorViewEngineX()
        {
            var list = new List<String>();
            list.Add("~/{2}/Views/{1}/{0}.cshtml");
            list.Add("~/{2}/Views/Shared/{0}.cshtml");
            list.Add("~/Areas/{2}/Views/{1}/{0}.cshtml");
            list.Add("~/Areas/{2}/Views/Shared/{0}.cshtml");

            var arr = list.ToArray();
            AreaViewLocationFormats = arr;
            AreaMasterLocationFormats = arr;
            AreaPartialViewLocationFormats = arr;
        }

        /// <summary>注册需要搜索的目录路径</summary>
        /// <param name="engines"></param>
        public static void Register(ViewEngineCollection engines)
        {
            // 如果没有注册，则注册
            var ve = engines.FirstOrDefault(e => e is RazorViewEngineX) as RazorViewEngineX;
            if (ve == null)
            {
                // 干掉旧引擎，使用新引擎
                var ve2 = engines.FirstOrDefault(e => e is RazorViewEngine);
                engines.Remove(ve2);

                ve = new RazorViewEngineX();
                engines.Insert(0, ve);
            }
        }
    }
}