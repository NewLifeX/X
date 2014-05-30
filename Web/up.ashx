<%@ WebHandler Language="C#" Class="up" %>

using System;
using System.IO;
using System.Web;
using NewLife.Log;

public class up : IHttpHandler {
    
    public void ProcessRequest (HttpContext context) {
        HttpRequest Request = context.Request;
        HttpResponse Response = context.Response;

        XTrace.WriteLine(Request.UserHostAddress);
        if (Request.HttpMethod == "POST")
        {
            Stream ms = Request.InputStream;
            String str = IOHelper.ToStr(ms, null);
            XTrace.WriteLine(str);

            Response.Write("GPS+GPRS OK!");
            Response.End();
        }
        else
        {
            XTrace.WriteLine(Request.HttpMethod);
        }

        Response.Write(DateTime.Now.ToString());
    }
 
    public bool IsReusable {
        get {
            return false;
        }
    }

}