//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Net;
//using System.Net.Sockets;
//using System.Runtime.InteropServices;

//namespace NewLife.Net.Application
//{
//    /// <summary>
//    /// Leap indicator
//    /// </summary>
//    public enum _LeapIndicator
//    {
//        NoWarning,        // 0 - No warning
//        LastMinute61,    // 1 - Last minute has 61 seconds
//        LastMinute59,    // 2 - Last minute has 59 seconds
//        Alarm            // 3 - Alarm condition (clock not synchronized)
//    }

//    /// <summary>
//    /// Mode
//    /// </summary>
//    public enum _Mode
//    {
//        SymmetricActive,    // 1 - Symmetric active
//        SymmetricPassive,    // 2 - Symmetric pasive
//        Client,                // 3 - Client
//        Server,                // 4 - Server
//        Broadcast,            // 5 - Broadcast
//        Unknown                // 0, 6, 7 - Reserved
//    }

//    /// <summary>
//    /// Stratum
//    /// </summary>
//    public enum _Stratum
//    {
//        Unspecified,            // 0 - unspecified or unavailable
//        PrimaryReference,        // 1 - primary reference (e.g. radio-clock)
//        SecondaryReference,        // 2-15 - secondary reference (via NTP or SNTP)
//        Reserved                // 16-255 - reserved
//    }

//    /// <summary>
//    /// NTPClient is a C# class designed to connect to time servers on the Internet.
//    /// The implementation of the protocol is based on the RFC 2030.
//    /// 
//    /// -----------------------------------------------------------------------------
//    /// Structure of the standard NTP header (as described in RFC 2030)
//    ///                       1                   2                   3
//    ///   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
//    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//    ///  |LI | VN  |Mode |    Stratum    |     Poll      |   Precision   |
//    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//    ///  |                          Root Delay                           |
//    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//    ///  |                       Root Dispersion                         |
//    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//    ///  |                     Reference Identifier                      |
//    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//    ///  |                                                               |
//    ///  |                   Reference Timestamp (64)                    |
//    ///  |                                                               |
//    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//    ///  |                                                               |
//    ///  |                   Originate Timestamp (64)                    |
//    ///  |                                                               |
//    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//    ///  |                                                               |
//    ///  |                    Receive Timestamp (64)                     |
//    ///  |                                                               |
//    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//    ///  |                                                               |
//    ///  |                    Transmit Timestamp (64)                    |
//    ///  |                                                               |
//    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//    ///  |                 Key Identifier (optional) (32)                |
//    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//    ///  |                                                               |
//    ///  |                                                               |
//    ///  |                 Message Digest (optional) (128)               |
//    ///  |                                                               |
//    ///  |                                                               |
//    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//    /// 
//    /// -----------------------------------------------------------------------------
//    /// 
//    /// NTP Timestamp Format (as described in RFC 2030)
//    ///                         1                   2                   3
//    ///     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
//    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//    /// |                           Seconds                             |
//    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//    /// |                  Seconds Fraction (0-padded)                  |
//    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
//    /// 
//    /// </summary>
//    public class NTPClient
//    {
//        // NTP Data Structure Length
//        private const byte NTPDataLength = 48;
//        // NTP Data Structure (as described in RFC 2030)
//        byte[] NTPData = new byte[NTPDataLength];

//        // Offset constants for timestamps in the data structure
//        private const byte offReferenceID = 12;
//        private const byte offReferenceTimestamp = 16;
//        private const byte offOriginateTimestamp = 24;
//        private const byte offReceiveTimestamp = 32;
//        private const byte offTransmitTimestamp = 40;

//        /// <summary>
//        /// Warns of an impending leap second to be inserted/deleted in the last minute of the current day. (See the _LeapIndicator enum)
//        /// </summary>
//        public _LeapIndicator LeapIndicator
//        {
//            get
//            {
//                // Isolate the two most significant bits
//                byte val = (byte)(NTPData[0] >> 6);
//                switch (val)
//                {
//                    case 0: return _LeapIndicator.NoWarning;
//                    case 1: return _LeapIndicator.LastMinute61;
//                    case 2: return _LeapIndicator.LastMinute59;
//                    case 3: goto default;
//                    default:
//                        return _LeapIndicator.Alarm;
//                }
//            }
//        }

//        /// <summary>
//        /// Version number of the protocol (3 or 4).
//        /// </summary>
//        public byte VersionNumber
//        {
//            get
//            {
//                // Isolate bits 3 - 5
//                byte val = (byte)((NTPData[0] & 0x38) >> 3);
//                return val;
//            }
//        }

