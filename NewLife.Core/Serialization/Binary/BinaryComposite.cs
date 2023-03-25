using System.Collections.Concurrent;
using System.Reflection;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Serialization.Interface;

namespace NewLife.Serialization;

/// <summary>复合对象处理器</summary>
public class BinaryComposite : BinaryHandlerBase
{
    /// <summary>实例化</summary>
    public BinaryComposite() => Priority = 100;

    /// <summary>写入对象</summary>
    /// <param name="value">目标对象</param>
    /// <param name="type">类型</param>
    /// <returns></returns>
    public override Boolean Write(Object value, Type type)
    {
        // 不支持基本类型
        if (Type.GetTypeCode(type) != TypeCode.Object) return false;

        var ims = Host.IgnoreMembers;

        var ms = GetMembers(type).Where(e => !ims.Contains(e.Name)).ToList();
        WriteLog("BinaryWrite类{0} 共有成员{1}个", type.Name, ms.Count);

        if (Host is Binary b && b.UseFieldSize)
        {
            // 遍历成员，寻找FieldSizeAttribute特性，重新设定大小字段的值
            foreach (var member in ms)
            {
                // 获取FieldSizeAttribute特性
                var atts = member.GetCustomAttributes<FieldSizeAttribute>();
                if (atts != null)
                {
                    foreach (var att in atts)
                    {
                        if (att.Version.IsNullOrEmpty() || att.Version == (Host as Binary).Version)
                            att.SetReferenceSize(value, member, Host.Encoding);
                    }
                }
            }
        }

        // 如果不是第一层，这里开始必须写对象引用
        if (WriteRef(value)) return true;

        Host.Hosts.Push(value);

        var context = new AccessorContext
        {
            Host = Host,
            Type = type,
            Value = value,
            UserState = Host.UserState
        };

        // 获取成员
        foreach (var member in ms)
        {
            var mtype = GetMemberType(member);
            context.Member = Host.Member = member;

            var v = value is IModel src ? src[member.Name] : value.GetValue(member);
            WriteLog("    {0}.{1} {2}", type.Name, member.Name, v);

            // 成员访问器优先
            if (value is IMemberAccessor ac && ac.Write(Host, context)) continue;
            if (TryGetAccessor(member, out var acc) && acc.Write(Host, context)) continue;

            if (!Host.Write(v, mtype))
            {
                Host.Hosts.Pop();
                return false;
            }
        }
        Host.Hosts.Pop();

        return true;
    }

    private Boolean WriteRef(Object value)
    {
        var bn = Host as Binary;
        if (!bn.UseRef) return false;
        if (Host.Hosts.Count == 0) return false;

        if (value == null)
        {
            Host.Write(0);
            return true;
        }

        // 找到对象索引，并写入
        var hs = Host.Hosts.ToArray();
        for (var i = 0; i < hs.Length; i++)
        {
            if (value == hs[i])
            {
                Host.WriteSize(i + 1);
                return true;
            }
        }

        // 埋下自己
        Host.WriteSize(Host.Hosts.Count + 1);

        return false;
    }

    /// <summary>尝试读取指定类型对象</summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public override Boolean TryRead(Type type, ref Object value)
    {
        if (type == typeof(Object)) return false;
        if (type == null)
        {
            if (value == null) return false;
            type = value.GetType();
        }

        // 不支持基本类型
        if (Type.GetTypeCode(type) != TypeCode.Object) return false;

        // 不支持基类不是Object的特殊类型
        if (!type.As<Object>()) return false;

        var ims = Host.IgnoreMembers;

        var ms = GetMembers(type).Where(e => !ims.Contains(e.Name)).ToList();
        WriteLog("BinaryRead类{0} 共有成员{1}个", type.Name, ms.Count);

        // 读取对象引用
        if (ReadRef(ref value)) return true;

        value ??= type.CreateInstance();

        Host.Hosts.Push(value);

        var context = new AccessorContext
        {
            Host = Host,
            Type = type,
            Value = value,
            UserState = Host.UserState
        };

        // 获取成员
        for (var i = 0; i < ms.Count; i++)
        {
            var member = ms[i];

            var mtype = GetMemberType(member);
            context.Member = Host.Member = member;
            WriteLog("    {0}.{1}", member.DeclaringType.Name, member.Name);

            // 成员访问器优先
            if (value is IMemberAccessor ac && ac.Read(Host, context)) continue;
            if (TryGetAccessor(member, out var acc) && acc.Read(Host, context)) continue;

            // 数据流不足时，放弃读取目标成员，并认为整体成功
            var hs = Host.Stream;
            if (hs.CanSeek && hs.Position >= hs.Length) break;

            Object v = null;
            v = value is IModel src ? src[member.Name] : value.GetValue(member);
            if (!Host.TryRead(mtype, ref v))
            {
                Host.Hosts.Pop();
                return false;
            }

            if (value is IModel dst)
                dst[member.Name] = v;
            else
                value.SetValue(member, v);
        }
        Host.Hosts.Pop();

        return true;
    }

    private Boolean ReadRef(ref Object value)
    {
        var bn = Host as Binary;
        if (!bn.UseRef) return false;
        if (Host.Hosts.Count == 0) return false;

        var rf = bn.ReadEncodedInt32();
        if (rf == 0)
        {
            //value = null;
            return true;
        }

        // 找到对象索引
        var hs = Host.Hosts.ToArray();
        // 如果引用是对象数加一，说明有对象紧跟着
        if (rf == hs.Length + 1) return false;

        if (rf < 0 || rf > hs.Length) throw new XException("无法在 {0} 个对象中找到引用 {1}", hs.Length, rf);

        value = hs[rf - 1];

        return true;
    }

    #region 获取成员
    /// <summary>获取成员</summary>
    /// <param name="type"></param>
    /// <param name="baseFirst"></param>
    /// <returns></returns>
    protected virtual List<MemberInfo> GetMembers(Type type, Boolean baseFirst = true)
    {
        if (Host.UseProperty)
            return type.GetProperties(baseFirst).Cast<MemberInfo>().ToList();
        else
            return type.GetFields(baseFirst).Cast<MemberInfo>().ToList();
    }

    private static Type GetMemberType(MemberInfo member)
    {
        return member.MemberType switch
        {
            MemberTypes.Field => (member as FieldInfo).FieldType,
            MemberTypes.Property => (member as PropertyInfo).PropertyType,
            _ => throw new NotSupportedException(),
        };
    }

    private static readonly ConcurrentDictionary<MemberInfo, IMemberAccessor> _cache = new();
    private static Boolean TryGetAccessor(MemberInfo member, out IMemberAccessor acc)
    {
        if (_cache.TryGetValue(member, out acc)) return acc != null;

        var atts = member.GetCustomAttributes();
        acc = atts.FirstOrDefault(e => e is IMemberAccessor) as IMemberAccessor;

        _cache[member] = acc;

        return acc != null;
    }
    #endregion
}