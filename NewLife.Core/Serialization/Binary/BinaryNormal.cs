using System.Net;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Serialization;

/// <summary>常用类型编码</summary>
public class BinaryNormal : BinaryHandlerBase
{
    /// <summary>初始化</summary>
    public BinaryNormal() => Priority = 12;

    /// <summary>写入</summary>
    /// <param name="value"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public override Boolean Write(Object? value, Type type)
    {
        if (type == typeof(Guid))
        {
            if (value is not Guid guid) guid = Guid.Empty;
            Write(guid.ToByteArray(), -1);
            return true;
        }
        else if (type == typeof(Byte[]) && value is Byte[] buf)
        {
            //Write((Byte[])value);
            if (Host is Binary bn)
            {
                var bc = bn.GetHandler<BinaryGeneral>();
                bc?.Write(buf);
            }

            return true;
        }
        else if (type == typeof(IPacket) || type.As<IPacket>())
        {
            if (value is IPacket pk)
            {
                Host.WriteSize(pk.Total);
                pk.CopyTo(Host.Stream);

                if (Host is Binary bn) bn.Total += pk.Total;
            }
            else
            {
                Host.WriteSize(0);
            }

            return true;
        }
        else if (type == typeof(Char[]) && value is Char[] cs)
        {
            //Write((Char[])value);
            if (Host is Binary bn)
            {
                var bc = bn.GetHandler<BinaryGeneral>();
                bc?.Write(cs, 0, -1);
            }

            return true;
        }
        else if (type == typeof(DateTimeOffset) && value is DateTimeOffset dto)
        {
            Host.Write(dto.DateTime);
            Host.Write(dto.Offset);
            return true;
        }
#if NET6_0_OR_GREATER
        else if (type == typeof(DateOnly) && value is DateOnly date)
        {
            Host.Write(date.DayNumber);
            return true;
        }
        else if (type == typeof(TimeOnly) && value is TimeOnly time)
        {
            Host.Write(time.Ticks);
            return true;
        }
#endif
        else if (type == typeof(IPAddress) && value is IPAddress addr)
        {
            Host.Write(addr.GetAddressBytes());
            return true;
        }
        else if (type == typeof(IPEndPoint) && value is IPEndPoint ep)
        {
            Host.Write(ep.Address.GetAddressBytes());
            Host.Write((UInt16)ep.Port);
            return true;
        }

        return false;
    }

    /// <summary>写入字节数组，自动计算长度</summary>
    /// <param name="buffer">缓冲区</param>
    /// <param name="count">数量</param>
    private void Write(Byte[] buffer, Int32 count)
    {
        if (buffer == null) return;

        if (count < 0 || count > buffer.Length) count = buffer.Length;

        Host.Write(buffer, 0, count);
    }

    /// <summary>读取</summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public override Boolean TryRead(Type type, ref Object? value)
    {
        var bn = (Host as Binary)!;
        if (type == typeof(Guid))
        {
#if NETCOREAPP || NETSTANDARD2_1
            Span<Byte> buffer = stackalloc Byte[16];
            if (Host.ReadBytes(buffer) == 0) return false;

            value = new Guid(buffer);
#else
            var buffer = Pool.Shared.Rent(16);
            if (Host.ReadBytes(buffer, 0, 16) == 0) return false;

            value = new Guid(buffer);
            Pool.Shared.Return(buffer);
#endif

            return true;
        }
        else if (type == typeof(Byte[]))
        {
            if (!TryReadArray(-1, out var buf)) return false;

            value = buf;
            return true;
        }
        else if (type == typeof(IPacket))
        {
            if (!TryReadArray(-1, out var buf)) return false;

            value = new ArrayPacket(buf);
            return true;
        }
        else if (type == typeof(Packet))
        {
            if (!TryReadArray(-1, out var buf)) return false;

            value = new Packet(buf);
            return true;
        }
        else if (type == typeof(Char[]))
        {
            //value = ReadChars(-1);
            if (!TryReadArray(-1, out var buf)) return false;

            value = Host.Encoding.GetChars(buf);
            return true;
        }
        else if (type == typeof(DateTimeOffset))
        {
            value = new DateTimeOffset(Host.Read<DateTime>(), Host.Read<TimeSpan>());
            return true;
        }
#if NET6_0_OR_GREATER
        else if (type == typeof(DateOnly))
        {
            value = DateOnly.FromDayNumber(Host.Read<Int32>());
            return true;
        }
        else if (type == typeof(TimeOnly))
        {
            value = new TimeOnly(Host.Read<Int64>());
            return true;
        }
#endif
        else if (type == typeof(IPAddress))
        {
            if (!TryReadArray(-1, out var buf)) return false;

            value = new IPAddress(buf);
            return true;
        }
        else if (type == typeof(IPEndPoint))
        {
            if (!TryReadArray(-1, out var buf)) return false;

            var ip = new IPAddress(buf);
            var port = Host.Read<UInt16>();
            value = new IPEndPoint(ip, port);
            return true;
        }

        return false;
    }

    /// <summary>从当前流中将 count 个字节读入字节数组，如果count小于0，则先读取字节数组长度。</summary>
    /// <param name="count">要读取的字节数。</param>
    /// <param name="buffer"></param>
    /// <returns></returns>
    protected virtual Boolean TryReadArray(Int32 count, out Byte[] buffer)
    {
        buffer = [];
        if (count < 0 && !Host.TryReadSize(out count)) return false;

        if (count <= 0) return true;

        var max = IOHelper.MaxSafeArraySize;
        if (count > max) throw new XException("Security required, reading large variable length arrays is not allowed {0:n0}>{1:n0}", count, max);

        buffer = Host.ReadBytes(count);

        return true;
    }
}