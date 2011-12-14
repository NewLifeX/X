//using System;
//using System.Collections.Generic;
//using System.IO;
//using NewLife.Security;

//namespace NewLife.Compression
//{
//    public class ZipOutputStream : Stream
//    {
//        #region 属性
//        private Stream _innerStream;
//        private List<ZipEntry> entries = new List<ZipEntry>();
//        Crc32 crc = new Crc32();
//        ZipEntry curEntry;
//        #endregion

//        #region 方法

//        public void PutNextEntry(ZipEntry entry)
//        {
//            //if (entry == null) throw new ArgumentNullException("entry");

//            //if (entries == null) throw new InvalidOperationException("ZipOutputStream was finished");

//            //if (curEntry != null) CloseEntry();

//            //if (entries.Count == int.MaxValue) throw new ZipException("Too many entries for Zip file");

//            //CompressionMethod method = entry.CompressionMethod;
//            //int compressionLevel = defaultCompressionLevel;

//            //// Clear flags that the library manages internally
//            //entry.Flags &= (int)GeneralBitFlags.UnicodeText;
//            //patchEntryHeader = false;

//            //bool headerInfoAvailable;

//            //// No need to compress - definitely no data.
//            //if (entry.Size == 0)
//            //{
//            //    entry.CompressedSize = entry.Size;
//            //    entry.Crc = 0;
//            //    method = CompressionMethod.Stored;
//            //    headerInfoAvailable = true;
//            //}
//            //else
//            //{
//            //    headerInfoAvailable = (entry.Size >= 0) && entry.HasCrc;

//            //    // Switch to deflation if storing isnt possible.
//            //    if (method == CompressionMethod.Stored)
//            //    {
//            //        if (!headerInfoAvailable)
//            //        {
//            //            if (!CanPatchEntries)
//            //            {
//            //                // Can't patch entries so storing is not possible.
//            //                method = CompressionMethod.Deflated;
//            //                compressionLevel = 0;
//            //            }
//            //        }
//            //        else // entry.size must be > 0
//            //        {
//            //            entry.CompressedSize = entry.Size;
//            //            headerInfoAvailable = entry.HasCrc;
//            //        }
//            //    }
//            //}

//            //if (headerInfoAvailable == false)
//            //{
//            //    if (CanPatchEntries == false)
//            //    {
//            //        // Only way to record size and compressed size is to append a data descriptor
//            //        // after compressed data.

//            //        // Stored entries of this form have already been converted to deflating.
//            //        entry.Flags |= 8;
//            //    }
//            //    else
//            //    {
//            //        patchEntryHeader = true;
//            //    }
//            //}

//            //if (Password != null)
//            //{
//            //    entry.IsCrypted = true;
//            //    if (entry.Crc < 0)
//            //    {
//            //        // Need to append a data descriptor as the crc isnt available for use
//            //        // with encryption, the date is used instead.  Setting the flag
//            //        // indicates this to the decompressor.
//            //        entry.Flags |= 8;
//            //    }
//            //}

//            //entry.Offset = offset;
//            //entry.CompressionMethod = (CompressionMethod)method;

//            //curMethod = method;
//            //sizePatchPos = -1;

//            //if ((useZip64_ == UseZip64.On) || ((entry.Size < 0) && (useZip64_ == UseZip64.Dynamic)))
//            //{
//            //    entry.ForceZip64();
//            //}

//            //// Write the local file header
//            //WriteLeInt(ZipConstants.LocalHeaderSignature);

//            //WriteLeShort(entry.Version);
//            //WriteLeShort(entry.Flags);
//            //WriteLeShort((byte)entry.CompressionMethodForHeader);
//            //WriteLeInt((int)entry.DosTime);

