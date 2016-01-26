using System;
using System.Text;
using System.Security.Permissions;
using System.Runtime.Serialization;

namespace NewLife.Security
{
    [Serializable]
    public sealed class BerDecodeException : Exception, ISerializable
    {
        private int m_position;
        public int Position
        { get { return m_position; } }

        public override string Message
        {
            get
            {
                StringBuilder sb = new StringBuilder(base.Message);

                sb.AppendFormat(" (Position {0}){1}",
                  m_position, Environment.NewLine);

                return sb.ToString();
            }
        }

        public BerDecodeException()
            : base() { }

        public BerDecodeException(String message)
            : base(message) { }

        public BerDecodeException(String message, Exception ex)
            : base(message, ex) { }

        public BerDecodeException(String message, int position)
            : base(message) { m_position = position; }

        public BerDecodeException(String message, int position, Exception ex)
            : base(message, ex) { m_position = position; }

        private BerDecodeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { m_position = info.GetInt32("Position"); }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Position", m_position);
        }
    }
}

