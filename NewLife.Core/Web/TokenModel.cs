using System.Runtime.Serialization;

namespace NewLife.Web;

/// <summary>访问令牌模型</summary>
public class TokenModel
{
    /// <summary>访问令牌</summary>
    [DataMember(Name = "access_token")]
    public String? AccessToken { get; set; }

    /// <summary>令牌类型</summary>
    [DataMember(Name = "token_type")]
    public String? TokenType { get; set; }

    /// <summary>过期时间。秒</summary>
    [DataMember(Name = "expire_in")]
    public Int32 ExpireIn { get; set; }

    /// <summary>刷新令牌</summary>
    [DataMember(Name = "refresh_token")]
    public String? RefreshToken { get; set; }
}