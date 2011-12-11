using System;
using System.IO;
using NewLife.Security;

namespace NewLife.Compression
{
    public class ZipInputStream : Stream
    {
        #region 属性
        private Stream _innerStream;
        Crc32 crc = new Crc32();
        #endregion

        #region 方法

        public ZipEntry GetNextEntry()
        {
            //if (_findRequired)
            //{
            //    // find the next signature
            //    long d = SharedUtilities.FindSignature(_inputStream, ZipConstants.ZipEntrySignature);
            //    if (d == -1) return null;
            //    // back up 4 bytes: ReadEntry assumes the file pointer is positioned before the entry signature
            //    _inputStream.Seek(-4, SeekOrigin.Current);
            //    // workitem 10178
            //    Ionic.Zip.SharedUtilities.Workaround_Ladybug318918(_inputStream);
            //}
            //// workitem 10923
            //else if (_firstEntry)
            //{
            //    // we've already read one entry.
            //    // Seek to the end of it.
            //    _inputStream.Seek(_endOfEntry, SeekOrigin.Begin);
            //    Ionic.Zip.SharedUtilities.Workaround_Ladybug318918(_inputStream);
            //}

            //_currentEntry = ZipEntry.ReadEntry(_container, !_firstEntry);
            //// ReadEntry leaves the file position after all the entry
            //// data and the optional bit-3 data descriptpr.  This is
            //// where the next entry would normally start.
            //_endOfEntry = _inputStream.Position;
            //_firstEntry = true;
            //_needSetup = true;
            //_findRequired = false;
            //return _currentEntry;
            return null;
        }

        public void CloseEntry()
        {
            //if (crc == null)
            //{
            //    throw new InvalidOperationException("Closed");
            //}

            //if (entry == null)
            //{
            //    return;
            //}

            //if (method == (int)CompressionMethod.Deflated)
            //{
            //    if ((flags & 8) != 0)
            //    {
            //        // We don't know how much we must skip, read until end.
            //        byte[] tmp = new byte[4096];

            //        // Read will close this entry
            //        while (Read(tmp, 0, tmp.Length) > 0)
            //        {
            //        }
            //        return;
            //    }

            //    csize -= inf.TotalIn;
            //    inputBuffer.Available += inf.RemainingInput;
            //}

            //if ((inputBuffer.Available > csize) && (csize >= 0))
            //{
            //    inputBuffer.Available = (int)((long)inputBuffer.Available - csize);
            //}
            //else
            //{
            //    csize -= inputBuffer.Available;
            //    inputBuffer.Available = 0;
            //    while (csize != 0)
            //    {
            //        long skipped = base.Skip(csize);

            //        if (skipped <= 0)
            //        {
            //            throw new ZipException("Zip archive ends early.");
            //        }

            //        csize -= skipped;
            //    }
            //}

            //CompleteCloseEntry(false);
        }

        /// <summary>
        /// Complete cleanup as the final part of closing.
        /// </summary>
        /// <param name="testCrc">True if the crc value should be tested</param>
        private void CompleteCloseEntry(bool testCrc)
        {
            //StopDecrypting();

            //if ((flags & 8) != 0)
            //{
            //    ReadDataDescriptor();
            //}

            //size = 0;

            //if (testCrc &&
            //    ((crc.Value & 0xFFFFFFFFL) != entry.Crc) && (entry.Crc != -1))
            //{
            //    throw new ZipException("CRC mismatch");
            //}

            //crc.Reset();

            //if (method == (int)CompressionMethod.Deflated)
            //{
            //    inf.Reset();
            //}
            //entry = null;
        }

        /// <summary>
        /// Handle attempts to read by throwing an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="destination">The destination array to store data in.</param>
        /// <param name="offset">The offset at which data read should be stored.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>Returns the number of bytes actually read.</returns>
        private int ReadingNotAvailable(byte[] destination, int offset, int count)
        {
            throw new InvalidOperationException("Unable to read from this stream");
        }

        /// <summary>
        /// Handle attempts to read from this entry by throwing an exception
        /// </summary>
        private int ReadingNotSupported(byte[] destination, int offset, int count)
        {
            throw new ZipException("The compression method for this entry is not supported");
        }

        /// <summary>
        /// Perform the initial read on an entry which may include
        /// reading encryption headers and setting up inflation.
        /// </summary>
        /// <param name="destination">The destination to fill with data read.</param>
        /// <param name="offset">The offset to start reading at.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The actual number of bytes read.</returns>
        private int InitialRead(byte[] destination, int offset, int count)
        {
            //if (!CanDecompressEntry)
            //{
            //    throw new ZipException("Library cannot extract this entry. Version required is (" + entry.Version.ToString() + ")");
            //}

            //// Handle encryption if required.
            //if (entry.IsCrypted)
            //{
            //    if (password == null)
            //    {
            //        throw new ZipException("No password set.");
            //    }

            //    // Generate and set crypto transform...
            //    PkzipClassicManaged managed = new PkzipClassicManaged();
            //    byte[] key = PkzipClassic.GenerateKeys(ZipConstants.ConvertToArray(password));

            //    inputBuffer.CryptoTransform = managed.CreateDecryptor(key, null);

            //    byte[] cryptbuffer = new byte[ZipConstants.CryptoHeaderSize];
            //    inputBuffer.ReadClearTextBuffer(cryptbuffer, 0, ZipConstants.CryptoHeaderSize);

            //    if (cryptbuffer[ZipConstants.CryptoHeaderSize - 1] != entry.CryptoCheckValue)
            //    {
            //        throw new ZipException("Invalid password");
            //    }

            //    if (csize >= ZipConstants.CryptoHeaderSize)
            //    {
            //        csize -= ZipConstants.CryptoHeaderSize;
            //    }
            //    else if ((entry.Flags & (int)GeneralBitFlags.Descriptor) == 0)
            //    {
            //        throw new ZipException(string.Format("Entry compressed size {0} too small for encryption", csize));
            //    }
            //}
            //else
            //{
            //    inputBuffer.CryptoTransform = null;
            //}

            //if ((csize > 0) || ((flags & (int)GeneralBitFlags.Descriptor) != 0))
            //{
            //    if ((method == (int)CompressionMethod.Deflated) && (inputBuffer.Available > 0))
            //    {
            //        inputBuffer.SetInflaterInput(inf);
            //    }

            //    internalReader = new ReadDataHandler(BodyRead);
            //    return BodyRead(destination, offset, count);
            //}
            //else
            //{
            //    internalReader = new ReadDataHandler(ReadingNotAvailable);
            //    return 0;
            //}

            return 0;
        }

