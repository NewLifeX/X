using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Data;

namespace NewLife.Algorithms
{
    internal struct Range
    {
        public Int32 Start;
        public Int32 End;
    }

    internal class BucketSource : IEnumerable<Range>
    {
        #region 属性
        public TimePoint[] Data { get; set; }

        public Int32 Offset { get; set; }

        public Int32 Length { get; set; }

        public Int32 Threshod { get; set; }

        public Double Step { get; private set; }

        /// <summary>
        /// 桶大小。若指定，则采用固定桶大小，例如每分钟一个桶
        /// </summary>
        public Int32 BucketSize { get; set; }

        /// <summary>
        /// 桶偏移。X轴对桶大小取模后的偏移量
        /// </summary>
        public Int32 BucketOffset { get; set; }
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
        public IEnumerator<Range> GetEnumerator() => BucketSize > 0 ? new FixedSizeBucketEnumerator(this) : new IndexBucketEnumerator(this);
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

        private class FixedSizeBucketEnumerator : IEnumerator<Range>
        {
            private readonly BucketSource _source;
            private Boolean _inited;

            private Range _current;
            public Range Current => _current;

            Object IEnumerator.Current => Current;

            public FixedSizeBucketEnumerator(BucketSource source) => _source = source;
            public void Dispose() { }

            public Boolean MoveNext()
            {
                var data = _source.Data;
                var size = _source.BucketSize;
                if (!_inited)
                {
                    _inited = true;

                    var p = (Int32)(data[0].Time % size);
                    p -= _source.BucketOffset;
                    if (p <= 0) p += size;

                    _current.Start = 0;
                    var flag = false;
                    for (var i = 1; i < data.Length; i++)
                    {
                        if (data[i].Time > p)
                        {
                            flag = true;
                            _current.End = i;
                            break;
                        }
                    }
                    if (!flag) _current.End = data.Length;
                }
                else
                {
                    if (_current.End >= data.Length) return false;

                    _current.Start = _current.End;
                    var p = _current.End + size;
                    var flag = false;
                    for (var i = _current.Start; i < data.Length; i++)
                    {
                        if (data[i].Time > p)
                        {
                            flag = true;
                            _current.End = i;
                            break;
                        }
                    }
                    if (!flag) _current.End = data.Length;
                }

                return true;
            }

            public void Reset()
            {
                _inited = false;
                _current = default;
            }
        }
        #endregion
    }
}