//        /// <summary>
//        /// Returns mode. (See the _Mode enum)
//        /// </summary>
//        public _Mode Mode
//        {
//            get
//            {
//                // Isolate bits 0 - 3
//                byte val = (byte)(NTPData[0] & 0x7);
//                switch (val)
//                {
//                    case 0: goto default;
//                    case 6: goto default;
//                    case 7: goto default;
//                    default:
//                        return _Mode.Unknown;
//                    case 1:
//                        return _Mode.SymmetricActive;
//                    case 2:
//                        return _Mode.SymmetricPassive;
//                    case 3:
//                        return _Mode.Client;
//                    case 4:
//                        return _Mode.Server;
//                    case 5:
//                        return _Mode.Broadcast;
//                }
//            }
//        }

//        /// <summary>
//        /// Stratum of the clock. (See the _Stratum enum)
//        /// </summary>
//        public _Stratum Stratum
//        {
//            get
//            {
//                byte val = (byte)NTPData[1];
//                if (val == 0) return _Stratum.Unspecified;
//                else
//                    if (val == 1) return _Stratum.PrimaryReference;
//                    else
//                        if (val <= 15) return _Stratum.SecondaryReference;
//                        else
//                            return _Stratum.Reserved;
//            }
//        }

//        /// <summary>
//        /// Maximum interval between successive messages.
//        /// </summary>
//        public uint PollInterval
//        {
//            get
//            {
//                return (uint)Math.Round(Math.Pow(2, NTPData[2]));
//            }
//        }

//        /// <summary>
//        /// Precision of the clock.
//        /// </summary>
//        public double Precision
//        {
//            get
//            {
//                return (1000 * Math.Pow(2, NTPData[3]));
//            }
//        }

//        /// <summary>
//        /// Round trip time to the primary reference source.
//        /// </summary>
//        public double RootDelay
//        {
//            get
//            {
//                int temp = 0;
//                temp = 256 * (256 * (256 * NTPData[4] + NTPData[5]) + NTPData[6]) + NTPData[7];
//                return 1000 * (((double)temp) / 0x10000);
//            }
//        }

//        /// <summary>
//        /// Nominal error relative to the primary reference source.
//        /// </summary>
//        public double RootDispersion
//        {
//            get
//            {
//                int temp = 0;
//                temp = 256 * (256 * (256 * NTPData[8] + NTPData[9]) + NTPData[10]) + NTPData[11];
//                return 1000 * (((double)temp) / 0x10000);
//            }
//        }

//        /// <summary>
//        /// Reference identifier (either a 4 character string or an IP address).
//        /// </summary>
//        public string ReferenceID
//        {
//            get
//            {
//                string val = "";
//                switch (Stratum)
//                {
//                    case _Stratum.Unspecified:
//                        goto case _Stratum.PrimaryReference;
//                    case _Stratum.PrimaryReference:
//                        val += (char)NTPData[offReferenceID + 0];
//                        val += (char)NTPData[offReferenceID + 1];
//                        val += (char)NTPData[offReferenceID + 2];
//                        val += (char)NTPData[offReferenceID + 3];
//                        break;
//                    case _Stratum.SecondaryReference:
//                        switch (VersionNumber)
//                        {
//                            case 3:    // Version 3, Reference ID is an IPv4 address
//                                string Address = NTPData[offReferenceID + 0].ToString() + "." +
//                                                 NTPData[offReferenceID + 1].ToString() + "." +
//                                                 NTPData[offReferenceID + 2].ToString() + "." +
//                                                 NTPData[offReferenceID + 3].ToString();
//                                try
//                                {
//                                    IPHostEntry Host = Dns.GetHostByAddress(Address);
//                                    val = Host.HostName + " (" + Address + ")";
//                                }
//                                catch (Exception)
//                                {
//                                    val = "N/A";
//                                }
//                                break;
//                            case 4: // Version 4, Reference ID is the timestamp of last update
//                                DateTime time = ComputeDate(GetMilliSeconds(offReferenceID));
//                                // Take care of the time zone
//                                TimeSpan offspan = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
//                                val = (time + offspan).ToString();
//                                break;
//                            default:
//                                val = "N/A";
//                                break;
//                        }
//                        break;
//                }

//                return val;
//            }
//        }

//        /// <summary>
//        /// The time at which the clock was last set or corrected.
//        /// </summary>
//        public DateTime ReferenceTimestamp
//        {
//            get
//            {
//                DateTime time = ComputeDate(GetMilliSeconds(offReferenceTimestamp));
//                // Take care of the time zone
//                TimeSpan offspan = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
//                return time + offspan;
//            }
//        }

//        /// <summary>
//        /// The time at which the request departed the client for the server.
//        /// </summary>
//        public DateTime OriginateTimestamp
//        {
//            get
//            {
//                return ComputeDate(GetMilliSeconds(offOriginateTimestamp));
//            }
//        }

