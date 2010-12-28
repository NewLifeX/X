using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.ComponentModel;
using XUrlRewrite.Configuration;

namespace XUrlRewrite.Entities
{
    /// <summary>
    /// 模板配置业务包装类,主要用于前端aspx中使用数据绑定
    /// </summary>
    [DataObject]
    [Description("模板配置业务包装")]
    public class ConfigWrap
    {
        delegate Boolean UrlElementTypeFilter(String type);
        static Boolean NormalFilter(String type)
        {
            return type.Trim().Equals("normal", StringComparison.OrdinalIgnoreCase);
        }

        static Boolean RegexpFilter(String type)
        {
            type = type.Trim();
            return type.Equals("regex", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("regexp", StringComparison.OrdinalIgnoreCase);
        }

        static Boolean DefaultAllFilter(String type)
        {
            return true;
        }

        static Dictionary<String, UrlElementTypeFilter> _Filters = null;
        static Dictionary<String, UrlElementTypeFilter> Filters
        {
            get
            {
                if (_Filters == null)
                {
                    _Filters = new Dictionary<String, UrlElementTypeFilter>();
                    _Filters["normal"] = NormalFilter;
                    _Filters["regexp"] = RegexpFilter;
                    _Filters["regex"] = RegexpFilter;
                }
                return _Filters;
            }
        }

        /// <summary>
        /// 查找指定应用的模板配置
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select)]
        public static UrlRewriteConfig FindTemplateConfig(HttpApplication app)
        {
            return Manager.GetConfig(app);

        }
        /// <summary>
        /// 查找指定应用,指定类型的模板Url映射配置
        /// </summary>
        /// <param name="app"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select)]
        public static List<UrlElement> FindAllUrlElements(HttpApplication app, String type)
        {
            List<UrlElement> ret = null;
            UrlRewriteConfig cfg = Manager.GetConfig(app);
            if (null != cfg)
            {
                ret = new List<UrlElement>(cfg.Urls.Count);
                UrlElementTypeFilter filter = Filters.ContainsKey(type.Trim()) ? Filters[type.Trim()] : DefaultAllFilter;

                foreach (UrlElement _url in cfg.Urls)
                {
                    //if (_url is UrlElement)
                    {
                        UrlElement url = (UrlElement)_url;
                        if (filter(url.Type))
                        {
                            ret.Add(url);
                        }
                    }
                }
            }
            if (ret != null && ret.Count == 0)
            {
                ret = null;
            }
            return ret;
        }
        /// <summary>
        /// 查找指定应用,指定url的Url映射配置
        /// </summary>
        /// <param name="app"></param>
        /// <param name="urlkey"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select)]
        public static UrlElement FindUrlElements(HttpApplication app, String urlkey)
        {
            UrlElement ret = null;
            if (String.IsNullOrEmpty(urlkey))
            {
                ret = new UrlElement();
                ret.Type = "regexp";
                ret.Enabled = true;
                ret.RegexFlag = "";
                ret.IgnoreCase = true;
            }
            else
            {
                UrlRewriteConfig cfg = Manager.GetConfig(app);
                if (null != cfg)
                {
                    foreach (Object _url in cfg.Urls)
                    {
                        if (_url is UrlElement)
                        {
                            UrlElement url = (UrlElement)_url;
                            if (url.Url == urlkey)
                            {
                                ret = url;
                                break;
                            }
                        }
                    }
                }

            }
            return ret;
        }
        /// <summary>
        /// 更新指定Url映射配置(包含添加或修改)
        /// TODO 暂时为当前应用,因为数据绑定不方便传递多参数
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Update)]
        public static UrlElement UpdateUrlElement(UrlElement url)
        {
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            UrlElement updateUrlElement = null;

            if (String.IsNullOrEmpty(url.UpdateKey))
            {
                updateUrlElement = FindUrlElements(app, url.Url);
                if (updateUrlElement != null)
                {
                    throw new Exception(String.Format("添加失败! 已存在Url为{0}的配置", url.Url));
                }
                updateUrlElement = url;
                Manager.GetConfig(app).Urls.Add(updateUrlElement);
            }
            else
            {
                updateUrlElement = FindUrlElements(app, url.UpdateKey);
                if (updateUrlElement == null)
                {
                    throw new Exception(String.Format("修改失败! Url为{0}的规则不存在,可能已被删除", url.UpdateKey));
                }
                updateUrlElement.Enabled = url.Enabled;
                updateUrlElement.Url = url.Url;
                updateUrlElement.To = url.To;
                updateUrlElement.Type = url.Type;
                if (url.Type == "normal")
                {
                    updateUrlElement.IgnoreCase = url.IgnoreCase;
                }
                else if (url.Type == "regexp")
                {
                    updateUrlElement.RegexFlag = url.RegexFlag;
                }
            }
            // Manager.GetConfigManager(app).Save(); //保存
            return updateUrlElement;
        }
        /// <summary>
        /// 删除指定Url的Url映射配置
        /// </summary>
        /// <param name="url"></param>
        [DataObjectMethod(DataObjectMethodType.Delete)]
        public static void RemoveUrlElement(UrlElement url)
        {
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            UrlRewriteConfig cfg = Manager.GetConfig(app);
            UrlCollection urls = cfg.Urls;
            for (Int32 i = 0; i < urls.Count; i++)
            {
                if (urls.Get(i).Url == url.Url)
                {
                    urls.RemoveAt(i);
                    break;
                }
            }

            // Manager.GetConfigManager(app).Save(); //保存

        }
        /// <summary>
        /// 移动指定应用,指定Url映射配置的顺序,这样会影响到其优先级,越向上的优先级越高
        /// 
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="url"></param>
        /// <param name="moveby">移动偏移量,-1即向上1位,+1即向下1位,如果超出则移动到第一位或最后一位</param>
        /// <returns></returns>
        public static Int32 MoveUrlElement(HttpApplication app, UrlElement url, Int32 moveby)
        {
            UrlRewriteConfig cfg = Manager.GetConfig(app);
            UrlCollection urls = cfg.Urls;
            Int32 moveindex = -1;
            UrlElement moveelement = null;
            for (Int32 i = 0; i < urls.Count; i++)
            {
                moveelement = urls.Get(i);
                if (moveelement.Url == url.Url)
                {
                    moveindex = i;
                    break;
                }
            }
            if (moveindex > -1)
            {
                urls.RemoveAt(moveindex);

                moveindex = moveindex + moveby;
                if (moveindex < 0)
                {
                    moveindex = 0;
                    urls.Add(moveindex, moveelement);
                }
                else if (moveindex >= urls.Count)
                {
                    moveindex = urls.Count;
                    urls.Add(moveindex, moveelement);
                }
                else
                {
                    urls.Add(moveindex, moveelement);
                }
            }
            return moveindex;
        }
        /// <summary>
        /// 获取指定应用的模板配置最后修改时间
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static DateTime GetLastWriteDateTime(HttpApplication app)
        {
            return Manager.GetConfigManager(app).LastWriteTime;
        }
        /// <summary>
        /// 保存指定应用的模板配置到配置文件中
        /// </summary>
        /// <param name="app"></param>
        public static void Save(HttpApplication app)
        {
            Manager.GetConfigManager(app).Save();
        }
        /// <summary>
        /// 查找指定应用的模板配置
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        [Obsolete("使用FindTemplateConfig代替此方法")]
        [DataObjectMethod(DataObjectMethodType.Select)]
        public static UrlRewriteConfig GetConfig(HttpApplication app)
        {
            return Manager.GetConfig(app);
        }
        /// <summary>
        /// 修改当前应用的模板配置,主要包括全局开关和模板文件目录
        /// </summary>
        /// <param name="config"></param>
        [DataObjectMethod(DataObjectMethodType.Update)]
        public static void UpdateConfig(UrlRewriteConfig config)
        {
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            UrlRewriteConfig cfg = Manager.GetConfig(app);
            cfg.Enabled = config.Enabled;
            cfg.Directory = config.Directory;
        }
        /// <summary>
        /// 重新从配置文件中加载指定引用的模板配置
        /// </summary>
        /// <param name="app"></param>
        public static void Reload(HttpApplication app)
        {
            Manager.GetConfigManager(app).NeedForReload = true;
        }
    }
}
