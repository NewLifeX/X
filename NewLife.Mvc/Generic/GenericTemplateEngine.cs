using System;
using System.Collections.Generic;
using NewLife.Exceptions;
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

        /// <summary>生成页面</summary>
        /// <param name="templateName">模版文件名</param>
        /// <param name="data">参数数据</param>
        /// <returns></returns>
        public virtual string Render(string templateName, IDictionary<string, object> data)
        {
            if (EngineType == null) throw new XException("没有找到模版引擎{0}！", Name);

            MethodInfoX method = MethodInfoX.Create(EngineType, "ProcessFile");
            String html = (String)method.Invoke(null, templateName, data);

            return html;
        }

        ///// <summary>生成页面</summary>
        ///// <param name="templates">模版集合</param>
        ///// <param name="data">参数数据</param>
        ///// <returns></returns>
        //public virtual String Render(IDictionary<String, String> templates, IDictionary<String, Object> data)
        //{
        //    if (EngineType == null) throw new XException("没有找到模版引擎{0}！", Name);

        //    MethodInfoX create = MethodInfoX.Create(EngineType, "Create", new Type[] { typeof(IDictionary<String, String>) });
        //    if (create == null) throw new XException("模版引擎版本错误，未找到Create方法！");
        //    MethodInfoX render = MethodInfoX.Create(EngineType, "Render", new Type[] { typeof(String), typeof(IDictionary<String, Object>) });
        //    if (render == null) throw new XException("模版引擎版本错误，未找到Render方法！");

        //    // 创建引擎
        //    Object engine = create.Invoke(null, templates);
        //    // 调用Render，模版名传入null，会采用第一个模版
        //    String html = (String)render.Invoke(engine, null, data);

        //    return html;
        //}

        #endregion ITemplateEngine 成员
    }
}