
namespace NewLife.Net.NTP
{
    /// <summary>Leap indicator</summary>
    public enum NTPLeapIndicator
    {
        /// <summary>0 - No warning</summary>
        NoWarning,

        /// <summary>1 - Last minute has 61 seconds</summary>
        LastMinute61,

        /// <summary>2 - Last minute has 59 seconds</summary>
        LastMinute59,

        /// <summary>3 - Alarm condition (clock not synchronized)</summary>
        Alarm
    }

    /// <summary>Mode</summary>
    public enum NTPMode
    {
        /// <summary>未知</summary>
        Reserved,

        /// <summary>1 - Symmetric active</summary>
        SymmetricActive,

        /// <summary>2 - Symmetric pasive</summary>
        SymmetricPassive,

        /// <summary>3 - Client</summary>
        Client,

        /// <summary>4 - Server</summary>
        Server,

        /// <summary>5 - Broadcast</summary>
        Broadcast,

        /// <summary>6 - reserved for NTP control message</summary>
        ReservedForNTPControlMessage,

        /// <summary>7 - reserved for private use</summary>
        ReservedForPrivateUse
    }

    /// <summary>Stratum</summary>
    public enum NTPStratum
    {
        /// <summary>0 - unspecified or unavailable</summary>
        Unspecified,

        /// <summary>1 - primary reference (e.g. radio-clock)</summary>
        PrimaryReference,

        /// <summary>2-15 - secondary reference (via NTP or SNTP)</summary>
        SecondaryReference,

        /// <summary>16-255 - reserved</summary>
        Reserved
    }
}