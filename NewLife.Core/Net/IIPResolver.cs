using System;
using System.Net;

namespace NewLife.Net;

/// <summary>IP地址提供者</summary>
public interface IIPResolver
{
    /// <summary>获取IP地址的物理地址位置</summary>
    /// <param name="addr"></param>
    /// <returns></returns>
    String GetAddress(IPAddress addr);
}