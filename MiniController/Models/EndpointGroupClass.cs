using System.Collections.Generic;

namespace MiniController.Models;

public class EndpointGroupClass
{
    public string Namespace { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public string RoutePrefix { get; set; } = string.Empty;

    public string? FilterType { get; set; }

    public AuthorizeMetadata? Authorize { get; set; }

    public List<EndpointMethod> EndpointMethods { get; set; } = new();

    /// <summary>
    /// 是否为静态类（用于向后兼容）
    /// </summary>
    public bool IsStatic { get; set; }
}