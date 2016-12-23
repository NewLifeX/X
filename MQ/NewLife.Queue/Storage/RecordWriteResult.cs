namespace NewLife.Queue.Storage
{
    public struct RecordWriteResult
    {
        public readonly bool Success;
        public readonly long Position;

        private RecordWriteResult(bool success, long position)
        {
            Success = success;
            Position = position;
        }

        public static RecordWriteResult NotEnoughSpace()
        {
            return new RecordWriteResult(false, -1);
        }
        public static RecordWriteResult Successful(long position)
        {
            return new RecordWriteResult(true, position);
        }

        public override string ToString()
        {
            return string.Format("[Success:{0}, Position:{1}]", Success, Position);
        }
    }
}
