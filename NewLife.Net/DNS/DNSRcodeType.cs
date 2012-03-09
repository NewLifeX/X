using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.DNS
{
    /// <summary>
    /// These are the return codes (RCODE) the server can send back. (RFC 1035 4.1.1)
    /// </summary>
    public enum DNSRcodeType
    {
        /// <summary>
        /// No error condition
        /// </summary>
        Success = 0,

        /// <summary>
        /// The name server was unable to interpret the query.
        /// </summary>
        FormatError = 1,

        /// <summary>
        /// The name server was unable to process this query due to a problem 
        /// with the name server.
        /// </summary>
        ServerFailure = 2,

        /// <summary>
        /// Meaningful only for responses from an authoritative name server, 
        /// this code signifies that the domain name referenced in the query 
        /// does not exist.
        /// </summary>
        NameError = 3,

        /// <summary>
        /// The name server does not support the requested kind of query.
        /// </summary>
        NotImplemented = 4,

        /// <summary>
        /// The name server refuses to perform the specified operation for 
        /// policy reasons.  For example, a name server may not wish to provide 
        /// the information to the particular requester, or a name server may 
        /// not wish to perform a particular operation (e.g., zone transfer) 
        /// for particular data.
        /// </summary>
        Refused = 5,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        Reserverd6 = 6,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        Reserverd7 = 7,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        Reserverd8 = 8,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        Reserverd9 = 9,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        Reserverd10 = 10,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        Reserverd11 = 11,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        Reserverd12 = 12,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        Reserverd13 = 13,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        Reserverd14 = 14,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        Reserverd15 = 15
    }
}
