#if NET4
using System;

namespace System.IO.Compression
{
    /// <summary>压缩方法</summary>
    public enum CompressionMethod : ushort
    {
        /// <summary>A direct copy of the file contents is held in the archive</summary>
        Stored = 0,

        /// <summary>
        /// Common Zip compression method using a sliding dictionary
        /// of up to 32KB and secondary compression from Huffman/Shannon-Fano trees
        /// </summary>
        Deflated = 8,

        /// <summary>An extension to deflate with a 64KB window. Not supported by #Zip currently</summary>
        Deflate64 = 9,

        /// <summary>BZip2 compression. Not supported by #Zip.</summary>
        BZip2 = 11,

        /// <summary>7Zip格式</summary>
        LZMA = 14,

        /// <summary>WinZip special for AES encryption, Now supported by #Zip.</summary>
        WinZipAES = 99,
    }

    /// <summary>系统类型</summary>
    enum HostSystem : ushort
    {
        /// <summary>Host system = MSDOS</summary>
        Msdos = 0,
        /// <summary>Host system = Amiga</summary>
        Amiga = 1,
        /// <summary>Host system = Open VMS</summary>
        OpenVms = 2,
        /// <summary>Host system = Unix</summary>
        Unix = 3,
        /// <summary>Host system = VMCms</summary>
        VMCms = 4,
        /// <summary>Host system = Atari ST</summary>
        AtariST = 5,
        /// <summary>Host system = OS2</summary>
        OS2 = 6,
        /// <summary>Host system = Macintosh</summary>
        Macintosh = 7,
        /// <summary>Host system = ZSystem</summary>
        ZSystem = 8,
        /// <summary>Host system = Cpm</summary>
        Cpm = 9,
        /// <summary>Host system = Windows NT</summary>
        WindowsNT = 10,
        /// <summary>Host system = MVS</summary>
        MVS = 11,
        /// <summary>Host system = VSE</summary>
        Vse = 12,
        /// <summary>Host system = Acorn RISC</summary>
        AcornRisc = 13,
        /// <summary>Host system = VFAT</summary>
        Vfat = 14,
        /// <summary>Host system = Alternate MVS</summary>
        AlternateMvs = 15,
        /// <summary>Host system = BEOS</summary>
        BeOS = 16,
        /// <summary>Host system = Tandem</summary>
        Tandem = 17,
        /// <summary>Host system = OS400</summary>
        OS400 = 18,
        /// <summary>Host system = OSX</summary>
        OSX = 19,
        /// <summary>WinRar</summary>
        WinRar = 45,
        /// <summary>Host system = WinZIP AES</summary>
        WinZipAES = 99,
    }

    /// <summary>通用标识位</summary>
    [Flags]
    enum GeneralBitFlags : ushort
    {
        /// <summary>Bit 0 if set indicates that the file is encrypted</summary>
        Encrypted = 0x0001,
        /// <summary>Bits 1 and 2 - Two bits defining the compression method (only for Method 6 Imploding and 8,9 Deflating)</summary>
        Method = 0x0006,
        /// <summary>Bit 3 if set indicates a trailing data desciptor is appended to the entry data</summary>
        Descriptor = 0x0008,
        /// <summary>Bit 4 is reserved for use with method 8 for enhanced deflation</summary>
        ReservedPKware4 = 0x0010,
        /// <summary>
        /// Bit 5 if set indicates the file contains Pkzip compressed patched data.
        /// Requires version 2.7 or greater.
        /// </summary>
        Patched = 0x0020,
        /// <summary>Bit 6 if set indicates strong encryption has been used for this entry.</summary>
        StrongEncryption = 0x0040,
        /// <summary>Bit 7 is currently unused</summary>
        Unused7 = 0x0080,
        /// <summary>Bit 8 is currently unused</summary>
        Unused8 = 0x0100,
        /// <summary>Bit 9 is currently unused</summary>
        Unused9 = 0x0200,
        /// <summary>Bit 10 is currently unused</summary>
        Unused10 = 0x0400,
        /// <summary>
        /// Bit 11 if set indicates the filename and
        /// comment fields for this file must be encoded using UTF-8.
        /// </summary>
        UnicodeText = 0x0800,
        /// <summary>Bit 12 is documented as being reserved by PKware for enhanced compression.</summary>
        EnhancedCompress = 0x1000,
        /// <summary>
        /// Bit 13 if set indicates that values in the local header are masked to hide
        /// their actual values, and the central directory is encrypted.
        /// </summary>
        /// <remarks>
        /// Used when encrypting the central directory contents.
        /// </remarks>
        HeaderMasked = 0x2000,
        /// <summary>Bit 14 is documented as being reserved for use by PKware</summary>
        ReservedPkware14 = 0x4000,
        /// <summary>Bit 15 is documented as being reserved for use by PKware</summary>
        ReservedPkware15 = 0x8000
    }

    static class ZipConstants
    {
        public const UInt32 PackedToRemovableMedia = 0x30304b50;
        //public const UInt32 Zip64EndOfCentralDirectoryRecordSignature = 0x06064b50;
        //public const UInt32 Zip64EndOfCentralDirectoryLocatorSignature = 0x07064b50;
        public const UInt32 DigitalSignature = 0x05054b50;
        public const UInt32 EndOfCentralDirectorySignature = 0x06054b50;
        public const UInt32 ZipEntrySignature = 0x04034b50;
        //public const UInt32 ZipEntryDataDescriptorSignature = 0x08074b50;
        //public const UInt32 SplitArchiveSignature = 0x08074b50;
        public const UInt32 ZipDirEntrySignature = 0x02014b50;
    }
}
#endif