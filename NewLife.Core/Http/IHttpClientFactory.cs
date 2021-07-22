using System;
using System.Net.Http;

namespace NewLife.Http
{
    /// <summary>HttpClient工厂</summary>
    public interface IHttpClientFactory
    {
        /// <summary>创建HttpClient</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        HttpClient CreateClient(String name);
    }
}