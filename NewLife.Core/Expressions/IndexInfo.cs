using System;
using System.Collections.Generic;

namespace NewLife.Expressions
{
    class IndexInfoResult
    {
        public IndexInfoResult(String indexMark)
        {
            Mark = indexMark;
            IndexInfos = new List<IndexInfo>();
        }

        public String Mark { get; set; }

        public IList<IndexInfo> IndexInfos { get; private set; }
    }

    class IndexInfo
    {
        public IndexInfo(String stockCode, Int32 value)
        {
            StockCode = stockCode;
            Value = value;
        }

        public IndexInfo(String code) => StockCode = code;

        public String StockCode { get; set; }

        public Int32? Value { get; set; }
    }
}