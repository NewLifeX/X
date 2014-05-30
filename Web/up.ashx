<%@ WebHandler Language="C#" Class="up" %>

using System;
using System.IO;
using System.Web;
using NewLife.Log;

public class up : IHttpHandler
{

    public void ProcessRequest(HttpContext context)
    {
        HttpRequest Request = context.Request;
        HttpResponse Response = context.Response;

        XTrace.WriteLine(Request.UserHostAddress);
        if (Request.HttpMethod == "POST")
        {
            Stream ms = Request.InputStream;
            String str = IOHelper.ToStr(ms, null);
            XTrace.WriteLine(str);

            // 分离
            String[] ss = StringHelper.Split(Environment.NewLine);
            foreach (String item in ss)
            {
                if (item.StartsWith("$GPRMC,"))
                {
                    String[] ss2 = StringHelper.Split(",");
                    if (ss2[2] == "A")
                    {
                        WriteLine("经度：{0}{1}", Double.Parse(ss2[5]) / 100, ss2[6]);
                        WriteLine("纬度：{0}{1}", Double.Parse(ss2[3]) / 100, ss2[4]);
                        WriteLine("速度：{0}{1}", ss2[7]);
                    }
                }
            }

            Response.Write("GPS+GPRS OK!");
            Response.End();
        }
        else
        {
            XTrace.WriteLine(Request.HttpMethod);
        }

        Response.Write(DateTime.Now.ToString());
    }

    public void WriteLine(String format, params Object[] args)
    {
        XTrace.WriteLine(format, args);
        HttpContext.Current.Response.Write(String.Format(format, args) + "<br />" + Environment.NewLine);
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

}