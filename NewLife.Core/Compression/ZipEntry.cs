using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace NewLife.Compression
{
    public class ZipEntry
    {
        #region 属性
        private String _FileName;
        /// <summary>文件名</summary>
        public String FileName { get { return _FileName; } set { _FileName = value; } }

        private String _Comment;
        /// <summary>注释</summary>
        public String Comment { get { return _Comment; } set { _Comment = value; } }

        private Boolean _IsDirectory;
        /// <summary>是否目录</summary>
        public Boolean IsDirectory { get { return _IsDirectory; } set { _IsDirectory = value; } }
        #endregion

        #region 读取核心
        internal static ZipEntry ReadEntry(ZipFile zipfile, Stream stream, bool first)
        {
            ZipEntry entry = new ZipEntry();

            // 有时候Zip文件以PK00开头
            if (first)
            {

            }

            // Read entry header, including any encryption header
            if (!entry.ReadHeader(stream, zipfile.Encoding)) return null;

            //// Store the position in the stream for this entry
            //// change for workitem 8098
            //entry.__FileDataPosition = entry.ArchiveStream.Position;

            //// seek past the data without reading it. We will read on Extract()
            //stream.Seek(entry._CompressedFileDataSize + entry._LengthOfTrailer, SeekOrigin.Current);

            //// ReadHeader moves the file pointer to the end of the entry header,
            //// as well as any encryption header.

            //// CompressedFileDataSize includes:
            ////   the maybe compressed, maybe encrypted file data
            ////   the encryption trailer, if any
            ////   the bit 3 descriptor, if any

            //// workitem 5306
            //// http://www.codeplex.com/DotNetZip/WorkItem/View.aspx?WorkItemId=5306
            //HandleUnexpectedDataDescriptor(entry);

            return entry;
        }

        private bool ReadHeader(Stream stream, Encoding defaultEncoding)
        {
            int bytesRead = 0;

            //            // change for workitem 8098
            //            _RelativeOffsetOfLocalHeader = ArchiveStream.Position;

            //            int signature = Ionic.Zip.SharedUtilities.ReadEntrySignature(ArchiveStream);
            //            bytesRead += 4;

            //            // Return false if this is not a local file header signature.
            //            if (ZipEntry.IsNotValidSig(signature))
            //            {
            //                // Getting "not a ZipEntry signature" is not always wrong or an error.
            //                // This will happen after the last entry in a zipfile.  In that case, we
            //                // expect to read :
            //                //    a ZipDirEntry signature (if a non-empty zip file) or
            //                //    a ZipConstants.EndOfCentralDirectorySignature.
            //                //
            //                // Anything else is a surprise.

            //                ArchiveStream.Seek(-4, SeekOrigin.Current); // unread the signature
            //                // workitem 10178
            //                Ionic.Zip.SharedUtilities.Workaround_Ladybug318918(ArchiveStream);
            //                if (ZipEntry.IsNotValidZipDirEntrySig(signature) && (signature != ZipConstants.EndOfCentralDirectorySignature))
            //                {
            //                    throw new BadReadException(String.Format("  Bad signature (0x{0:X8}) at position  0x{1:X8}", signature, ArchiveStream.Position));
            //                }
            //                return false;
            //            }

            //            byte[] block = new byte[26];
            //            int n = ArchiveStream.Read(block, 0, block.Length);
            //            if (n != block.Length) return false;
            //            bytesRead += n;

            //            int i = 0;
            //            _VersionNeeded = (Int16)(block[i++] + block[i++] * 256);
            //            _BitField = (Int16)(block[i++] + block[i++] * 256);
            //            _CompressionMethod_FromZipFile = _CompressionMethod = (Int16)(block[i++] + block[i++] * 256);
            //            _TimeBlob = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            //            // transform the time data into something usable (a DateTime)
            //            _LastModified = Ionic.Zip.SharedUtilities.PackedToDateTime(_TimeBlob);
            //            _timestamp |= ZipEntryTimestamp.DOS;

            //            if ((_BitField & 0x01) == 0x01)
            //            {
            //                _Encryption_FromZipFile = _Encryption = EncryptionAlgorithm.PkzipWeak; // this *may* change after processing the Extra field
            //                _sourceIsEncrypted = true;
            //            }

            //            // NB: if ((_BitField & 0x0008) != 0x0008), then the Compressed, uncompressed and
            //            // CRC values are not true values; the true values will follow the entry data.
            //            // But, regardless of the status of bit 3 in the bitfield, the slots for
            //            // the three amigos may contain marker values for ZIP64.  So we must read them.
            //            {
            //                _Crc32 = (Int32)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
            //                _CompressedSize = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
            //                _UncompressedSize = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);

            //                if ((uint)_CompressedSize == 0xFFFFFFFF ||
            //                    (uint)_UncompressedSize == 0xFFFFFFFF)

            //                    _InputUsesZip64 = true;
            //            }

            //            Int16 filenameLength = (short)(block[i++] + block[i++] * 256);
            //            Int16 extraFieldLength = (short)(block[i++] + block[i++] * 256);

            //            block = new byte[filenameLength];
            //            n = ArchiveStream.Read(block, 0, block.Length);
            //            bytesRead += n;

            //            // if the UTF8 bit is set for this entry, override the
            //            // encoding the application requested.

            //            if ((_BitField & 0x0800) == 0x0800)
            //            {
            //                // workitem 12744
            //                AlternateEncoding = System.Text.Encoding.UTF8;
            //                AlternateEncodingUsage = ZipOption.Always;
            //            }

            //            // need to use this form of GetString() for .NET CF
            //            _FileNameInArchive = AlternateEncoding.GetString(block, 0, block.Length);

            //            // workitem 6898
            //            if (_FileNameInArchive.EndsWith("/")) MarkAsDirectory();

            //            bytesRead += ProcessExtraField(ArchiveStream, extraFieldLength);

            //            _LengthOfTrailer = 0;

            //            // workitem 6607 - don't read for directories
            //            // actually get the compressed size and CRC if necessary
            //            if (!_FileNameInArchive.EndsWith("/") && (_BitField & 0x0008) == 0x0008)
            //            {
            //                // This descriptor exists only if bit 3 of the general
            //                // purpose bit flag is set (see below).  It is byte aligned
            //                // and immediately follows the last byte of compressed data,
            //                // as well as any encryption trailer, as with AES.
            //                // This descriptor is used only when it was not possible to
            //                // seek in the output .ZIP file, e.g., when the output .ZIP file
            //                // was standard output or a non-seekable device.  For ZIP64(tm) format
            //                // archives, the compressed and uncompressed sizes are 8 bytes each.

            //                // workitem 8098: ok (restore)
            //                long posn = ArchiveStream.Position;

            //                // Here, we're going to loop until we find a ZipEntryDataDescriptorSignature and
            //                // a consistent data record after that.   To be consistent, the data record must
            //                // indicate the length of the entry data.
            //                bool wantMore = true;
            //                long SizeOfDataRead = 0;
            //                int tries = 0;
            //                while (wantMore)
            //                {
            //                    tries++;
            //                    // We call the FindSignature shared routine to find the specified signature
            //                    // in the already-opened zip archive, starting from the current cursor
            //                    // position in that filestream.  If we cannot find the signature, then the
            //                    // routine returns -1, and the ReadHeader() method returns false,
            //                    // indicating we cannot read a legal entry header.  If we have found it,
            //                    // then the FindSignature() method returns the number of bytes in the
            //                    // stream we had to seek forward, to find the sig.  We need this to
            //                    // determine if the zip entry is valid, later.

            //                    if (_container.ZipFile != null)
            //                        _container.ZipFile.OnReadBytes(ze);

            //                    long d = Ionic.Zip.SharedUtilities.FindSignature(ArchiveStream, ZipConstants.ZipEntryDataDescriptorSignature);
            //                    if (d == -1) return false;

            //                    // total size of data read (through all loops of this).
            //                    SizeOfDataRead += d;

            //                    if (_InputUsesZip64)
            //                    {
            //                        // read 1x 4-byte (CRC) and 2x 8-bytes (Compressed Size, Uncompressed Size)
            //                        block = new byte[20];
            //                        n = ArchiveStream.Read(block, 0, block.Length);
            //                        if (n != 20) return false;

            //                        // do not increment bytesRead - it is for entry header only.
            //                        // the data we have just read is a footer (falls after the file data)
            //                        //bytesRead += n;

            //                        i = 0;
            //                        _Crc32 = (Int32)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
            //                        _CompressedSize = BitConverter.ToInt64(block, i);
            //                        i += 8;
            //                        _UncompressedSize = BitConverter.ToInt64(block, i);
            //                        i += 8;

            //                        _LengthOfTrailer += 24;  // bytes including sig, CRC, Comp and Uncomp sizes
            //                    }
            //                    else
            //                    {
            //                        // read 3x 4-byte fields (CRC, Compressed Size, Uncompressed Size)
            //                        block = new byte[12];
            //                        n = ArchiveStream.Read(block, 0, block.Length);
            //                        if (n != 12) return false;

            //                        // do not increment bytesRead - it is for entry header only.
            //                        // the data we have just read is a footer (falls after the file data)
            //                        //bytesRead += n;

            //                        i = 0;
            //                        _Crc32 = (Int32)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
            //                        _CompressedSize = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
            //                        _UncompressedSize = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);

            //                        _LengthOfTrailer += 16;  // bytes including sig, CRC, Comp and Uncomp sizes
            //                    }

            //                    wantMore = (SizeOfDataRead != _CompressedSize);

            //                    if (wantMore)
            //                    {
            //                        // Seek back to un-read the last 12 bytes  - maybe THEY contain
            //                        // the ZipEntryDataDescriptorSignature.
            //                        // (12 bytes for the CRC, Comp and Uncomp size.)
            //                        ArchiveStream.Seek(-12, SeekOrigin.Current);
            //                        // workitem 10178
            //                        Ionic.Zip.SharedUtilities.Workaround_Ladybug318918(ArchiveStream);

            //                        // Adjust the size to account for the false signature read in
            //                        // FindSignature().
            //                        SizeOfDataRead += 4;
            //                    }
            //                }

            //                // seek back to previous position, to prepare to read file data
            //                // workitem 8098: ok (restore)
            //                ArchiveStream.Seek(posn, SeekOrigin.Begin);
            //                // workitem 10178
            //                Ionic.Zip.SharedUtilities.Workaround_Ladybug318918(ArchiveStream);
            //            }

            //            _CompressedFileDataSize = _CompressedSize;

            //            // bit 0 set indicates that some kind of encryption is in use
            //            if ((_BitField & 0x01) == 0x01)
            //            {
            //#if AESCRYPTO
            //                if (Encryption == EncryptionAlgorithm.WinZipAes128 ||
            //                    Encryption == EncryptionAlgorithm.WinZipAes256)
            //                {
            //                    int bits = ZipEntry.GetKeyStrengthInBits(_Encryption_FromZipFile);
            //                    // read in the WinZip AES metadata: salt + PV. 18 bytes for AES256. 10 bytes for AES128.
            //                    _aesCrypto_forExtract = WinZipAesCrypto.ReadFromStream(null, bits, ArchiveStream);
            //                    bytesRead += _aesCrypto_forExtract.SizeOfEncryptionMetadata - 10; // MAC (follows crypto bytes)
            //                    // according to WinZip, the CompressedSize includes the AES Crypto framing data.
            //                    _CompressedFileDataSize -= _aesCrypto_forExtract.SizeOfEncryptionMetadata;
            //                    _LengthOfTrailer += 10;  // MAC
            //                }
            //                else
            //#endif
            //                {
            //                    // read in the header data for "weak" encryption
            //                    _WeakEncryptionHeader = new byte[12];
            //                    bytesRead += ZipEntry.ReadWeakEncryptionHeader(_archiveStream, _WeakEncryptionHeader);
            //                    // decrease the filedata size by 12 bytes
            //                    _CompressedFileDataSize -= 12;
            //                }
            //            }

            //            // Remember the size of the blob for this entry.
            //            // We also have the starting position in the stream for this entry.
            //            _LengthOfHeader = bytesRead;
            //            _TotalEntrySize = _LengthOfHeader + _CompressedFileDataSize + _LengthOfTrailer;

            //            // We've read in the regular entry header, the extra field, and any
            //            // encryption header.  The pointer in the file is now at the start of the
            //            // filedata, which is potentially compressed and encrypted.  Just ahead in
            //            // the file, there are _CompressedFileDataSize bytes of data, followed by
            //            // potentially a non-zero length trailer, consisting of optionally, some
            //            // encryption stuff (10 byte MAC for AES), and the bit-3 trailer (16 or 24
            //            // bytes).

            return true;
        }

        internal static ZipEntry ReadDirEntry(ZipFile zipfile, Stream stream, Dictionary<String, Object> previouslySeen)
        {
            Stream s = stream;
            BinaryReader reader = new BinaryReader(stream);

            //            int signature = reader.ReadInt32();
            //            // return null if this is not a local file header signature
            //            if (IsNotValidZipDirEntrySig(signature))
            //            {
            //                s.Seek(-4, SeekOrigin.Current);

            //                // Getting "not a ZipDirEntry signature" here is not always wrong or an
            //                // error.  This can happen when walking through a zipfile.  After the
            //                // last ZipDirEntry, we expect to read an
            //                // EndOfCentralDirectorySignature.  When we get this is how we know
            //                // we've reached the end of the central directory.
            //                if (signature != ZipConstants.EndOfCentralDirectorySignature &&
            //                    signature != ZipConstants.Zip64EndOfCentralDirectoryRecordSignature &&
            //                    signature != ZipConstants.ZipEntrySignature  // workitem 8299
            //                    )
            //                {
            //                    throw new ZipException(String.Format("  Bad signature (0x{0:X8}) at position 0x{1:X8}", signature, s.Position));
            //                }
            //                return null;
            //            }

            //            int bytesRead = 42 + 4;
            //            byte[] block = new byte[42];
            //            int n = s.Read(block, 0, block.Length);
            //            if (n != block.Length) return null;

            //            int i = 0;
            //            ZipEntry zde = new ZipEntry();
            //            zde.AlternateEncoding = expectedEncoding;
            //            zde._Source = ZipEntrySource.ZipFile;
            //            zde._container = new ZipContainer(zf);

            //            unchecked
            //            {
            //                zde._VersionMadeBy = (short)(block[i++] + block[i++] * 256);
            //                zde._VersionNeeded = (short)(block[i++] + block[i++] * 256);
            //                zde._BitField = (short)(block[i++] + block[i++] * 256);
            //                zde._CompressionMethod = (Int16)(block[i++] + block[i++] * 256);
            //                zde._TimeBlob = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            //                zde._LastModified = Ionic.Zip.SharedUtilities.PackedToDateTime(zde._TimeBlob);
            //                zde._timestamp |= ZipEntryTimestamp.DOS;

            //                zde._Crc32 = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            //                zde._CompressedSize = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
            //                zde._UncompressedSize = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
            //            }

            //            // preserve
            //            zde._CompressionMethod_FromZipFile = zde._CompressionMethod;

            //            zde._filenameLength = (short)(block[i++] + block[i++] * 256);
            //            zde._extraFieldLength = (short)(block[i++] + block[i++] * 256);
            //            zde._commentLength = (short)(block[i++] + block[i++] * 256);
            //            zde._diskNumber = (UInt32)(block[i++] + block[i++] * 256);

            //            zde._InternalFileAttrs = (short)(block[i++] + block[i++] * 256);
            //            zde._ExternalFileAttrs = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;

            //            zde._RelativeOffsetOfLocalHeader = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);

            //            // workitem 7801
            //            zde.IsText = ((zde._InternalFileAttrs & 0x01) == 0x01);

            //            block = new byte[zde._filenameLength];
            //            n = s.Read(block, 0, block.Length);
            //            bytesRead += n;
            //            if ((zde._BitField & 0x0800) == 0x0800)
            //            {
            //                // UTF-8 is in use
            //                zde._FileNameInArchive = Ionic.Zip.SharedUtilities.Utf8StringFromBuffer(block);
            //            }
            //            else
            //            {
            //                zde._FileNameInArchive = Ionic.Zip.SharedUtilities.StringFromBuffer(block, expectedEncoding);
            //            }

            //            // workitem 10330
            //            // insure unique entry names
            //            while (previouslySeen.ContainsKey(zde._FileNameInArchive))
            //            {
            //                zde._FileNameInArchive = CopyHelper.AppendCopyToFileName(zde._FileNameInArchive);
            //                zde._metadataChanged = true;
            //            }

            //            if (zde.AttributesIndicateDirectory)
            //                zde.MarkAsDirectory();  // may append a slash to filename if nec.
            //            // workitem 6898
            //            else if (zde._FileNameInArchive.EndsWith("/")) zde.MarkAsDirectory();

            //            zde._CompressedFileDataSize = zde._CompressedSize;
            //            if ((zde._BitField & 0x01) == 0x01)
            //            {
            //                // this may change after processing the Extra field
            //                zde._Encryption_FromZipFile = zde._Encryption =
            //                    EncryptionAlgorithm.PkzipWeak;
            //                zde._sourceIsEncrypted = true;
            //            }

            //            if (zde._extraFieldLength > 0)
            //            {
            //                zde._InputUsesZip64 = (zde._CompressedSize == 0xFFFFFFFF ||
            //                      zde._UncompressedSize == 0xFFFFFFFF ||
            //                      zde._RelativeOffsetOfLocalHeader == 0xFFFFFFFF);

            //                // Console.WriteLine("  Input uses Z64?:      {0}", zde._InputUsesZip64);

            //                bytesRead += zde.ProcessExtraField(s, zde._extraFieldLength);
            //                zde._CompressedFileDataSize = zde._CompressedSize;
            //            }

            //            // we've processed the extra field, so we know the encryption method is set now.
            //            if (zde._Encryption == EncryptionAlgorithm.PkzipWeak)
            //            {
            //                // the "encryption header" of 12 bytes precedes the file data
            //                zde._CompressedFileDataSize -= 12;
            //            }
            //#if AESCRYPTO
            //            else if (zde.Encryption == EncryptionAlgorithm.WinZipAes128 ||
            //                        zde.Encryption == EncryptionAlgorithm.WinZipAes256)
            //            {
            //                zde._CompressedFileDataSize = zde.CompressedSize -
            //                    (ZipEntry.GetLengthOfCryptoHeaderBytes(zde.Encryption) + 10);
            //                zde._LengthOfTrailer = 10;
            //            }
            //#endif

            //            // tally the trailing descriptor
            //            if ((zde._BitField & 0x0008) == 0x0008)
            //            {
            //                // sig, CRC, Comp and Uncomp sizes
            //                if (zde._InputUsesZip64)
            //                    zde._LengthOfTrailer += 24;
            //                else
            //                    zde._LengthOfTrailer += 16;
            //            }

            //            // workitem 12744
            //            zde.AlternateEncoding = ((zde._BitField & 0x0800) == 0x0800)
            //                ? Encoding.UTF8
            //                : expectedEncoding;

            //            zde.AlternateEncodingUsage = ZipOption.Always;

            //            if (zde._commentLength > 0)
            //            {
            //                block = new byte[zde._commentLength];
            //                n = s.Read(block, 0, block.Length);
            //                bytesRead += n;
            //                if ((zde._BitField & 0x0800) == 0x0800)
            //                {
            //                    // UTF-8 is in use
            //                    zde._Comment = Ionic.Zip.SharedUtilities.Utf8StringFromBuffer(block);
            //                }
            //                else
            //                {
            //                    zde._Comment = Ionic.Zip.SharedUtilities.StringFromBuffer(block, expectedEncoding);
            //                }
            //            }
            //            //zde._LengthOfDirEntry = bytesRead;
            //            return zde;
            return null;
        }
        #endregion


    }
}