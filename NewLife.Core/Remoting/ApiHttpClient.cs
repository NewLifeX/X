using System;
using NewLife.Net;

namespace NewLife.Remoting
{
    class ApiHttpClient : ApiNetClient
    {
        //public WebClientX Client { get; set; }

        //public String Remote { get; set; }

        public override Boolean Init(Object config)
        {
            var url = config as String;
            if (url.IsNullOrEmpty()) return false;

            if (config is NetUri)
                Client = (config as NetUri).CreateRemote();
            else if (config is Uri)
                Client = (config as Uri).CreateRemote();
            Remote = url;

            return true;
        }

        //public void Open()
        //{
        //}

        //public void Close()
        //{
        //}

        ///// <summary>发送数据</summary>
        ///// <param name="data"></param>
        ///// <returns></returns>
        //public Task<Byte[]> SendAsync(Byte[] data)
        //{
        //    return Client.UploadDataTaskAsync(Remote, data);
        //}


        //#region 日志
        ///// <summary>日志</summary>
        //public ILog Log { get; set; } = Logger.Null;
        //#endregion
    }
}