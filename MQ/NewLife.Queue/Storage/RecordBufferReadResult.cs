namespace NewLife.Queue.Storage
{
    public struct RecordBufferReadResult
    {
        public static readonly RecordBufferReadResult Failure = new RecordBufferReadResult(false, null);

        public readonly bool Success;
        public readonly byte[] RecordBuffer;

        public RecordBufferReadResult(bool success, byte[] recordBuffer)
        {
            Success = success;
            RecordBuffer = recordBuffer;
        }

        public override string ToString()
        {
            return string.Format("[Success:{0}, RecordBufferLength:{1}]", Success, RecordBuffer != null ? RecordBuffer.Length : 0);
        }
    }
}
