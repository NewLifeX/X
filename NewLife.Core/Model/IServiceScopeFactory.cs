namespace NewLife.Model;

/// <summary>范围服务工厂</summary>
public interface IServiceScopeFactory
{
    /// <summary>创建范围服务</summary>
    /// <returns></returns>
    IServiceScope CreateScope();
}

class MyServiceScopeFactory : IServiceScopeFactory
{
    public IServiceProvider? ServiceProvider { get; set; }

    public IServiceScope CreateScope() => new MyServiceScope { MyServiceProvider = ServiceProvider };
}