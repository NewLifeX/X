using System;
using System.Data;
using System.Data.Common;

namespace XCode.TDengine
{
    /// <summary>参数</summary>
    public class TDengineParameter : DbParameter
    {
        /// <summary>类型</summary>
        public override DbType DbType { get; set; } = DbType.String;

        /// <summary>方向</summary>
        public override ParameterDirection Direction { get; set; }

        /// <summary>允许空</summary>
        public override Boolean IsNullable { get; set; } = true;

        /// <summary>参数名</summary>
        public override String ParameterName { get; set; }

        /// <summary>参数值</summary>
        public override Object Value { get; set; }

        /// <summary>大小</summary>
        public override Int32 Size { get; set; }

        /// <summary>源列</summary>
        public override String SourceColumn { get; set; } = String.Empty;

        /// <summary>源列空映射</summary>
        public override Boolean SourceColumnNullMapping { get; set; }

        /// <summary>源版本</summary>
        public override DataRowVersion SourceVersion { get; set; } = DataRowVersion.Current;

        /// <summary>重置</summary>
        public override void ResetDbType() => DbType = DbType.String;
    }
}