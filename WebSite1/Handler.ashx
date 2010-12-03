<%@ WebHandler Language="C#" Class="Handler" %>

using System;
using System.Web;
using NewLife.Web;
using NewLife.Messaging;

public class Handler : IHttpHandler {
    
    public void ProcessRequest (HttpContext context) {
        // 可以写一个HttpMessageHandler，然后Web.Config中配置映射即可
        HttpStream stream = new HttpStream(context);
        Message msg = Message.Deserialize(stream);
    }
 
    public bool IsReusable {
        get {
            return false;
        }
    }
}