//        /// <summary>
//        /// The time at which the request arrived at the server.
//        /// </summary>
//        public DateTime ReceiveTimestamp
//        {
//            get
//            {
//                DateTime time = ComputeDate(GetMilliSeconds(offReceiveTimestamp));
//                // Take care of the time zone
//                TimeSpan offspan = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
//                return time + offspan;
//            }
//        }

//        /// <summary>
//        /// The time at which the reply departed the server for client.
//        /// </summary>
//        public DateTime TransmitTimestamp
//        {
//            get
//            {
//                DateTime time = ComputeDate(GetMilliSeconds(offTransmitTimestamp));
//                // Take care of the time zone
//                TimeSpan offspan = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
//                return time + offspan;
//            }
//            set
//            {
//                SetDate(offTransmitTimestamp, value);
//            }
//        }

//        /// <summary>
//        /// ReceptionTimestamp
//        /// </summary>
//        public DateTime ReceptionTimestamp;

//        /// <summary>
//        /// The time between the departure of request and arrival of reply.
//        /// </summary>
//        public int RoundTripDelay
//        {
//            get
//            {
//                TimeSpan span = (ReceiveTimestamp - OriginateTimestamp) + (ReceptionTimestamp - TransmitTimestamp);
//                return (int)span.TotalMilliseconds;
//            }
//        }

//        /// <summary>
//        /// Local clock offset (in milliseconds)
//        /// </summary>
//        public int LocalClockOffset
//        {
//            get
//            {
//                TimeSpan span = (ReceiveTimestamp - OriginateTimestamp) - (ReceptionTimestamp - TransmitTimestamp);
//                return (int)(span.TotalMilliseconds / 2);
//            }
//        }

//        // Compute date, given the number of milliseconds since January 1, 1900
//        private DateTime ComputeDate(ulong milliseconds)
//        {
//            TimeSpan span = TimeSpan.FromMilliseconds((double)milliseconds);
//            DateTime time = new DateTime(1900, 1, 1);
//            time += span;
//            return time;
//        }

//        // Compute the number of milliseconds, given the offset of a 8-byte array
//        private ulong GetMilliSeconds(byte offset)
//        {
//            ulong intpart = 0, fractpart = 0;

//            for (int i = 0; i <= 3; i++)
//            {
//                intpart = 256 * intpart + NTPData[offset + i];
//            }
//            for (int i = 4; i <= 7; i++)
//            {
//                fractpart = 256 * fractpart + NTPData[offset + i];
//            }
//            ulong milliseconds = intpart * 1000 + (fractpart * 1000) / 0x100000000L;
//            return milliseconds;
//        }

//        // Compute the 8-byte array, given the date
//        private void SetDate(byte offset, DateTime date)
//        {
//            ulong intpart = 0, fractpart = 0;
//            DateTime StartOfCentury = new DateTime(1900, 1, 1, 0, 0, 0);    // January 1, 1900 12:00 AM

//            ulong milliseconds = (ulong)(date - StartOfCentury).TotalMilliseconds;
//            intpart = milliseconds / 1000;
//            fractpart = ((milliseconds % 1000) * 0x100000000L) / 1000;

//            ulong temp = intpart;
//            for (int i = 3; i >= 0; i--)
//            {
//                NTPData[offset + i] = (byte)(temp % 256);
//                temp = temp / 256;
//            }

//            temp = fractpart;
//            for (int i = 7; i >= 4; i--)
//            {
//                NTPData[offset + i] = (byte)(temp % 256);
//                temp = temp / 256;
//            }
//        }

//        /// <summary>
//        /// Sets up data structure and prepares for connection.
//        /// </summary>
//        private void Initialize()
//        {
//            // Set version number to 4 and Mode to 3 (client)
//            NTPData[0] = 0x1B;
//            // Initialize all other fields with 0
//            for (int i = 1; i < 48; i++)
//            {
//                NTPData[i] = 0;
//            }
//            // Initialize the transmit timestamp
//            TransmitTimestamp = DateTime.Now;
//        }

//        /// <summary>
//        /// 实例化
//        /// </summary>
//        /// <param name="host"></param>
//        public NTPClient(string host)
//        {
//            TimeServer = host;
//        }

//        /// <summary>
//        /// Connects to the time server and populates the data structure.It can also set the system time.
//        /// </summary>
//        /// <param name="UpdateSystemTime"></param>
//        public void Connect(bool UpdateSystemTime)
//        {
//            try
//            {
//                // Resolve server address
//                IPHostEntry hostadd = Dns.Resolve(TimeServer);
//                IPEndPoint EPhost = new IPEndPoint(hostadd.AddressList[0], 123);

