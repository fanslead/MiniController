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
    /// ����������Ϣ����������ʵ���������ã�
    /// </summary>
    public List<MethodParameterInfo> Parameters { get; set; } = new();

    /// <summary>
    /// ������������
    /// </summary>
    public string ReturnType { get; set; } = "IResult";

    /// <summary>
    /// �Ƿ�Ϊ��̬����
    /// </summary>
    public bool IsStatic { get; set; }
}

/// <summary>
/// ����������Ϣ
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