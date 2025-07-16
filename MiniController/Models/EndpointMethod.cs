using System.Collections.Generic;

namespace MiniController.Models;

public class EndpointMethod
{
    public string Name { get; set; } = string.Empty;

    public string HttpMethod { get; set; } = string.Empty;

    public string RouteTemplate { get; set; } = string.Empty;

    public string? FilterType { get; set; }

    public AuthorizeMetadata? Authorize { get; set; }

    public List<ResponseTypeMetadata> ResponseTypes { get; set; } = new();

    public ApiExplorerSettingsMetadata? ApiExplorerSettings { get; set; }

    /// <summary>
    /// 方法参数信息（用于生成实例方法调用）
    /// </summary>
    public List<MethodParameterInfo> Parameters { get; set; } = new();

    /// <summary>
    /// 方法返回类型
    /// </summary>
    public string ReturnType { get; set; } = "IResult";

    /// <summary>
    /// 是否为静态方法
    /// </summary>
    public bool IsStatic { get; set; }
}

/// <summary>
/// 方法参数信息
/// </summary>
public class MethodParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsFromServices { get; set; }
    public bool IsFromRoute { get; set; }
    public bool IsFromQuery { get; set; }
    public bool IsFromBody { get; set; }
    public bool IsFromHeader { get; set; }
    public bool IsFromForm { get; set; }
}