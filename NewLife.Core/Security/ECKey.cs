using System.Security.Cryptography;
using NewLife.Buffers;
using NewLife.Serialization;

namespace NewLife.Security;

/// <summary>椭圆曲线密钥</summary>
public class ECKey : IAccessor, ISpanSerializable
{
    #region 属性
    private AlgorithmKeyBlob _Algorithm;
    /// <summary>算法</summary>
    public String Algorithm { get => _Algorithm + ""; set => _Algorithm = (AlgorithmKeyBlob)Enum.Parse(typeof(AlgorithmKeyBlob), value); }

    /// <summary>坐标X</summary>
    public Byte[]? X { get; set; }

    /// <summary>坐标Y</summary>
    public Byte[]? Y { get; set; }

    /// <summary>私钥才有</summary>
    public Byte[]? D { get; set; }
    #endregion

    #region 方法
    /// <summary>设置算法参数</summary>
    /// <param name="oid">曲线Oid</param>
    /// <param name="privateKey">是否私钥</param>
    public void SetAlgorithm(Oid oid, Boolean privateKey)
    {
        var name = oid.FriendlyName ?? throw new ArgumentNullException(nameof(oid));

        if (privateKey)
            Algorithm = name.Replace("_", "_PRIVATE_");
        else
            Algorithm = name.Replace("_", "_PUBLIC_");
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
    public Boolean Read(Stream stream, Object? context)
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
    public Boolean Write(Stream stream, Object? context)
    {
        var writer = context as BinaryWriter ?? new BinaryWriter(stream);

        var x = X ?? throw new InvalidOperationException("未设置椭圆曲线公钥坐标X");
        var y = Y ?? throw new InvalidOperationException("未设置椭圆曲线公钥坐标Y");

        // 幻数(4) + 长度len(4) + X(len) + Y(len) + D(len)
        writer.Write((Int32)_Algorithm);
        writer.Write(x.Length);
        writer.Write(x);
        writer.Write(y);
        if (D != null && D.Length > 0) writer.Write(D);

        return true;
    }

#if !NETFRAMEWORK
    /// <summary>导出参数</summary>
    /// <remarks>net45/net461/net462 等老框架没有 ECParameters 类型，仅在其余目标提供</remarks>
    /// <returns></returns>
    public ECParameters ExportParameters()
    {
        var x = X ?? throw new InvalidOperationException("未设置椭圆曲线公钥坐标X");
        var y = Y ?? throw new InvalidOperationException("未设置椭圆曲线公钥坐标Y");

        var name = Algorithm.Replace("_PRIVATE_", "_").Replace("_PUBLIC_", "_");

        // 通用幻数（ECDSA_GENERIC/ECDH_GENERIC）不带曲线信息，按坐标长度推断命名曲线
        if (name.EndsWith("_GENERIC"))
        {
            var family = name.StartsWith("ECDH") ? "ECDH" : "ECDSA";
            name = x.Length switch
            {
                32 => $"{family}_P256",
                48 => $"{family}_P384",
                66 => $"{family}_P521",
                _ => name,
            };
        }

        return new ECParameters
        {
            D = D,
            Q = new ECPoint
            {
                X = x,
                Y = y,
            },
            Curve = ECCurve.CreateFromFriendlyName(name),
        };
    }
#endif

    /// <summary>写入到Span写入器</summary>
    /// <param name="writer">Span写入器</param>
    public void Write(ref SpanWriter writer)
    {
        var x = X ?? throw new InvalidOperationException("未设置椭圆曲线公钥坐标X");
        var y = Y ?? throw new InvalidOperationException("未设置椭圆曲线公钥坐标Y");

        // 幻数(4) + 长度len(4) + X(len) + Y(len) + D(len)
        writer.Write((Int32)_Algorithm);
        writer.Write(x.Length);
        writer.Write(x);
        writer.Write(y);
        if (D != null && D.Length > 0)
        {
            writer.Write(D);
        }
    }

    /// <summary>从Span读取器读取</summary>
    /// <param name="reader">Span读取器</param>
    public void Read(ref SpanReader reader)
    {
        // 幻数(4) + 长度len(4) + X(len) + Y(len) + D(len)
        _Algorithm = (AlgorithmKeyBlob)reader.ReadInt32();

        var len = reader.ReadInt32();
        X = reader.ReadBytes(len).ToArray();
        Y = reader.ReadBytes(len).ToArray();
        if (reader.Available > 0)
        {
            D = reader.ReadBytes(len).ToArray();
        }
    }
    #endregion
}