        /// <summary>
        /// Reads a block of bytes from the current zip entry.
        /// </summary>
        /// <returns>
        /// The number of bytes read (this may be less than the length requested, even before the end of stream), or 0 on end of stream.
        /// </returns>
        /// <exception name="IOException">
        /// An i/o error occured.
        /// </exception>
        /// <exception cref="ZipException">
        /// The deflated stream is corrupted.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The stream is not open.
        /// </exception>
        private int BodyRead(byte[] buffer, int offset, int count)
        {
            //if (crc == null)
            //{
            //    throw new InvalidOperationException("Closed");
            //}

            //if ((entry == null) || (count <= 0))
            //{
            //    return 0;
            //}

            //if (offset + count > buffer.Length)
            //{
            //    throw new ArgumentException("Offset + count exceeds buffer size");
            //}

            //bool finished = false;

            //switch (method)
            //{
            //    case (int)CompressionMethod.Deflated:
            //        count = base.Read(buffer, offset, count);
            //        if (count <= 0)
            //        {
            //            if (!inf.IsFinished)
            //            {
            //                throw new ZipException("Inflater not finished!");
            //            }
            //            inputBuffer.Available = inf.RemainingInput;

            //            // A csize of -1 is from an unpatched local header
            //            if ((flags & 8) == 0 &&
            //                (inf.TotalIn != csize && csize != 0xFFFFFFFF && csize != -1 || inf.TotalOut != size))
            //            {
            //                throw new ZipException("Size mismatch: " + csize + ";" + size + " <-> " + inf.TotalIn + ";" + inf.TotalOut);
            //            }
            //            inf.Reset();
            //            finished = true;
            //        }
            //        break;

            //    case (int)CompressionMethod.Stored:
            //        if ((count > csize) && (csize >= 0))
            //        {
            //            count = (int)csize;
            //        }

            //        if (count > 0)
            //        {
            //            count = inputBuffer.ReadClearTextBuffer(buffer, offset, count);
            //            if (count > 0)
            //            {
            //                csize -= count;
            //                size -= count;
            //            }
            //        }

            //        if (csize == 0)
            //        {
            //            finished = true;
            //        }
            //        else
            //        {
            //            if (count < 0)
            //            {
            //                throw new ZipException("EOF in stored block");
            //            }
            //        }
            //        break;
            //}

            //if (count > 0)
            //{
            //    crc.Update(buffer, offset, count);
            //}

            //if (finished)
            //{
            //    CompleteCloseEntry(true);
            //}

            return count;
        }

        /// <summary>
        /// Closes the zip input stream
        /// </summary>
        public override void Close()
        {
            //internalReader = new ReadDataHandler(ReadingNotAvailable);
            //crc = null;
            //entry = null;

            base.Close();
        }

        #endregion

        #region 接口

        public override bool CanRead { get { return true; } }

        public override bool CanSeek { get { return _innerStream.CanWrite; } }

        public override bool CanWrite { get { return false; } }

        public override void Flush() { throw new NotImplementedException(); }

        public override long Length { get { return _innerStream.Length; } }

        public override long Position { get { return _innerStream.Position; } set { _innerStream.Position = value; } }

        public override int Read(byte[] buffer, int offset, int count)
        {
            //if (_closed)
            //{
            //    _exceptionPending = true;
            //    throw new System.InvalidOperationException("The stream has been closed.");
            //}

            //if (_needSetup)
            //    SetupStream();

            //if (_LeftToRead == 0) return 0;

            //int len = (_LeftToRead > count) ? count : (int)_LeftToRead;
            //int n = _crcStream.Read(buffer, offset, len);

            //_LeftToRead -= n;

            //if (_LeftToRead == 0)
            //{
            //    int CrcResult = _crcStream.Crc;
            //    _currentEntry.VerifyCrcAfterExtract(CrcResult);
            //    _inputStream.Seek(_endOfEntry, SeekOrigin.Begin);
            //    // workitem 10178
            //    Ionic.Zip.SharedUtilities.Workaround_Ladybug318918(_inputStream);
            //}

            //return n;

            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) { return _innerStream.Seek(offset, origin); }

        public override void SetLength(long value) { throw new NotImplementedException(); }

        public override void Write(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }

        #endregion
    }
}