using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace NewLife.Cube.Admin
{
    /// <summary>自定义视图引擎。为了让系统优先查找当前区域目录</summary>
    public class RazorViewEngineX : RazorViewEngine
    {
        #region 属性
        //private HashSet<String> _Paths = new HashSet<String>(StringComparer.OrdinalIgnoreCase) { "Views" };
        ///// <summary>扩展搜索的附属路径</summary>
        //public HashSet<String> Paths { get { return _Paths; } set { _Paths = value; } }
        #endregion

        #region 构造
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
        #endregion

        #region 方法
        ///// <summary>添加要搜索的视图目录</summary>
        ///// <param name="path"></param>
        //public void AddPath(String path)
        //{
        //    if (String.IsNullOrEmpty(path)) return;

        //    path = path.Trim('/');

        //    if (Paths.Contains(path)) return;
        //    Paths.Add(path);

        //    var list = new List<String>();
        //    foreach (var item in Paths.Reverse())
        //    {
        //        list.Add("~/" + item + "/{1}/{0}.cshtml");
        //        list.Add("~/" + item + "/Shared/{0}.cshtml");
        //    }

        //    var arr = list.ToArray();
        //    ViewLocationFormats = arr;
        //    MasterLocationFormats = arr;
        //    PartialViewLocationFormats = arr;
        //}

        //public override ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        //{
        //    return base.FindView(controllerContext, viewName, masterName, useCache);
        //}

        /// <summary>注册需要搜索的目录路径</summary>
        /// <param name="name"></param>
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

            //ve.AddPath(name);
        }
        #endregion
    }
}