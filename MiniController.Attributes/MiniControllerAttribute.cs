namespace MiniController.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class MiniControllerAttribute : Attribute
{
    public MiniControllerAttribute(string? routePrefix = null)
    {
        RoutePrefix = routePrefix;
    }

    public string? RoutePrefix { get; }
    
    /// <summary>
    /// 端点过滤器类型
    /// </summary>
    public Type? FilterType { get; set; }
}
