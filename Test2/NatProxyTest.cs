using System;
using System.Linq;
using NewLife.Net;
using NewLife.Net.Proxy;

namespace Test2
{
    public class NatProxyTest
    {
        private NATProxy proxy;


        public int LocalPort { get; set; }

        public string RemoteHost { get; set; }

        public int RemotePort { get; set; }

        public NatProxyTest(string remoteHost, int remotePort)
        {
            this.RemoteHost = remoteHost;
            this.RemotePort = remotePort;
            this.LocalPort = 8000;
        }

        public void Init()
        {
            proxy = new NATProxy(RemoteHost, RemotePort);
            proxy.Port = this.LocalPort;
            proxy.Servers.ForEach(ser => ser.MaxNotActive = 0);
        }

        public void Start()
        {
            proxy.Start();
        }

        public void Stop()
        {
            proxy.Stop();
        }
    }
}