//            //// TODO: Refactor header writing.  Its done in several places.
//            //if (headerInfoAvailable == true)
//            //{
//            //    WriteLeInt((int)entry.Crc);
//            //    if (entry.LocalHeaderRequiresZip64)
//            //    {
//            //        WriteLeInt(-1);
//            //        WriteLeInt(-1);
//            //    }
//            //    else
//            //    {
//            //        WriteLeInt(entry.IsCrypted ? (int)entry.CompressedSize + ZipConstants.CryptoHeaderSize : (int)entry.CompressedSize);
//            //        WriteLeInt((int)entry.Size);
//            //    }
//            //}
//            //else
//            //{
//            //    if (patchEntryHeader)
//            //    {
//            //        crcPatchPos = baseOutputStream_.Position;
//            //    }
//            //    WriteLeInt(0);	// Crc

//            //    if (patchEntryHeader)
//            //    {
//            //        sizePatchPos = baseOutputStream_.Position;
//            //    }

//            //    // For local header both sizes appear in Zip64 Extended Information
//            //    if (entry.LocalHeaderRequiresZip64 || patchEntryHeader)
//            //    {
//            //        WriteLeInt(-1);
//            //        WriteLeInt(-1);
//            //    }
//            //    else
//            //    {
//            //        WriteLeInt(0);	// Compressed size
//            //        WriteLeInt(0);	// Uncompressed size
//            //    }
//            //}

//            //byte[] name = ZipConstants.ConvertToArray(entry.Flags, entry.Name);

//            //if (name.Length > 0xFFFF)
//            //{
//            //    throw new ZipException("Entry name too long.");
//            //}

//            //ZipExtraData ed = new ZipExtraData(entry.ExtraData);

//            //if (entry.LocalHeaderRequiresZip64)
//            //{
//            //    ed.StartNewEntry();
//            //    if (headerInfoAvailable)
//            //    {
//            //        ed.AddLeLong(entry.Size);
//            //        ed.AddLeLong(entry.CompressedSize);
//            //    }
//            //    else
//            //    {
//            //        ed.AddLeLong(-1);
//            //        ed.AddLeLong(-1);
//            //    }
//            //    ed.AddNewEntry(1);

//            //    if (!ed.Find(1))
//            //    {
//            //        throw new ZipException("Internal error cant find extra data");
//            //    }

//            //    if (patchEntryHeader)
//            //    {
//            //        sizePatchPos = ed.CurrentReadIndex;
//            //    }
//            //}
//            //else
//            //{
//            //    ed.Delete(1);
//            //}

//            //if (entry.AESKeySize > 0)
//            //{
//            //    AddExtraDataAES(entry, ed);
//            //}

//            //byte[] extra = ed.GetEntryData();

//            //WriteLeShort(name.Length);
//            //WriteLeShort(extra.Length);

//            //if (name.Length > 0)
//            //{
//            //    baseOutputStream_.Write(name, 0, name.Length);
//            //}

//            //if (entry.LocalHeaderRequiresZip64 && patchEntryHeader)
//            //{
//            //    sizePatchPos += baseOutputStream_.Position;
//            //}

//            //if (extra.Length > 0)
//            //{
//            //    baseOutputStream_.Write(extra, 0, extra.Length);
//            //}

//            //offset += ZipConstants.LocalHeaderBaseSize + name.Length + extra.Length;
//            //// Fix offsetOfCentraldir for AES
//            //if (entry.AESKeySize > 0)
//            //    offset += entry.AESOverheadSize;

//            //// Activate the entry.
//            //curEntry = entry;
//            //crc.Reset();
//            //if (method == CompressionMethod.Deflated)
//            //{
//            //    deflater_.Reset();
//            //    deflater_.SetLevel(compressionLevel);
//            //}
//            //size = 0;

//            //if (entry.IsCrypted)
//            //{
//            //    if (entry.AESKeySize > 0)
//            //    {
//            //        WriteAESHeader(entry);
//            //    }
//            //    else
//            //    {
//            //        if (entry.Crc < 0)
//            //        {			// so testing Zip will says its ok
//            //            WriteEncryptionHeader(entry.DosTime << 16);
//            //        }
//            //        else
//            //        {
//            //            WriteEncryptionHeader(entry.Crc);
//            //        }
//            //    }
//            //}
//        }

