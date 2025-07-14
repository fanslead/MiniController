namespace MiniController.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class MiniControllerAttribute : Attribute
{
    public MiniControllerAttribute(string? routePrefix = null)
    {
        RoutePrefix = routePrefix;
    }

    public string? RoutePrefix { get; }
}
