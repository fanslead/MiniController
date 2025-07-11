namespace MiniController.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class MiniControllerAttribute : Attribute
{
    public MiniControllerAttribute(string? routePrefix = null)
    {
        RoutePrefix = routePrefix;
    }

    public string? RoutePrefix { get; }

    public string? Name { get; set; }

    public Type? FilterType { get; set; }

    public string[]? Policies { get; set; }
}
