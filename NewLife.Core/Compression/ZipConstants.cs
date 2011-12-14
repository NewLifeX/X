using System;
using System.Text;
using System.Threading;

namespace NewLife.Compression
{
    #region Enumerations

    ///// <summary>
    ///// Determines how entries are tested to see if they should use Zip64 extensions or not.
    ///// </summary>
    //public enum UseZip64
    //{
    //    /// <summary>
    //    /// Zip64 will not be forced on entries during processing.
    //    /// </summary>
    //    /// <remarks>An entry can have this overridden if required <see cref="ZipEntry.ForceZip64"></see></remarks>
    //    Off,
    //    /// <summary>
    //    /// Zip64 should always be used.
    //    /// </summary>
    //    On,
    //    /// <summary>
    //    /// #ZipLib will determine use based on entry values when added to archive.
    //    /// </summary>
    //    Dynamic,
    //}

    /// <summary>
    /// The kind of compression used for an entry in an archive
    /// </summary>
    public enum CompressionMethod
    {
        /// <summary>
        /// A direct copy of the file contents is held in the archive
        /// </summary>
        Stored = 0,

        /// <summary>
        /// Common Zip compression method using a sliding dictionary
        /// of up to 32KB and secondary compression from Huffman/Shannon-Fano trees
        /// </summary>
        Deflated = 8,

        /// <summary>
        /// An extension to deflate with a 64KB window. Not supported by #Zip currently
        /// </summary>
        Deflate64 = 9,

        /// <summary>
        /// BZip2 compression. Not supported by #Zip.
        /// </summary>
        BZip2 = 11,

        /// <summary>
        /// WinZip special for AES encryption, Now supported by #Zip.
        /// </summary>
        WinZipAES = 99,
    }

    /// <summary>
    /// Identifies the encryption algorithm used for an entry
    /// </summary>
    public enum EncryptionAlgorithm
    {
        /// <summary>
        /// No encryption has been used.
        /// </summary>
        None = 0,
        /// <summary>
        /// Encrypted using PKZIP 2.0 or 'classic' encryption.
        /// </summary>
        PkzipClassic = 1,
        /// <summary>
        /// DES encryption has been used.
        /// </summary>
        Des = 0x6601,
        /// <summary>
        /// RCS encryption has been used for encryption.
        /// </summary>
        RC2 = 0x6602,
        /// <summary>
        /// Triple DES encryption with 168 bit keys has been used for this entry.
        /// </summary>
        TripleDes168 = 0x6603,
        /// <summary>
        /// Triple DES with 112 bit keys has been used for this entry.
        /// </summary>
        TripleDes112 = 0x6609,
        /// <summary>
        /// AES 128 has been used for encryption.
        /// </summary>
        Aes128 = 0x660e,
        /// <summary>
        /// AES 192 has been used for encryption.
        /// </summary>
        Aes192 = 0x660f,
        /// <summary>
        /// AES 256 has been used for encryption.
        /// </summary>
        Aes256 = 0x6610,
        /// <summary>
        /// RC2 corrected has been used for encryption.
        /// </summary>
        RC2Corrected = 0x6702,
        /// <summary>
        /// Blowfish has been used for encryption.
        /// </summary>
        Blowfish = 0x6720,
        /// <summary>
        /// Twofish has been used for encryption.
        /// </summary>
        Twofish = 0x6721,
        /// <summary>
        /// RC4 has been used for encryption.
        /// </summary>
        RC4 = 0x6801,
        /// <summary>
        /// An unknown algorithm has been used for encryption.
        /// </summary>
        Unknown = 0xffff
    }

    /// <summary>
    /// Defines the contents of the general bit flags field for an archive entry.
    /// </summary>
    [Flags]
    public enum GeneralBitFlags : int
    {
        /// <summary>
        /// Bit 0 if set indicates that the file is encrypted
        /// </summary>
        Encrypted = 0x0001,
        /// <summary>
        /// Bits 1 and 2 - Two bits defining the compression method (only for Method 6 Imploding and 8,9 Deflating)
        /// </summary>
        Method = 0x0006,
        /// <summary>
        /// Bit 3 if set indicates a trailing data desciptor is appended to the entry data
        /// </summary>
        Descriptor = 0x0008,
        /// <summary>
        /// Bit 4 is reserved for use with method 8 for enhanced deflation
        /// </summary>
        ReservedPKware4 = 0x0010,
        /// <summary>
        /// Bit 5 if set indicates the file contains Pkzip compressed patched data.
        /// Requires version 2.7 or greater.
        /// </summary>
        Patched = 0x0020,
        /// <summary>
        /// Bit 6 if set indicates strong encryption has been used for this entry.
        /// </summary>
        StrongEncryption = 0x0040,
        /// <summary>
        /// Bit 7 is currently unused
        /// </summary>
        Unused7 = 0x0080,
        /// <summary>
        /// Bit 8 is currently unused
        /// </summary>
        Unused8 = 0x0100,
        /// <summary>
        /// Bit 9 is currently unused
        /// </summary>
        Unused9 = 0x0200,
        /// <summary>
        /// Bit 10 is currently unused
        /// </summary>
        Unused10 = 0x0400,
        /// <summary>
        /// Bit 11 if set indicates the filename and
        /// comment fields for this file must be encoded using UTF-8.
        /// </summary>
        UnicodeText = 0x0800,
        /// <summary>
        /// Bit 12 is documented as being reserved by PKware for enhanced compression.
        /// </summary>
        EnhancedCompress = 0x1000,
        /// <summary>
        /// Bit 13 if set indicates that values in the local header are masked to hide
        /// their actual values, and the central directory is encrypted.
        /// </summary>
        /// <remarks>
        /// Used when encrypting the central directory contents.
        /// </remarks>
        HeaderMasked = 0x2000,
        /// <summary>
        /// Bit 14 is documented as being reserved for use by PKware
        /// </summary>
        ReservedPkware14 = 0x4000,
        /// <summary>
        /// Bit 15 is documented as being reserved for use by PKware
        /// </summary>
        ReservedPkware15 = 0x8000
    }

    #endregion

    static class ZipConstants
    {
        public const UInt32 PackedToRemovableMedia = 0x30304b50;
        public const UInt32 Zip64EndOfCentralDirectoryRecordSignature = 0x06064b50;
        public const UInt32 Zip64EndOfCentralDirectoryLocatorSignature = 0x07064b50;
        public const UInt32 EndOfCentralDirectorySignature = 0x06054b50;
        public const int ZipEntrySignature = 0x04034b50;
        public const int ZipEntryDataDescriptorSignature = 0x08074b50;
        public const int SplitArchiveSignature = 0x08074b50;
        public const int ZipDirEntrySignature = 0x02014b50;

        // These are dictated by the Zip Spec.See APPNOTE.txt
        public const int AesKeySize = 192;  // 128, 192, 256
        public const int AesBlockSize = 128;  // ???

        public const UInt16 AesAlgId128 = 0x660E;
        public const UInt16 AesAlgId192 = 0x660F;
        public const UInt16 AesAlgId256 = 0x6610;
    }

}