using System;
using NewLife.Queue.Utilities;

namespace NewLife.Queue.Protocols
{
    [Serializable]
    public class Message
    {
        public string Topic { get; set; }
        public string Tag { get; set; }
        public int Code { get; set; }
        public byte[] Body { get; set; }
        public DateTime CreatedTime { get; set; }

        public Message() { }
        public Message(string topic, int code, byte[] body, string tag = null) : this(topic, code, body, DateTime.Now, tag) { }
        public Message(string topic, int code, byte[] body, DateTime createdTime, string tag = null)
        {
            Ensure.NotNull(topic, "topic");
            Ensure.Positive(code, "code");
            Ensure.NotNull(body, "body");
            Topic = topic;
            Tag = tag;
            Code = code;
            Body = body;
            CreatedTime = createdTime;
        }

        public override string ToString()
        {
            return string.Format("[Topic={0},Code={1},Tag={2},CreatedTime={3},BodyLength={4}]", Topic, Code, Tag, CreatedTime, Body.Length);
        }
    }
}