//        /// <summary>
//        /// Closes the current entry, updating header and footer information as required
//        /// </summary>
//        /// <exception cref="System.IO.IOException">
//        /// An I/O error occurs.
//        /// </exception>
//        /// <exception cref="System.InvalidOperationException">
//        /// No entry is active.
//        /// </exception>
//        public void CloseEntry()
//        {
//            //if (curEntry == null) throw new InvalidOperationException("No open entry");

//            //long csize = size;

//            //// First finish the deflater, if appropriate
//            //if (curMethod == CompressionMethod.Deflated)
//            //{
//            //    if (size >= 0)
//            //    {
//            //        base.Finish();
//            //        csize = deflater_.TotalOut;
//            //    }
//            //    else
//            //    {
//            //        deflater_.Reset();
//            //    }
//            //}

//            //// Write the AES Authentication Code (a hash of the compressed and encrypted data)
//            //if (curEntry.AESKeySize > 0)
//            //{
//            //    baseOutputStream_.Write(AESAuthCode, 0, 10);
//            //}

//            //if (curEntry.Size < 0)
//            //{
//            //    curEntry.Size = size;
//            //}
//            //else if (curEntry.Size != size)
//            //{
//            //    throw new ZipException("size was " + size + ", but I expected " + curEntry.Size);
//            //}

//            //if (curEntry.CompressedSize < 0)
//            //{
//            //    curEntry.CompressedSize = csize;
//            //}
//            //else if (curEntry.CompressedSize != csize)
//            //{
//            //    throw new ZipException("compressed size was " + csize + ", but I expected " + curEntry.CompressedSize);
//            //}

//            //if (curEntry.Crc < 0)
//            //{
//            //    curEntry.Crc = crc.Value;
//            //}
//            //else if (curEntry.Crc != crc.Value)
//            //{
//            //    throw new ZipException("crc was " + crc.Value + ", but I expected " + curEntry.Crc);
//            //}

//            //offset += csize;

//            //if (curEntry.IsCrypted)
//            //{
//            //    if (curEntry.AESKeySize > 0)
//            //    {
//            //        curEntry.CompressedSize += curEntry.AESOverheadSize;
//            //    }
//            //    else
//            //    {
//            //        curEntry.CompressedSize += ZipConstants.CryptoHeaderSize;
//            //    }
//            //}

//            //// Patch the header if possible
//            //if (patchEntryHeader)
//            //{
//            //    patchEntryHeader = false;

//            //    long curPos = baseOutputStream_.Position;
//            //    baseOutputStream_.Seek(crcPatchPos, SeekOrigin.Begin);
//            //    WriteLeInt((int)curEntry.Crc);

//            //    if (curEntry.LocalHeaderRequiresZip64)
//            //    {
//            //        if (sizePatchPos == -1)
//            //        {
//            //            throw new ZipException("Entry requires zip64 but this has been turned off");
//            //        }

//            //        baseOutputStream_.Seek(sizePatchPos, SeekOrigin.Begin);
//            //        WriteLeLong(curEntry.Size);
//            //        WriteLeLong(curEntry.CompressedSize);
//            //    }
//            //    else
//            //    {
//            //        WriteLeInt((int)curEntry.CompressedSize);
//            //        WriteLeInt((int)curEntry.Size);
//            //    }
//            //    baseOutputStream_.Seek(curPos, SeekOrigin.Begin);
//            //}

//            //// Add data descriptor if flagged as required
//            //if ((curEntry.Flags & 8) != 0)
//            //{
//            //    WriteLeInt(ZipConstants.DataDescriptorSignature);
//            //    WriteLeInt(unchecked((int)curEntry.Crc));

