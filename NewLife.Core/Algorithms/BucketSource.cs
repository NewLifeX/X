using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Data;

namespace NewLife.Algorithms
{
    internal class BucketSource : IEnumerable<Range>
    {
        #region 属性
        public TimePoint[] Data { get; set; }

        public Int32 Offset { get; set; }

        public Int32 Length { get; set; }

        public Int32 Threshod { get; set; }

        public Double Step { get; private set; }
        #endregion

        #region 方法
        public void Init()
        {
            if (Threshod > 0) Step = (Double)Length / Threshod;
            if (Length == 0) Length = Data.Length;

            //XTrace.WriteLine("offset={0} length={1} threshod={2} step={3}", Offset, Length, Threshod, Step);
        }
        #endregion

        #region 枚举
        public IEnumerator<Range> GetEnumerator() =>  new IndexBucketEnumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private class IndexBucketEnumerator : IEnumerator<Range>
        {
            private readonly BucketSource _source;
            private Int32 _index = -1;

            private Range _current;
            public Range Current => _current;

            Object IEnumerator.Current => Current;

            public IndexBucketEnumerator(BucketSource source) => _source = source;
            public void Dispose() { }

            public Boolean MoveNext()
            {
                _index++;

                _current.Start = _source.Offset + (Int32)Math.Round((_index + 0) * _source.Step);
                _current.End = _source.Offset + (Int32)Math.Round((_index + 1) * _source.Step);
                var end = _source.Offset + _source.Length;
                if (_current.Start >= end) return false;
                if (_current.End > end) _current.End = end;

                return true;
            }

            public void Reset()
            {
                _index = -1;
                _current = default;
            }
        }
        #endregion
    }
}
