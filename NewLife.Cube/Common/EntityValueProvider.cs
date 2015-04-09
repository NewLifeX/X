using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace NewLife.Cube.Common
{
    /// <summary>实体对象的值提供程序</summary>
    public class EntityValueProvider : NameValueCollectionValueProvider
    {
        internal EntityValueProvider(ControllerContext controllerContext)
            : base(controllerContext.HttpContext.Request.Form, controllerContext.HttpContext.Request.Unvalidated().Form, CultureInfo.CurrentCulture)
        {
        }
    }

    /// <summary>表示用来创建值提供程序对象的工厂</summary>
    public class EntityValueProviderFactory : ValueProviderFactory
    {
        /// <summary>为指定控制器上下文返回值提供程序对象</summary>
        /// <param name="controllerContext"></param>
        /// <returns></returns>
        public override IValueProvider GetValueProvider(ControllerContext controllerContext)
        {
            if (controllerContext == null) throw new ArgumentNullException("controllerContext");

            return new EntityValueProvider(controllerContext);
        }
    }
}