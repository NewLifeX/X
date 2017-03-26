using System.Collections.Generic;
using System.Net;


namespace System
{
    /// <summary>网络结点扩展</summary>
    public static class EndPointExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static String ToAddress(this EndPoint endpoint)
        {
            return ((IPEndPoint)endpoint).ToAddress();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static String ToAddress(this IPEndPoint endpoint)
        {
            return string.Format("{0}:{1}", endpoint.Address, endpoint.Port);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static IPEndPoint ToEndPoint(this String address)
        {
            var array = address.Split(new String[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            if (array.Length != 2)
            {
                throw new Exception("Invalid endpoint address: " + address);
            }
            var ip = IPAddress.Parse(array[0]);
            var port = int.Parse(array[1]);
            return new IPEndPoint(ip, port);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="addresses"></param>
        /// <returns></returns>
        public static IEnumerable<IPEndPoint> ToEndPoints(this String addresses)
        {
            var array = addresses.Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<IPEndPoint>();
            foreach (var item in array)
            {
                list.Add(item.ToEndPoint());
            }
            return list;
        }
    }
}