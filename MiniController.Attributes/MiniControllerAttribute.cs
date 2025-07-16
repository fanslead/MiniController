namespace MiniController.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class MiniControllerAttribute : Attribute
{
    public MiniControllerAttribute(string? routePrefix = null)
    {
        RoutePrefix = routePrefix;
    }

    public string? RoutePrefix { get; }
    
    /// <summary>
    /// 端点组名称，用于API文档分组
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// 端点过滤器类型
    /// </summary>
    public Type? FilterType { get; set; }
}
