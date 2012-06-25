<%@ Application Language="C#" %>

<script RunAt="server">

    void Application_Start(object sender, EventArgs e)
    {
        //在应用程序启动时运行的代码

    }

    void Application_End(object sender, EventArgs e)
    {
        //在应用程序关闭时运行的代码

    }

    void Application_Error(object sender, EventArgs e)
    {
        //Exception ex = Server.GetLastError();
        //if (ex == null) return;
        //if (ex is System.Threading.ThreadAbortException) return;
        //if (ex is System.Security.Cryptography.CryptographicException && ex.Message.Contains("填充无效")) return;
        
        //String msg = ex.ToString();
        //msg += Environment.NewLine + String.Format("来源：{0}", Request.UserHostAddress);
        //msg += Environment.NewLine + String.Format("平台：{0}", Request.UserAgent);
        //msg += Environment.NewLine + String.Format("访问：{0}", Request.RawUrl);
        //msg += Environment.NewLine + String.Format("引用：{0}", Request.UrlReferrer);
        //NewLife.Log.XTrace.WriteLine(msg);

        //if (!NewLife.Log.XTrace.Debug)
        //{
        //    Server.ClearError();
        //    Response.Write("非常抱歉，服务器遇到错误，请与管理员联系！");
        //}
    }

    void Session_Start(object sender, EventArgs e)
    {
        //在新会话启动时运行的代码

    }

    void Session_End(object sender, EventArgs e)
    {
        //在会话结束时运行的代码。 
        // 注意: 只有在 Web.config 文件中的 sessionstate 模式设置为
        // InProc 时，才会引发 Session_End 事件。如果会话模式 
        //设置为 StateServer 或 SQLServer，则不会引发该事件。

    }
      
</script>

