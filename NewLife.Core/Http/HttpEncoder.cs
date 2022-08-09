using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Serialization;

namespace NewLife.Http;

/// <summary>Http编码器</summary>
public class HttpEncoder : EncoderBase, IEncoder
{
    #region 属性
    /// <summary>是否使用Http状态。默认false，使用json包装响应码</summary>
    public Boolean UseHttpStatus { get; set; }
    #endregion

    /// <summary>编码</summary>
    /// <param name="action"></param>
    /// <param name="code"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual Packet Encode(String action, Int32 code, Object value)
    {
        //if (value == null) return null;

        if (value is Packet pk) return pk;
        if (value is IAccessor acc) return acc.ToPacket();

        // 不支持序列化异常
        if (value is Exception ex) value = ex.GetTrue()?.Message;

        String json;
        if (UseHttpStatus)
            json = value.ToJson(false, false, false);
        else
            json = new { action, code, data = value }.ToJson(false, true, false);
        WriteLog("{0}=>{1}", action, json);

        return json.GetBytes();
    }

    /// <summary>解码参数</summary>
    /// <param name="action"></param>
    /// <param name="data"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    public virtual IDictionary<String, Object> DecodeParameters(String action, Packet data, IMessage msg)
    {
        /*
         * 数据内容解析需要根据http数据类型来判定使用什么格式处理
         * **/

        var str = data.ToStr();
        WriteLog("{0}<={1}", action, str);
        if (str.IsNullOrEmpty()) return null;

        var ctype = Array.Empty<String>();
        if (msg is HttpMessage hmsg && str[0] == '{')
        {
            if (hmsg.ParseHeaders()) ctype = (hmsg.Headers["Content-type"] + "").Split(';');
        }

        if (ctype.Contains("application/json"))
        {
            var dic = new JsonParser(str).Decode().ToDictionary();
            var rs = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in dic)
            {
                if (item.Value is String str2)
                    rs[item.Key] = HttpUtility.UrlDecode(str2);
                else
                    rs[item.Key] = item.Value;
            }

            return rs;
        }
        else
        {
            var dic = str.SplitAsDictionary("=", "&");
            var rs = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in dic)
            {
                rs[item.Key] = HttpUtility.UrlDecode(item.Value);
            }
            return rs;
        }
    }

    /// <summary>解码结果</summary>
    /// <param name="action"></param>
    /// <param name="data"></param>
    /// <param name="msg">消息</param>
    /// <returns></returns>
    public virtual Object DecodeResult(String action, Packet data, IMessage msg)
    {
        var json = data.ToStr();
        WriteLog("{0}<={1}", action, json);

        return new JsonParser(json).Decode();
    }

    /// <summary>转换为目标类型</summary>
    /// <param name="obj"></param>
    /// <param name="targetType"></param>
    /// <returns></returns>
    public virtual Object Convert(Object obj, Type targetType) => JsonHelper.Default.Convert(obj, targetType);

    #region 编码/解码
    /// <summary>创建请求</summary>
    /// <param name="action"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public virtual IMessage CreateRequest(String action, Object args)
    {
        // 请求方法 GET / HTTP/1.1
        var req = new HttpMessage();
        var sb = Pool.StringBuilder.Get();
        sb.Append("GET ");
        sb.Append(action);

        // 准备参数，二进制优先
        if (args is Packet pk)
        {
        }
        // 支持IAccessor
        else if (args is IAccessor acc)
            pk = acc.ToPacket();
        else if (args is Byte[] buf)
            pk = new Packet(buf);
        else
        {
            pk = null;

            // url参数
            sb.Append('?');
            if (args.GetType().GetTypeCode() != TypeCode.Object)
            {
                sb.Append(args);
            }
            else
            {
                foreach (var item in args.ToDictionary())
                {
                    sb.AppendFormat("{0}={1}", item.Key, item.Value);
                }
            }
        }
        sb.AppendLine(" HTTP/1.1");

        if (pk != null && pk.Total > 0)
        {
            sb.AppendFormat("Content-Length:{0}\r\n", pk.Total);
            sb.AppendLine("Content-Type:application/json");
        }
        sb.Append("Connection:keep-alive");

        req.Header = sb.Put(true).GetBytes();

        return req;
    }

    /// <summary>创建响应</summary>
    /// <param name="msg"></param>
    /// <param name="action"></param>
    /// <param name="code"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual IMessage CreateResponse(IMessage msg, String action, Int32 code, Object value)
    {
        if (code <= 0 && UseHttpStatus) code = 200;

        // 编码响应数据包，二进制优先
        var pk = Encode(action, code, value);

        // 构造响应消息
        var rs = new HttpMessage
        {
            Payload = pk
        };
        if (code >= 500) rs.Error = true;

        // HTTP/1.1 502 Bad Gateway
        var sb = Pool.StringBuilder.Get();
        sb.Append("HTTP/1.1 ");

        if (UseHttpStatus)
        {
            sb.Append(code);
            if (code < 500)
                sb.AppendLine(" OK");
            else
                sb.AppendLine(" Error");
        }
        else
        {
            sb.AppendLine("200 OK");
        }

        sb.AppendFormat("Content-Length:{0}\r\n", pk?.Total ?? 0);
        sb.AppendLine("Content-Type:application/json");
        sb.Append("Connection:keep-alive");

        rs.Header = sb.Put(true).GetBytes();

        return rs;
    }

    /// <summary>解码 请求/响应</summary>
    /// <param name="msg">消息</param>
    /// <param name="action">服务动作</param>
    /// <param name="code">错误码</param>
    /// <param name="value">参数或结果</param>
    /// <returns></returns>
    public override Boolean Decode(IMessage msg, out String action, out Int32 code, out Packet value)
    {
        action = null;
        code = 0;
        value = null;

        if (msg is not HttpMessage http) return false;

        // 分析请求方法 GET / HTTP/1.1
        var p = http.Header.IndexOf(new[] { (Byte)'\r', (Byte)'\n' });
        if (p <= 0) return false;

        var line = http.Header.ToStr(null, 0, p);

        var ss = line.Split(' ');
        if (ss.Length < 3) return false;

        // 第二段是地址
        var url = ss[1];
        p = url.IndexOf('?');
        if (p > 0)
        {
            action = url[1..p];
            value = url[(p + 1)..].GetBytes();
        }
        else
        {
            action = url[1..];
            value = http.Payload;
        }

        return true;
    }
    #endregion
}