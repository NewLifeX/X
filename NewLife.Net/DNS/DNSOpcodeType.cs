using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.DNS
{
    /// <summary>
    /// The Query Types (OPCODE) that specifies kind of query in a message.
    /// (RFC 1035 4.1.1 and 1002 4.2.1.1)
    /// </summary>
    public enum DNSOpcodeType : byte
    {
        /// <summary>
        /// A standard query (QUERY); used for NetBIOS, too
        /// </summary>
        Query = 0,

        /// <summary>
        /// An inverse query (IQUERY)
        /// </summary>
        InverseQuery = 1,

        /// <summary>
        /// A server status request (STATUS)
        /// </summary>
        Status = 2,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        [Obsolete("Reserved for future use.")]
        Reserverd3 = 3,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        [Obsolete("Reserved for future use.")]
        Reserverd4 = 4,

        /// <summary>
        /// NetBIOS registration
        /// </summary>
        Registration = 5,

        /// <summary>
        /// NetBIOS release
        /// </summary>
        Release = 6,

        /// <summary>
        /// NetBIOS WACK
        /// </summary>
        WACK = 7,

        /// <summary>
        /// NetBIOS refresh
        /// </summary>
        Refresh = 8,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        [Obsolete("Reserved for future use.")]
        Reserverd9 = 9,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        [Obsolete("Reserved for future use.")]
        Reserverd10 = 10,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        [Obsolete("Reserved for future use.")]
        Reserverd11 = 11,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        [Obsolete("Reserved for future use.")]
        Reserverd12 = 12,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        [Obsolete("Reserved for future use.")]
        Reserverd13 = 13,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        [Obsolete("Reserved for future use.")]
        Reserverd14 = 14,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        [Obsolete("Reserved for future use.")]
        Reserverd15 = 15
    }
}