using System;
using System.Web;
using NewLife.Exceptions;

namespace NewLife.Web
{
    /// <summary>子目录HttpModule加载模块。用于加载子目录web.config中配置的IHttpModule。</summary>
    /// <remarks>
    /// 将来的系统设计将会朝着模块化的方向发展，每个模块一个独立的子目录，最好包括配置文件的所有内容也一起在里面
    /// </remarks>
    public class HttpModuleLoader : IHttpModule
    {
        #region IHttpModule Members
        void IHttpModule.Dispose() { }

        /// <summary>初始化模块，准备拦截请求。</summary>
        /// <param name="context"></param>
        void IHttpModule.Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(context_BeginRequest);
        }

        void context_BeginRequest(object sender, EventArgs e)
        {
            Process();
        }
        #endregion

        #region 加载
        /// <summary>根据路径判断是否加载</summary>
        protected virtual void Process()
        {
            //todo 根据路径判断是否加载
            throw new XException("本功能未完成！");
        }
        #endregion
    }
}