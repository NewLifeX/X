using System.Collections;
using NewLife.Reflection;

namespace NewLife.Serialization;

/// <summary>列表数据编码</summary>
public class JsonArray : JsonHandlerBase
{
    /// <summary>初始化</summary>
    public JsonArray()
    {
        // 优先级
        Priority = 20;
    }

    /// <summary>写入</summary>
    /// <param name="value"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public override Boolean Write(Object value, Type type)
    {
        if (value is not IList list) return false;

        Host.Write("[");

        // 循环写入数据
        foreach (var item in list)
        {
            Host.Write(item);
        }

        Host.Write("]");

        return true;
    }

    /// <summary>读取</summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public override Boolean TryRead(Type type, ref Object value)
    {
        if (!type.As<IList>() && !type.As(typeof(IList<>))) return false;

        // 先读取
        if (!Host.Read("[")) return false;

        // 子元素类型
        var elmType = type.GetElementTypeEx();

        var list = typeof(IList<>).MakeGenericType(elmType).CreateInstance() as IList;
        while (!Host.Read("]"))
        {
            Object obj = null;
            if (!Host.TryRead(elmType, ref obj)) return false;

            list.Add(obj);
        }

        // 数组的创建比较特别
        if (type.As<Array>())
        {
            value = Array.CreateInstance(type.GetElementTypeEx(), list.Count);
            list.CopyTo((Array)value, 0);
        }
        else
            value = list;

        return true;
    }
}