//                //Connect the time server
//                UdpClient TimeSocket = new UdpClient();
//                TimeSocket.Connect(EPhost);

//                // Initialize data structure
//                Initialize();
//                TimeSocket.Send(NTPData, NTPData.Length);
//                NTPData = TimeSocket.Receive(ref EPhost);
//                if (!IsResponseValid())
//                {
//                    throw new Exception("Invalid response from " + TimeServer);
//                }
//                ReceptionTimestamp = DateTime.Now;
//            }
//            catch (SocketException e)
//            {
//                throw new Exception(e.Message);
//            }

//            // Update system time
//            if (UpdateSystemTime)
//            {
//                SetTime();
//            }
//        }

//        /// <summary>
//        /// Returns true if received data is valid and if comes from a NTP-compliant time server.
//        /// </summary>
//        /// <returns></returns>
//        public bool IsResponseValid()
//        {
//            if (NTPData.Length < NTPDataLength || Mode != _Mode.Server)
//            {
//                return false;
//            }
//            else
//            {
//                return true;
//            }
//        }

//        /// <summary>
//        /// Returns a string representation of the object.
//        /// </summary>
//        /// <returns></returns>
//        public override string ToString()
//        {
//            string str;

//            str = "Leap Indicator: ";
//            switch (LeapIndicator)
//            {
//                case _LeapIndicator.NoWarning:
//                    str += "No warning";
//                    break;
//                case _LeapIndicator.LastMinute61:
//                    str += "Last minute has 61 seconds";
//                    break;
//                case _LeapIndicator.LastMinute59:
//                    str += "Last minute has 59 seconds";
//                    break;
//                case _LeapIndicator.Alarm:
//                    str += "Alarm Condition (clock not synchronized)";
//                    break;
//            }
//            str += "\r\nVersion number: " + VersionNumber.ToString() + "\r\n";
//            str += "Mode: ";
//            switch (Mode)
//            {
//                case _Mode.Unknown:
//                    str += "Unknown";
//                    break;
//                case _Mode.SymmetricActive:
//                    str += "Symmetric Active";
//                    break;
//                case _Mode.SymmetricPassive:
//                    str += "Symmetric Pasive";
//                    break;
//                case _Mode.Client:
//                    str += "Client";
//                    break;
//                case _Mode.Server:
//                    str += "Server";
//                    break;
//                case _Mode.Broadcast:
//                    str += "Broadcast";
//                    break;
//            }
//            str += "\r\nStratum: ";
//            switch (Stratum)
//            {
//                case _Stratum.Unspecified:
//                case _Stratum.Reserved:
//                    str += "Unspecified";
//                    break;
//                case _Stratum.PrimaryReference:
//                    str += "Primary Reference";
//                    break;
//                case _Stratum.SecondaryReference:
//                    str += "Secondary Reference";
//                    break;
//            }
//            str += "\r\nLocal time: " + TransmitTimestamp.ToString();
//            str += "\r\nPrecision: " + Precision.ToString() + " ms";
//            str += "\r\nPoll Interval: " + PollInterval.ToString() + " s";
//            str += "\r\nReference ID: " + ReferenceID.ToString();
//            str += "\r\nRoot Dispersion: " + RootDispersion.ToString() + " ms";
//            str += "\r\nRound Trip Delay: " + RoundTripDelay.ToString() + " ms";
//            str += "\r\nLocal Clock Offset: " + LocalClockOffset.ToString() + " ms";
//            str += "\r\n";

//            return str;
//        }

//        /// <summary>
//        /// 系统时间结构体
//        /// </summary>
//        [StructLayoutAttribute(LayoutKind.Sequential)]
//        private struct SYSTEMTIME
//        {
//            public short year;
//            public short month;
//            public short dayOfWeek;
//            public short day;
//            public short hour;
//            public short minute;
//            public short second;
//            public short milliseconds;
//        }

//        [DllImport("kernel32.dll")]
//        static extern bool SetLocalTime(ref SYSTEMTIME time);


//        // Set system time according to transmit timestamp
//        private void SetTime()
//        {
//            SYSTEMTIME st;

//            DateTime trts = TransmitTimestamp;
//            st.year = (short)trts.Year;
//            st.month = (short)trts.Month;
//            st.dayOfWeek = (short)trts.DayOfWeek;
//            st.day = (short)trts.Day;
//            st.hour = (short)trts.Hour;
//            st.minute = (short)trts.Minute;
//            st.second = (short)trts.Second;
//            st.milliseconds = (short)trts.Millisecond;

//            SetLocalTime(ref st);
//        }

//        // The URL of the time server we're connecting to
//        private string TimeServer;
//    }
//}