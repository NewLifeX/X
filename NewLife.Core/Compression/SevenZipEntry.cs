using System.IO;
using System.Linq;
using SharpCompress.Common;
using SharpCompress.Common.SevenZip;
using System.Collections.Generic;
using System;

namespace NewLife.Compression
{
    public class SevenZipEntry 
    {
        #region 属性
        internal SevenZipEntry(SevenZipFilePart filePart)
        {
            this.FilePart = filePart;
        }

        internal SevenZipFilePart FilePart { get; private set; }

        public override CompressionType CompressionType        {            get            {                return FilePart.CompressionType;            }        }

        public override uint Crc        {            get { return (uint)FilePart.Header.FileCRC; }        }

        public override string FilePath        {            get { return FilePart.Header.Name; }        }

        public override long CompressedSize        {            get { return 0; }        }

        public override long Size        {            get { return (long)FilePart.Header.Size; }        }

        public override DateTime? LastModifiedTime        {            get { throw new NotImplementedException(); }        }

        public override DateTime? CreatedTime        {            get { throw new NotImplementedException(); }        }

        public override DateTime? LastAccessedTime        {            get { throw new NotImplementedException(); }        }

        public override DateTime? ArchivedTime        {            get { throw new NotImplementedException(); }        }

        public override bool IsEncrypted        {            get { return false; }        }

        public override bool IsDirectory        {            get { return FilePart.Header.IsDirectory; }        }

        public override bool IsSplit        {            get { return false; }        }

        internal override IEnumerable<FilePart> Parts
        {
            get { return FilePart.AsEnumerable<FilePart>(); }
        }

        internal override void Close()        {        }
        #endregion

        private SevenZipArchive archive;

        internal SevenZipArchiveEntry(SevenZipArchive archive, SevenZipFilePart part)
            : base(part)
        {
            this.archive = archive;
        }

        public Stream OpenEntryStream()
        {
            return Parts.Single().GetStream();
        }

        public void WriteTo(Stream stream)
        {
            if (IsEncrypted)
            {
                throw new PasswordProtectedException("Entry is password protected and cannot be extracted.");
            }
            this.Extract(archive, stream);
        }

        public bool IsComplete
        {
            get
            {
                return true;
            }
        }
    }
}