//            //    if (curEntry.LocalHeaderRequiresZip64)
//            //    {
//            //        WriteLeLong(curEntry.CompressedSize);
//            //        WriteLeLong(curEntry.Size);
//            //        offset += ZipConstants.Zip64DataDescriptorSize;
//            //    }
//            //    else
//            //    {
//            //        WriteLeInt((int)curEntry.CompressedSize);
//            //        WriteLeInt((int)curEntry.Size);
//            //        offset += ZipConstants.DataDescriptorSize;
//            //    }
//            //}

//            //entries.Add(curEntry);
//            //curEntry = null;
//        }

//        public void Finish()
//        {
//            if (entries == null) return;

//            if (curEntry != null) CloseEntry();

//            long numEntries = entries.Count;
//            long sizeEntries = 0;

//            //foreach (ZipEntry entry in entries)
//            //{
//            //    WriteLeInt(ZipConstants.CentralHeaderSignature);
//            //    WriteLeShort(ZipConstants.VersionMadeBy);
//            //    WriteLeShort(entry.Version);
//            //    WriteLeShort(entry.Flags);
//            //    WriteLeShort((short)entry.CompressionMethodForHeader);
//            //    WriteLeInt((int)entry.DosTime);
//            //    WriteLeInt((int)entry.Crc);

//            //    if (entry.IsZip64Forced() ||
//            //        (entry.CompressedSize >= uint.MaxValue))
//            //    {
//            //        WriteLeInt(-1);
//            //    }
//            //    else
//            //    {
//            //        WriteLeInt((int)entry.CompressedSize);
//            //    }

//            //    if (entry.IsZip64Forced() ||
//            //        (entry.Size >= uint.MaxValue))
//            //    {
//            //        WriteLeInt(-1);
//            //    }
//            //    else
//            //    {
//            //        WriteLeInt((int)entry.Size);
//            //    }

//            //    byte[] name = ZipConstants.ConvertToArray(entry.Flags, entry.Name);

//            //    if (name.Length > 0xffff)
//            //    {
//            //        throw new ZipException("Name too long.");
//            //    }

//            //    ZipExtraData ed = new ZipExtraData(entry.ExtraData);

//            //    if (entry.CentralHeaderRequiresZip64)
//            //    {
//            //        ed.StartNewEntry();
//            //        if (entry.IsZip64Forced() ||
//            //            (entry.Size >= 0xffffffff))
//            //        {
//            //            ed.AddLeLong(entry.Size);
//            //        }

//            //        if (entry.IsZip64Forced() ||
//            //            (entry.CompressedSize >= 0xffffffff))
//            //        {
//            //            ed.AddLeLong(entry.CompressedSize);
//            //        }

//            //        if (entry.Offset >= 0xffffffff)
//            //        {
//            //            ed.AddLeLong(entry.Offset);
//            //        }

//            //        ed.AddNewEntry(1);
//            //    }
//            //    else
//            //    {
//            //        ed.Delete(1);
//            //    }

//            //    if (entry.AESKeySize > 0)
//            //    {
//            //        AddExtraDataAES(entry, ed);
//            //    }

//            //    byte[] extra = ed.GetEntryData();

//            //    byte[] entryComment =
//            //        (entry.Comment != null) ?
//            //        ZipConstants.ConvertToArray(entry.Flags, entry.Comment) :
//            //        new byte[0];

//            //    if (entryComment.Length > 0xffff)
//            //    {
//            //        throw new ZipException("Comment too long.");
//            //    }

//            //    WriteLeShort(name.Length);
//            //    WriteLeShort(extra.Length);
//            //    WriteLeShort(entryComment.Length);
//            //    WriteLeShort(0);	// disk number
//            //    WriteLeShort(0);	// internal file attributes
//            //    // external file attributes

//            //    if (entry.ExternalFileAttributes != -1)
//            //    {
//            //        WriteLeInt(entry.ExternalFileAttributes);
//            //    }
//            //    else
//            //    {
//            //        if (entry.IsDirectory)
//            //        {                         // mark entry as directory (from nikolam.AT.perfectinfo.com)
//            //            WriteLeInt(16);
//            //        }
//            //        else
//            //        {
//            //            WriteLeInt(0);
//            //        }
//            //    }

