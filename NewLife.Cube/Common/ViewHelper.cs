using System;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XCode.Configuration;
using System.Web;

namespace NewLife.Cube
{
    /// <summary>视图助手</summary>
    public static class ViewHelper
    {
        /// <summary>创建页面设置的委托</summary>
        public static Func<Bootstrap> CreateBootstrap = () => new Bootstrap();

        /// <summary>获取页面设置</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Bootstrap Bootstrap(this HttpContextBase context)
        {
            var bs = context.Items["Bootstrap"] as Bootstrap;
            if (bs == null)
            {
                bs = CreateBootstrap();
                context.Items["Bootstrap"] = bs;
            }

            return bs;
        }

        /// <summary>获取页面设置</summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public static Bootstrap Bootstrap(this WebViewPage page)
        {
            return Bootstrap(page.Context);
        }

        /// <summary>获取页面设置</summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        public static Bootstrap Bootstrap(this Controller controller)
        {
            return Bootstrap(controller.HttpContext);
        }
    }

    /// <summary>Bootstrap页面控制。允许继承</summary>
    public class Bootstrap
    {
        #region 属性
        /// <summary>最大列数</summary>
        public Int32 MaxColumn { get; set; } = 2;

        /// <summary>默认标签宽度</summary>
        public Int32 LabelWidth { get; set; } = 4;
        #endregion

        #region 当前项
        ///// <summary>当前项</summary>
        //public FieldItem Item { get; set; }

        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>类型</summary>
        public Type Type { get; set; }

        /// <summary>长度</summary>
        public Int32 Length { get; set; }

        /// <summary>设置项</summary>
        public void Set(FieldItem item)
        {
            Name = item.Name;
            Type = item.Type;
            Length = item.Length;
        }
        #endregion

        #region 方法
        /// <summary>获取分组宽度</summary>
        /// <returns></returns>
        public virtual Int32 GetGroupWidth()
        {
            if (MaxColumn > 1 && Type != null)
            {
                if (Type != typeof(String) || Length <= 100) return 12 / MaxColumn;
            }

            return 12;
        }
        #endregion
    }
}