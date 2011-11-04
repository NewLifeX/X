using System;
using System.Collections.Generic;
using NewLife.Reflection;

namespace NewLife.Mvc
{
    /// <summary>一般模版引擎XTemplate</summary>
    /// <remarks>
    /// 默认通过快速反射调用XTemplate.Templating.Template.ProcessFile。
    /// 可以通过重写Render改为调用XTemplate.Templating.Template.ProcessFile.ProcessTemplate，实现支持数据库模版等第三方模版源。
    /// </remarks>
    public class GenericTemplateEngine : ITemplateEngine
    {
        #region ITemplateEngine 成员

        /// <summary>名称</summary>
        public virtual string Name { get { return "XTemplate"; } }

        private Type _EngineType;

        /// <summary>XTemplate模版引擎类型</summary>
        public virtual Type EngineType
        {
            get
            {
                if (_EngineType == null) _EngineType = TypeX.GetType("XTemplate.Templating.Template", true);
                return _EngineType;
            }
            set { _EngineType = value; }
        }

        /// <summary>
        /// 生成页面。
        /// </summary>
        /// <param name="templateName">模版文件名</param>
        /// <param name="data">参数数据</param>
        /// <returns></returns>
        public virtual string Render(string templateName, IDictionary<string, object> data)
        {
            if (EngineType == null)
            {
                throw new Exception("没有引用模版引擎,默认是XTemplate");
            }

            MethodInfoX method = MethodInfoX.Create(EngineType, "ProcessFile");
            String html = (String)method.Invoke(null, templateName, data);

            return html;
        }

        #endregion ITemplateEngine 成员
    }
}