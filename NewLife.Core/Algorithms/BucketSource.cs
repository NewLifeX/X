using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Data;

namespace NewLife.Algorithms
{
    public struct Range
    {
        public Int32 Start;
        public Int32 End;
        public Int64 Time;
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
            private Int32 _index = -1;
            private Range[] _buckets;

            private Range _current;
            public Range Current => _current;

            Object IEnumerator.Current => Current;

            public FixedSizeBucketEnumerator(BucketSource source) => _source = source;
            public void Dispose() { }

            public Boolean MoveNext()
            {
                if (!_inited)
                {
                    _inited = true;
                    _index = 0;

                    // 计算首尾的两个桶的值
                    var data = _source.Data;
                    var size = _source.BucketSize;
                    var s = (data[0].Time / size) * size + _source.BucketOffset;
                    var e = (data[data.Length - 1].Time / size) * size + _source.BucketOffset;

                    // 初始化所有桶
                    var list = new List<Range>();
                    for (var i = s; i <= e; i += size)
                    {
                        list.Add(new Range
                        {
                            Start = -1,
                            End = -1,
                            Time = i
                        });
                    }
                    _buckets = list.ToArray();

                    // 计算每个桶的头尾
                    var j = 0;
                    for (var i = 0; i < _buckets.Length; i++)
                    {
                        // 顺序遍历原始数据，这里假设原始数据为升序
                        ref var b = ref _buckets[i];
                        for (; j < data.Length; j++)
                        {
                            // 如果超过了当前桶的结尾，则换下一个桶
                            var t = data[j].Time;
                            if (t >= b.Time + size) break;

                            if (b.Time <= t)
                            {
                                if (b.Start < 0)
                                    b.Start = j;
                                else if (b.End < 0 || b.End < t)
                                    b.End = j;
                            }
                        }
                    }
                }

                // 桶位置
                if (_index >= _buckets.Length) return false;

                _current = _buckets[_index++];

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
