using System;
using System.Collections.Generic;
using System.Linq;

namespace NewLife.Queue.Protocols
{
    public class RemotingResponse
    {
        public short RequestType { get; set; }
        public short RequestCode { get; set; }
        public long RequestSequence { get; set; }
        public DateTime RequestTime { get; set; }
        public IDictionary<string, string> RequestHeader { get; set; }
        public short ResponseCode { get; set; }
        public byte[] ResponseBody { get; set; }
        public DateTime ResponseTime { get; set; }
        public IDictionary<string, string> ResponseHeader { get; set; }

        public RemotingResponse() { }
        public RemotingResponse(short requestType, short requestCode, long requestSequence, DateTime requestTime, short responseCode, byte[] responseBody, DateTime responseTime, IDictionary<string, string> requestHeader, IDictionary<string, string> responseHeader)
        {
            RequestType = requestType;
            RequestCode = requestCode;
            RequestSequence = requestSequence;
            RequestTime = requestTime;
            ResponseCode = responseCode;
            ResponseBody = responseBody;
            ResponseTime = responseTime;
            RequestHeader = requestHeader;
            ResponseHeader = responseHeader;
        }

        public override string ToString()
        {
            var responseBodyLength = 0;
            if (ResponseBody != null)
            {
                responseBodyLength = ResponseBody.Length;
            }
            var requestTime = RequestTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var responseTime = ResponseTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var requestHeader = string.Empty;
            if (RequestHeader != null && RequestHeader.Count > 0)
            {
                requestHeader = string.Join(",", RequestHeader.Select(x => string.Format("{0}:{1}", x.Key, x.Value)));
            }
            var responseHeader = string.Empty;
            if (ResponseHeader != null && ResponseHeader.Count > 0)
            {
                responseHeader = string.Join(",", ResponseHeader.Select(x => string.Format("{0}:{1}", x.Key, x.Value)));
            }

            return string.Format("[RequestType:{0}, RequestCode:{1}, RequestSequence:{2}, RequestTime:{3}, RequestHeader: [{4}], ResponseCode:{5}, ResponseTime:{6}, ResponseBodyLength:{7}, ResponseHeader: [{8}]]",
                RequestType, RequestCode, RequestSequence, requestTime, requestHeader, ResponseCode, responseTime, responseBodyLength, responseHeader);
        }
    }
}