//            //    if (entry.Offset >= uint.MaxValue)
//            //    {
//            //        WriteLeInt(-1);
//            //    }
//            //    else
//            //    {
//            //        WriteLeInt((int)entry.Offset);
//            //    }

//            //    if (name.Length > 0)
//            //    {
//            //        baseOutputStream_.Write(name, 0, name.Length);
//            //    }

//            //    if (extra.Length > 0)
//            //    {
//            //        baseOutputStream_.Write(extra, 0, extra.Length);
//            //    }

//            //    if (entryComment.Length > 0)
//            //    {
//            //        baseOutputStream_.Write(entryComment, 0, entryComment.Length);
//            //    }

//            //    sizeEntries += ZipConstants.CentralHeaderBaseSize + name.Length + extra.Length + entryComment.Length;
//            //}

//            //using (ZipHelperStream zhs = new ZipHelperStream(baseOutputStream_))
//            //{
//            //    zhs.WriteEndOfCentralDirectory(numEntries, sizeEntries, offset, zipComment);
//            //}

//            entries = null;
//        }

//        #endregion

//        #region 接口

//        public override bool CanRead { get { return false; } }

//        public override bool CanSeek { get { return _innerStream.CanSeek; } }

//        public override bool CanWrite { get { return true; } }

//        public override void Flush() { throw new NotImplementedException(); }

//        public override long Length { get { return _innerStream.Length; } }

//        public override long Position { get { return _innerStream.Position; } set { _innerStream.Position = value; } }

//        public override int Read(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }

//        public override long Seek(long offset, SeekOrigin origin) { return _innerStream.Seek(offset, origin); }

//        public override void SetLength(long value) { throw new NotImplementedException(); }

//        public override void Write(byte[] buffer, int offset, int count)
//        {
//            if (curEntry == null) throw new InvalidOperationException("No open entry.");

//            if (buffer == null) throw new ArgumentNullException("buffer");

//            if (offset < 0) throw new ArgumentOutOfRangeException("offset", "Cannot be negative");

//            if (count < 0) throw new ArgumentOutOfRangeException("count", "Cannot be negative");

//            if ((buffer.Length - offset) < count) throw new ArgumentException("Invalid offset/count combination");

//            crc.Update(buffer, offset, count);
//            //size += count;

//            //switch (curMethod)
//            //{
//            //    case CompressionMethod.Deflated:
//            //        base.Write(buffer, offset, count);
//            //        break;

//            //    case CompressionMethod.Stored:
//            //        if (Password != null)
//            //        {
//            //            CopyAndEncrypt(buffer, offset, count);
//            //        }
//            //        else
//            //        {
//            //            baseOutputStream_.Write(buffer, offset, count);
//            //        }
//            //        break;
//            //}
//        }

//        #endregion

//        #region 辅助

//        /// <summary>
//        /// Write an unsigned short in little endian byte order.
//        /// </summary>
//        private void WriteLeShort(int value)
//        {
//            unchecked
//            {
//                _innerStream.WriteByte((byte)(value & 0xff));
//                _innerStream.WriteByte((byte)((value >> 8) & 0xff));
//            }
//        }

//        /// <summary>
//        /// Write an int in little endian byte order.
//        /// </summary>
//        private void WriteLeInt(int value)
//        {
//            unchecked
//            {
//                WriteLeShort(value);
//                WriteLeShort(value >> 16);
//            }
//        }

//        /// <summary>
//        /// Write an int in little endian byte order.
//        /// </summary>
//        private void WriteLeLong(long value)
//        {
//            unchecked
//            {
//                WriteLeInt((int)value);
//                WriteLeInt((int)(value >> 32));
//            }
//        }

//        #endregion
//    }
//}