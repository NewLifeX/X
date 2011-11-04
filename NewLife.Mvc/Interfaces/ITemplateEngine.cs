using System;
using System.Collections.Generic;

namespace NewLife.Mvc
{
    /// <summary>模版引擎接口</summary>
    public interface ITemplateEngine
    {
        /// <summary>模版引擎名称</summary>
        String Name { get; }

        /// <summary>
        /// 生成页面
        /// </summary>
        /// <param name="templateName">模版文件名</param>
        /// <param name="data">参数数据</param>
        /// <returns></returns>
        String Render(String templateName, IDictionary<String, Object> data);
    }
}