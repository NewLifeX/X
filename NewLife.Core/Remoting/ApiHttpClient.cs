using System;
using NewLife.Net;

namespace NewLife.Remoting
{
    class ApiHttpClient : ApiNetClient
    {
        //public WebClientX Client { get; set; }

        //public String Remote { get; set; }

        public override bool Init(object config)
        {
            var url = config as string;
            if (url.IsNullOrEmpty()) return false;

            var uri = config as NetUri;
            if (uri != null)
                Client = uri.CreateRemote();
            else if (config is Uri)
                Client = ((Uri) config).CreateRemote();
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