<%@ WebHandler Language="C#" Class="Message2" %>

using System;
using System.Web;
using NewLife.IO;
using NewLife.Messaging;
using NewLife.Remoting;

public class Message2 : NewLife.IO.StreamHttpHandler
{
    static Message2()
    {
        RemotingMessageHandler.Init();
    }

    public override string GetName(HttpContext context)
    {
        return "Message";
    }
}