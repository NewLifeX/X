using System;
using System.IO;
using System.Security.Cryptography;
using NewLife.Serialization;

namespace NewLife.Security
{
    /// <summary>椭圆曲线密钥</summary>
    public class ECKey : IAccessor
    {
        #region 属性
        private AlgorithmKeyBlob _Algorithm;
        /// <summary>算法</summary>
        public String Algorithm { get => _Algorithm + ""; set => _Algorithm = (AlgorithmKeyBlob)Enum.Parse(typeof(AlgorithmKeyBlob), value); }

        /// <summary>坐标X</summary>
        public Byte[] X { get; set; }

        /// <summary>坐标Y</summary>
        public Byte[] Y { get; set; }

        /// <summary>私钥才有</summary>
        public Byte[] D { get; set; }
        #endregion

        #region 方法
        /// <summary>设置算法参数</summary>
        /// <param name="oid"></param>
        /// <param name="privateKey"></param>
        public void SetAlgorithm(Oid oid, Boolean privateKey)
        {
            if (privateKey)
                Algorithm = oid.FriendlyName.Replace("_", "_PRIVATE_");
            else
                Algorithm = oid.FriendlyName.Replace("_", "_PUBLIC_");
        }
        #endregion

        #region 导入导出
        /// <summary>读取</summary>
        /// <param name="data"></param>
        public void Read(Byte[] data) => Read(new MemoryStream(data), null);

        /// <summary>转字节数组</summary>
        /// <returns></returns>
        public Byte[] ToArray()
        {
            var ms = new MemoryStream();
            Write(ms, null);
            return ms.ToArray();
        }

        /// <summary>读取</summary>
        /// <param name="stream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Boolean Read(Stream stream, Object context)
        {
            var reader = context as BinaryReader ?? new BinaryReader(stream);

            // 幻数(4) + 长度len(4) + X(len) + Y(len) + D(len)
            _Algorithm = (AlgorithmKeyBlob)reader.ReadInt32();

            var len = reader.ReadInt32();

            X = reader.ReadBytes(len);
            Y = reader.ReadBytes(len);
            if (reader.BaseStream.Position < reader.BaseStream.Length) D = reader.ReadBytes(len);

            return true;
        }

        /// <summary>写入</summary>
        /// <param name="stream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Boolean Write(Stream stream, Object context)
        {
            var writer = context as BinaryWriter ?? new BinaryWriter(stream);

            // 幻数(4) + 长度len(4) + X(len) + Y(len) + D(len)
            writer.Write((Int32)_Algorithm);
            writer.Write(X.Length);
            writer.Write(X);
            writer.Write(Y);
            if (D != null && D.Length > 0) writer.Write(D);

            return true;
        }

#if __CORE__
        /// <summary>导出参数</summary>
        /// <returns></returns>
        public ECParameters ExportParameters()
        {
            return new ECParameters
            {
                D = D,
                Q = new ECPoint
                {
                    X = X,
                    Y = Y,
                },
                Curve = ECCurve.CreateFromFriendlyName(Algorithm.Replace("_PRIVATE_", "_").Replace("_PUBLIC_", "_")),
            };
        }
#endif
        #endregion
    }
}