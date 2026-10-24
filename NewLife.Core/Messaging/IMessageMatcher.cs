namespace NewLife.Messaging;

/// <summary>请求-响应配对语义。由需要请求响应匹配的协议实现，配合会话消息泵把响应交付给等待中的请求</summary>
/// <remarks>
/// <para>会话在协议模式下发送消息并等待响应时，用本接口判断收到的响应是否与挂起请求配对；未实现本接口的协议不支持等待响应。</para>
/// <para>典型实现按协议配对键比较（如标准消息的序列号）。配对判定只在消息头部字段上进行，不读取负载。</para>
/// <para>无配对键的简单协议可用恒真实现（任意响应匹配任意请求），适合串行请求-响应的使用方式（参考 <see cref="LengthFieldCodec"/>）。</para>
/// </remarks>
/// <example>
/// <code>
/// public class MyCodec : IMessageCodec, IMessageMatcher
/// {
///     public Boolean Match(IMessage request, IMessage response) => ...;
/// }
/// </code>
/// </example>
public interface IMessageMatcher
{
    /// <summary>判断响应是否匹配请求（按协议配对键，如序列号）</summary>
    /// <param name="request">挂起的请求消息</param>
    /// <param name="response">收到的响应消息</param>
    /// <returns>是否配对</returns>
    Boolean Match(IMessage request, IMessage response);
}
