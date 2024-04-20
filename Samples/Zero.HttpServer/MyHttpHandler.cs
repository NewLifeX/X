using NewLife.Http;

namespace Zero.HttpServer;

/// <summary>自定义控制器。包含多个服务</summary>
internal class MyHttpHandler : IHttpHandler
{
    public void ProcessRequest(IHttpContext context)
    {
        // 获取请求参数
        var name = context.Parameters["name"];

        var html = $"<h2>你好，<span color=\"red\">{name}</span></h2>";

        // 支持文件上传
        var files = context.Request.Files;
        if (files != null && files.Length > 0)
        {
            foreach (var file in files)
            {
                file.SaveToFile();
                html += $"<br />文件：{file.FileName} 大小：{file.Length} 类型：{file.ContentType}";
            }
        }

        // 设置响应结果
        context.Response.SetResult(html);
    }
}