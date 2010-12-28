使用方法:

在Web网站项目中 web.config 的configuration - > system.web -> httpModules 增加

      <add name="HttpModule" type="XUrlRewrite.Helper.HttpModule, XUrlRewrite"/>

然后在Web网站项目中创建 ~/UrlRewrite.config 文件,内容如下

<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <section name="urlRewriteConfig" type="XUrlRewrite.Configuration.UrlRewriteConfig, XUrlRewrite"/>
  </configSections>
  <!-- directory是重写目标的默认路径,如果不使用应该写~/,那么下面to都应该是相对于这个路径 -->
  <urlRewriteConfig directory="~/Template" enabled="true">
    <urls>
	  <!--
	    下面这行是错误的,只是用来描述参数
		url是来源 to是重写的目标,可以包含?用来包含query参数
		type是类型 有normal regex(别名 regexp),normal即普通,regex即正则,这会影响url匹配的规则
		regexFlag type="regex"时生效,定义正则的额外选项,可选的值有i IgnoreCase, c CultureInvariant, m Multiline, s Singleline,其中m 和s单选一
		ignoreCase type="normal"时生效,是否大小写敏感
		enabled 是否生效
	  -->
	  <!-- <add url="^/foo.aspx" to="/bar.aspx?foo=bar" type="normal" regexFlag="icms" ignoreCase="true" enabled="true"> -->

      <add url="^/(\w+)\.aspx" to="/$1.aspx"/>
    </urls>
  </urlRewriteConfig>
</configuration>

----------------------------------------------------------------

改变WebForm的action

建议写到自己的Page基类中

        private RewriteHelper _RewriteHelper;
        /// <summary>
        /// 重写地址辅助工具,用于得到真实的地址信息,以及将相对于模板目录的路径转换为Web访问可用的Url
        /// </summary>
        protected RewriteHelper RewriteHelper
        {
            get
            {
                if (_RewriteHelper == null)
                {
                    _RewriteHelper = RewriteHelper.Current;
                    Page.Load += delegate(object sender, EventArgs e)
                    {
                        Page.Form.Action = RewriteHelper.FormAction;
                    };
                }
                return _RewriteHelper;
            }
        }

以及确保至少在Page.Load触发前访问一次RewriteHelper,比如
        protected override void InitializeCulture()
		{
			RewriteHelper.Current.ToString();
		}

RewriteHelper类型包含一些实用方法,用于获取真实客户端访问的地址,以及提供实用的转换地址方法

-----------------------------------------------------------------------

在WebForm中修改Rewrite信息

配置信息通过 XUrlRewrite.Entities.ConfigWrap 得到

模板文件通过 XUrlRewrite.Entities.FileManager 得到

